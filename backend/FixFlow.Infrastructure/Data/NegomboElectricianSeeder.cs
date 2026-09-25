using FixFlow.Application.Interfaces;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class NegomboElectricianSeeder(FixFlowDbContext db, IPasswordHasher passwordHasher, ILogger<NegomboElectricianSeeder> logger)
{
    public const string Password = JaffnaTechnicianSeeder.Password;

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var people = new[]
        {
            new { Email = "kumar.electrician@negombo.fixflow.local", Name = "Kumar Fernando", Phone = "0312220101", UserId = Guid.Parse("b7a1c001-0001-4000-8000-080100000000"), ProfileId = Guid.Parse("b7a1c001-0002-4000-8000-080100000000"), ApplicationId = Guid.Parse("b7a1c001-0003-4000-8000-080100000000") },
            new { Email = "suresh.electrician@negombo.fixflow.local", Name = "Suresh Perera", Phone = "0312220102", UserId = Guid.Parse("b7a1c001-0001-4000-8000-080200000000"), ProfileId = Guid.Parse("b7a1c001-0002-4000-8000-080200000000"), ApplicationId = Guid.Parse("b7a1c001-0003-4000-8000-080200000000") }
        };

        var existing = await db.Users
            .Where(x => x.Email.EndsWith("@negombo.fixflow.local"))
            .Select(x => x.Email)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = people.Where(x => !existingSet.Contains(x.Email)).ToArray();
        if (pending.Length == 0)
        {
            logger.LogInformation("Negombo electrician seed already present.");
            return 0;
        }

        var hash = passwordHasher.Hash(Password);
        var now = DateTimeOffset.UtcNow;
        foreach (var person in pending)
        {
            db.Users.Add(new User
            {
                Id = person.UserId,
                Email = person.Email,
                PasswordHash = hash,
                Role = UserRole.Technician,
                DisplayName = person.Name,
                Phone = person.Phone,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.TechnicianProfiles.Add(new TechnicianProfile
            {
                Id = person.ProfileId,
                UserId = person.UserId,
                Bio = "Verified electrician for household switch and wiring work.",
                ServiceArea = "Negombo",
                LatitudeApprox = 7.2008,
                LongitudeApprox = 79.8737,
                ExperienceSummary = "Serves Negombo and nearby coastal towns.",
                CreatedAt = now,
                UpdatedAt = now
            });
            db.TechnicianCategoryApplications.Add(new TechnicianCategoryApplication
            {
                Id = person.ApplicationId,
                TechnicianId = person.ProfileId,
                CategoryId = ServiceCategorySeed.Electrician,
                Status = ApplicationStatus.Approved,
                SubmittedAt = now,
                DecidedAt = now,
                DecisionNotes = "Seeded approved electrician for the Negombo example scenario.",
                Version = 1
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Negombo electrician seed complete. Added {Added}.", pending.Length);
        return pending.Length;
    }
}
