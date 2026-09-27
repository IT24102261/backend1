using System.Net;
using System.Net.Http.Json;
using FixFlow.Application.DTOs.Auth;
using FixFlow.Domain.Constants;
using FixFlow.Tests.Support;

namespace FixFlow.Tests.Integration;

[Collection(nameof(FixFlowApiCollection))]
public class AuthApiTests(FixFlowApiFixture fixture)
{
    [Fact]
    public async Task Register_ValidCustomer_ReturnsTokenAndRole()
    {
        var email = $"auth.valid.{Guid.NewGuid():N}@fixflow.test";
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = FixFlowApiFixture.Password,
            displayName = "Valid Customer",
            role = "CUSTOMER",
            phone = "0771234567"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(FixFlowApiFixture.Json);
        Assert.NotNull(body);
        Assert.Equal(email, body.Email);
        Assert.Equal("CUSTOMER", body.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.NotEqual(Guid.Empty, body.UserId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var first = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = first.Email,
            password = FixFlowApiFixture.Password,
            displayName = "Duplicate",
            role = "CUSTOMER"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_AdminRole_IsForbidden()
    {
        using var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"admin.self.{Guid.NewGuid():N}@fixflow.test",
            password = FixFlowApiFixture.Password,
            displayName = "Admin",
            role = "ADMIN"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        var registered = await fixture.RegisterAsync("CUSTOMER");
        var loggedIn = await fixture.LoginAsync(registered.Email);

        Assert.Equal(registered.UserId, loggedIn.UserId);
        Assert.Equal("CUSTOMER", loggedIn.Role);
        Assert.False(string.IsNullOrWhiteSpace(loggedIn.AccessToken));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var registered = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = registered.Email,
            password = "WrongPassword1!"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Technician_WaitsForAdminApproval()
    {
        var email = $"tech.pending.{Guid.NewGuid():N}@fixflow.test";
        using var client = fixture.CreateClient();
        using var form = FixFlowApiFixture.TechnicianRegisterForm(email, "Pending Technician", ServiceCategorySeed.Painter);
        var response = await client.PostAsync("/api/auth/register/technician", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(FixFlowApiFixture.Json);
        Assert.True(body!.RequiresAdminApproval);
        Assert.True(string.IsNullOrWhiteSpace(body.AccessToken));

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = FixFlowApiFixture.Password
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var admin = await fixture.LoginAdminAsync();
        using var adminClient = fixture.CreateClient(admin.AccessToken);
        var users = await adminClient.GetFromJsonAsync<FixFlow.Application.Common.PagedResult<FixFlow.Application.DTOs.Admin.AdminUserDto>>(
            $"/api/admin/users?search={email}&status=PENDING",
            FixFlowApiFixture.Json);
        var pending = Assert.Single(users!.Items);
        Assert.Equal("PENDING", pending.LoginStatus);
        Assert.Equal("Painter", pending.RequestedCategory);

        var accepted = await adminClient.PatchAsJsonAsync($"/api/admin/users/{pending.Id}/status", new { isActive = true }, FixFlowApiFixture.Json);
        accepted.EnsureSuccessStatusCode();

        var allowed = await fixture.LoginAsync(email);
        Assert.Equal("TECHNICIAN", allowed.Role);
        Assert.False(string.IsNullOrWhiteSpace(allowed.AccessToken));
    }

    [Fact]
    public async Task Register_TechnicianWithoutNic_ReturnsBadRequest()
    {
        using var client = fixture.CreateClient();
        using var form = FixFlowApiFixture.TechnicianRegisterForm(
            $"tech.nonic.{Guid.NewGuid():N}@fixflow.test",
            "No Nic",
            ServiceCategorySeed.Painter,
            includeNic: false);
        var response = await client.PostAsync("/api/auth/register/technician", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ElectricianWithoutCertificate_ReturnsBadRequest()
    {
        using var client = fixture.CreateClient();
        using var form = FixFlowApiFixture.TechnicianRegisterForm(
            $"tech.nocert.{Guid.NewGuid():N}@fixflow.test",
            "No Certificate",
            ServiceCategorySeed.Electrician,
            includeCertificate: false);
        var response = await client.PostAsync("/api/auth/register/technician", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_TechnicianWithoutProfilePhoto_ReturnsBadRequest()
    {
        using var client = fixture.CreateClient();
        using var form = FixFlowApiFixture.TechnicianRegisterForm(
            $"tech.nophoto.{Guid.NewGuid():N}@fixflow.test",
            "No Photo",
            ServiceCategorySeed.Painter,
            includeProfilePhoto: false);
        var response = await client.PostAsync("/api/auth/register/technician", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_TechnicianCannotCreateCustomerRequest()
    {
        var technician = await fixture.RegisterAsync("TECHNICIAN");
        using var client = fixture.CreateClient(technician.AccessToken);

        var response = await client.PostAsJsonAsync("/api/requests", new
        {
            categoryId = ServiceCategorySeed.Electrician,
            description = "Two wall switches are not working."
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_CustomerCannotApproveTechnicianApplications()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/technician-applications/{Guid.NewGuid()}/approve",
            new { notes = "no" },
            FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsRejected()
    {
        using var client = fixture.CreateClient();
        var me = await client.GetAsync("/api/me");
        var create = await client.PostAsJsonAsync("/api/requests", new
        {
            categoryId = ServiceCategorySeed.Electrician,
            description = "Need an electrician"
        }, FixFlowApiFixture.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Me_UpdateProfileAndChangePassword_Succeeds()
    {
        var customer = await fixture.RegisterAsync("CUSTOMER");
        using var client = fixture.CreateClient(customer.AccessToken);

        var updated = await client.PutAsJsonAsync("/api/me", new { displayName = "Updated Customer", phone = "0771234567" }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var me = await updated.Content.ReadFromJsonAsync<MeResponse>(FixFlowApiFixture.Json);
        Assert.Equal("Updated Customer", me!.DisplayName);
        Assert.Equal("0771234567", me.Phone);

        var password = await client.PostAsJsonAsync("/api/me/password", new
        {
            currentPassword = FixFlowApiFixture.Password,
            newPassword = "NewPassword1!"
        }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.NoContent, password.StatusCode);

        using var anon = fixture.CreateClient();
        var oldLogin = await anon.PostAsJsonAsync("/api/auth/login", new { email = customer.Email, password = FixFlowApiFixture.Password }, FixFlowApiFixture.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        var fresh = await fixture.LoginAsync(customer.Email, "NewPassword1!");
        Assert.Equal(customer.UserId, fresh.UserId);
    }
}
