using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FixFlow.Application.DTOs.Auth;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Tests.Support;

[CollectionDefinition(nameof(FixFlowApiCollection))]
public sealed class FixFlowApiCollection : ICollectionFixture<FixFlowApiFixture>;

public sealed class FixFlowApiFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin.tests@fixflow.local";
    public const string Password = "Password1!";

    private PostgresTestDatabase? _database;

    public FixFlowApiFactory Factory { get; private set; } = null!;
    public string ConnectionString => Factory.ConnectionString;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        _database = await PostgresTestDatabase.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _database.ConnectionString);
        Factory = new FixFlowApiFactory(_database.ConnectionString);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(x => x.Email == AdminEmail))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            db.Users.Add(new User
            {
                Email = AdminEmail,
                PasswordHash = hasher.Hash(Password),
                DisplayName = "Test Admin",
                Role = UserRole.Admin,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    public HttpClient CreateClient(string? accessToken = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    public async Task<AuthResponse> RegisterAsync(string role, string? email = null, string displayName = "Test User")
    {
        email ??= $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@fixflow.test";
        using var client = CreateClient();
        HttpResponseMessage response;
        if (role.Equals("TECHNICIAN", StringComparison.OrdinalIgnoreCase))
        {
            using var content = TechnicianRegisterForm(
                email,
                displayName,
                ServiceCategorySeed.Electrician,
                includeCertificate: true);
            response = await client.PostAsync("/api/auth/register/technician", content);
        }
        else
        {
            response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password, displayName, role }, Json);
        }
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        if (role.Equals("TECHNICIAN", StringComparison.OrdinalIgnoreCase) &&
            (body.RequiresAdminApproval || string.IsNullOrWhiteSpace(body.AccessToken)))
        {
            await ScopeAsync(async services =>
            {
                var db = services.GetRequiredService<FixFlowDbContext>();
                var user = await db.Users.SingleAsync(x => x.Email == email);
                user.IsActive = true;
                await db.SaveChangesAsync();
            });
            return await LoginAsync(email);
        }

        return body;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password = Password)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    public Task<AuthResponse> LoginAdminAsync() => LoginAsync(AdminEmail);

    public async Task<T> ScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public async Task ScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    public static MultipartFormDataContent TechnicianRegisterForm(
        string email,
        string displayName,
        Guid categoryId,
        string? password = null,
        bool includeNic = true,
        bool includeCertificate = false,
        bool includeProfilePhoto = true)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(email), "email");
        content.Add(new StringContent(password ?? Password), "password");
        content.Add(new StringContent(displayName), "displayName");
        content.Add(new StringContent("TECHNICIAN"), "role");
        content.Add(new StringContent("0771234567"), "phone");
        content.Add(new StringContent("Nallur, Jaffna"), "address");
        content.Add(new StringContent(categoryId.ToString()), "categoryId");
        if (includeProfilePhoto)
        {
            var photo = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xD9]);
            photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(photo, "profilePhoto", "profile.jpg");
        }
        if (includeNic)
        {
            var nic = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xD9]);
            nic.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(nic, "nicPhoto", "nic.jpg");
        }

        if (includeCertificate)
        {
            var certificate = new ByteArrayContent("study-certificate"u8.ToArray());
            certificate.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(certificate, "certificate", "certificate.pdf");
        }

        return content;
    }
}
