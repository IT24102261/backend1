using FixFlow.Application.Agents;
using FixFlow.Application.Interfaces;
using FixFlow.Infrastructure.Authentication;
using FixFlow.Infrastructure.Data;
using FixFlow.Infrastructure.ExternalServices;
using FixFlow.Infrastructure.Repositories;
using FixFlow.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured. Use environment variables or user secrets.");

        services.AddDbContext<FixFlowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(5);
                    npgsql.MigrationsAssembly(typeof(FixFlowDbContext).Assembly.GetName().Name);
                })
                .UseSnakeCaseNamingConvention());

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<JaffnaTechnicianSeeder>();
        services.AddScoped<JaffnaRequestSeeder>();
        services.AddScoped<NegomboElectricianSeeder>();
        services.AddScoped<TechnicianRatingSeeder>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddSingleton<DeterministicAiModelClient>();
        services.AddSingleton<IAiModelClient>(sp => sp.GetRequiredService<DeterministicAiModelClient>());
        services.AddSingleton<ILlmModelAdapter>(sp => sp.GetRequiredService<DeterministicAiModelClient>());
        services.Configure<MapOptions>(configuration.GetSection(MapOptions.SectionName));
        services.AddHttpClient<IMapService, HttpMapService>((sp, client) =>
        {
            var maps = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MapOptions>>().Value;
            if (Uri.TryCreate(maps.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, maps.TimeoutSeconds));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(maps.UserAgent)
                ? "FixFlowAI/1.0"
                : maps.UserAgent);
        });

        return services;
    }
}
