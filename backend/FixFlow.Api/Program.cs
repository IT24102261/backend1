using FixFlow.Api.Extensions;
using FixFlow.Api.Middleware;
using FixFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

builder.Logging.AddJsonConsole();
builder.Services.AddControllers();
builder.Services.AddFixFlowServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:5173"]));
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migrate failed. Registration and categories will return 500 until ConnectionStrings__Default is a valid Render Postgres string.");
        throw;
    }

    if (app.Environment.IsDevelopment())
    {
        await scope.ServiceProvider.GetRequiredService<JaffnaTechnicianSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<NegomboElectricianSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<JaffnaRequestSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<TechnicianRatingSeeder>().SeedAsync();
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
