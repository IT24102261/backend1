using FixFlow.Application.Interfaces;
using FixFlow.Domain.Constants;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Infrastructure.Data;

public sealed class JaffnaTechnicianSeeder(FixFlowDbContext db, IPasswordHasher passwordHasher, ILogger<JaffnaTechnicianSeeder> logger)
{
    public const string Password = "JaffnaTech@123";
    public const string ServiceArea = "Jaffna";

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await db.Users
            .Where(x => x.Email.EndsWith("@jaffna.fixflow.local"))
            .Select(x => x.Email)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = Catalog.Where(person => !existingSet.Contains(person.Email)).ToArray();
        if (pending.Length == 0)
        {
            logger.LogInformation("Jaffna technician seed already present ({Total} accounts).", Catalog.Length);
            return 0;
        }

        var passwordHash = passwordHasher.Hash(Password);
        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var person in pending)
        {

            var user = new User
            {
                Id = person.UserId,
                Email = person.Email,
                PasswordHash = passwordHash,
                Role = UserRole.Technician,
                DisplayName = person.DisplayName,
                Phone = person.Phone,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var profile = new TechnicianProfile
            {
                Id = person.ProfileId,
                UserId = user.Id,
                Bio = person.Bio,
                ServiceArea = ServiceArea,
                LatitudeApprox = person.Latitude,
                LongitudeApprox = person.Longitude,
                ExperienceSummary = person.Experience,
                CreatedAt = now,
                UpdatedAt = now
            };

            var application = new TechnicianCategoryApplication
            {
                Id = person.ApplicationId,
                TechnicianId = profile.Id,
                CategoryId = person.CategoryId,
                Status = ApplicationStatus.Approved,
                SubmittedAt = now,
                DecidedAt = now,
                DecisionNotes = "Seeded approved technician for Jaffna District.",
                Version = 1
            };

            db.Users.Add(user);
            db.TechnicianProfiles.Add(profile);
            db.TechnicianCategoryApplications.Add(application);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Jaffna technician seed complete. Added {Added} of {Total}.", added, Catalog.Length);
        return added;
    }

    private static readonly SeedPerson[] Catalog =
    [
        Person(1, 1, ServiceCategorySeed.AcRefrigeration, "Nimal Rajkumar", "0212220101", "Nallur, Jaffna District", 9.6682, 80.0291, "AC installation and gas charging"),
        Person(1, 2, ServiceCategorySeed.AcRefrigeration, "Suresh Vigneswaran", "0212220102", "Chundikuli, Jaffna District", 9.6548, 80.0214, "Split AC and fridge repair"),
        Person(1, 3, ServiceCategorySeed.AcRefrigeration, "Kajan Thurairajah", "0212220103", "Gurunagar, Jaffna District", 9.6491, 80.0176, "Commercial refrigeration"),
        Person(1, 4, ServiceCategorySeed.AcRefrigeration, "Ravi Chandran", "0212220104", "Kopay, Jaffna District", 9.7054, 80.0762, "Home AC servicing"),
        Person(1, 5, ServiceCategorySeed.AcRefrigeration, "Pradeep Selvarajah", "0212220105", "Chunnakam, Jaffna District", 9.7418, 80.0169, "Cold room and freezer work"),

        Person(2, 1, ServiceCategorySeed.ApplianceRepair, "Gajan Mahendran", "0212220201", "Jaffna Town, Jaffna District", 9.6615, 80.0255, "Washing machine and cooker repair"),
        Person(2, 2, ServiceCategorySeed.ApplianceRepair, "Dilani Sivakumar", "0212220202", "Manipay, Jaffna District", 9.7206, 80.0041, "Kitchen appliance diagnostics"),
        Person(2, 3, ServiceCategorySeed.ApplianceRepair, "Arun Nadesan", "0212220203", "Nallur, Jaffna District", 9.6701, 80.0312, "TV and microwave repair"),
        Person(2, 4, ServiceCategorySeed.ApplianceRepair, "Meena Pathmanathan", "0212220204", "Tellippalai, Jaffna District", 9.7864, 80.0338, "Fridge and iron servicing"),
        Person(2, 5, ServiceCategorySeed.ApplianceRepair, "Kumar Jegatheeswaran", "0212220205", "Karainagar, Jaffna District", 9.7312, 79.8824, "Home appliance call-outs"),

        Person(3, 1, ServiceCategorySeed.Carpenter, "Selvam Rajaratnam", "0212220301", "Nallur, Jaffna District", 9.6674, 80.0284, "Doors, windows and furniture"),
        Person(3, 2, ServiceCategorySeed.Carpenter, "Thanujan Sivananthan", "0212220302", "Chundikuli, Jaffna District", 9.6562, 80.0228, "Kitchen cabinet carpentry"),
        Person(3, 3, ServiceCategorySeed.Carpenter, "Bala Krishnan", "0212220303", "Kopay, Jaffna District", 9.7088, 80.0791, "Roof timber and repairs"),
        Person(3, 4, ServiceCategorySeed.Carpenter, "Jeyanthan Murugesu", "0212220304", "Point Pedro, Jaffna District", 9.8167, 80.2333, "Custom woodwork"),
        Person(3, 5, ServiceCategorySeed.Carpenter, "Ruban Thevarajah", "0212220305", "Jaffna Town, Jaffna District", 9.6602, 80.0241, "Wardrobes and partitions"),

        Person(4, 1, ServiceCategorySeed.Electrician, "Siva Kanagaratnam", "0212220401", "Jaffna Town, Jaffna District", 9.6610, 80.0250, "House wiring and fault finding"),
        Person(4, 2, ServiceCategorySeed.Electrician, "Ajanthan Yogeswaran", "0212220402", "Nallur, Jaffna District", 9.6693, 80.0304, "Switchboard and lighting"),
        Person(4, 3, ServiceCategorySeed.Electrician, "Niroshan Ponnambalam", "0212220403", "Chunnakam, Jaffna District", 9.7432, 80.0188, "Three-phase and motor work"),
        Person(4, 4, ServiceCategorySeed.Electrician, "Kavitha Ramanathan", "0212220404", "Manipay, Jaffna District", 9.7221, 80.0062, "Home electrical upgrades"),
        Person(4, 5, ServiceCategorySeed.Electrician, "Tharshan Elayathamby", "0212220405", "Gurunagar, Jaffna District", 9.6484, 80.0168, "Emergency electrical repairs"),
        Person(4, 6, ServiceCategorySeed.Electrician, "Mahesh Kanesan", "0212220406", "Jaffna Town, Jaffna District", 9.6618, 80.0258, "Switch replacement and house wiring"),
        Person(4, 7, ServiceCategorySeed.Electrician, "Logeshwaran Sivarajah", "0212220407", "Nallur, Jaffna District", 9.6686, 80.0296, "Socket, lighting and breaker work"),
        Person(4, 8, ServiceCategorySeed.Electrician, "Anushiya Rajkumar", "0212220408", "Jaffna Town, Jaffna District", 9.6604, 80.0239, "Domestic electrical repairs"),

        Person(5, 1, ServiceCategorySeed.Painter, "Manoj Ganeshan", "0212220501", "Chundikuli, Jaffna District", 9.6555, 80.0209, "Interior wall painting"),
        Person(5, 2, ServiceCategorySeed.Painter, "Lavan Shanmuganathan", "0212220502", "Nallur, Jaffna District", 9.6668, 80.0277, "Exterior weather coating"),
        Person(5, 3, ServiceCategorySeed.Painter, "Priya Kailasapathy", "0212220503", "Jaffna Town, Jaffna District", 9.6624, 80.0266, "Colour consultation and finish"),
        Person(5, 4, ServiceCategorySeed.Painter, "Dinesh Vamathevan", "0212220504", "Kopay, Jaffna District", 9.7066, 80.0748, "House and shop painting"),
        Person(5, 5, ServiceCategorySeed.Painter, "Roshan Anantharajah", "0212220505", "Tellippalai, Jaffna District", 9.7881, 80.0352, "Wood and metal painting"),
        Person(5, 6, ServiceCategorySeed.Painter, "Karthik Sivarajah", "0212220506", "Manipay, Jaffna District", 9.7212, 80.0051, "Interior and exterior house painting"),
        Person(5, 7, ServiceCategorySeed.Painter, "Niranjala Thevarajah", "0212220507", "Chunnakam, Jaffna District", 9.7426, 80.0177, "Weatherproof coating and wall finish"),
        Person(5, 8, ServiceCategorySeed.Painter, "Suthan Rajendram", "0212220508", "Gurunagar, Jaffna District", 9.6489, 80.0174, "House and shop painting"),

        Person(6, 1, ServiceCategorySeed.Plumber, "Vasanth Muralitharan", "0212220601", "Jaffna Town, Jaffna District", 9.6608, 80.0248, "Leak repair and tap fitting"),
        Person(6, 2, ServiceCategorySeed.Plumber, "Sanjeev Iyathurai", "0212220602", "Nallur, Jaffna District", 9.6689, 80.0298, "Bathroom plumbing"),
        Person(6, 3, ServiceCategorySeed.Plumber, "Harini Balasingam", "0212220603", "Chunnakam, Jaffna District", 9.7405, 80.0154, "Water tank and pump work"),
        Person(6, 4, ServiceCategorySeed.Plumber, "Gopi Nadarajah", "0212220604", "Gurunagar, Jaffna District", 9.6477, 80.0182, "Drain and pipe replacement"),
        Person(6, 5, ServiceCategorySeed.Plumber, "Theepan Sriskandarajah", "0212220605", "Point Pedro, Jaffna District", 9.8142, 80.2311, "New house plumbing"),

        Person(7, 1, ServiceCategorySeed.Solar, "Aravindh Coomaraswamy", "0212220701", "Kopay, Jaffna District", 9.7041, 80.0774, "Rooftop solar installation"),
        Person(7, 2, ServiceCategorySeed.Solar, "Nisha Jeyakumar", "0212220702", "Jaffna Town, Jaffna District", 9.6631, 80.0271, "Inverter and panel servicing"),
        Person(7, 3, ServiceCategorySeed.Solar, "Yathusan Ponnuthurai", "0212220703", "Chunnakam, Jaffna District", 9.7456, 80.0196, "Off-grid solar systems"),
        Person(7, 4, ServiceCategorySeed.Solar, "Keerthi Sivasubramaniam", "0212220704", "Manipay, Jaffna District", 9.7194, 80.0033, "Solar wiring and earthing"),
        Person(7, 5, ServiceCategorySeed.Solar, "Praveen Rajendram", "0212220705", "Karainagar, Jaffna District", 9.7338, 79.8841, "Panel cleaning and maintenance")
    ];

    private static Guid SeedId(int kind, int categoryIndex, int personIndex) =>
        Guid.Parse($"b7a1c001-{kind:0000}-4000-8000-{categoryIndex:00}{personIndex:00}00000000");

    private static SeedPerson Person(
        int categoryIndex,
        int personIndex,
        Guid categoryId,
        string displayName,
        string phone,
        string address,
        double latitude,
        double longitude,
        string skill)
    {
        var slug = displayName.ToLowerInvariant().Replace(' ', '.');
        return new SeedPerson(
            UserId: SeedId(1, categoryIndex, personIndex),
            ProfileId: SeedId(2, categoryIndex, personIndex),
            ApplicationId: SeedId(3, categoryIndex, personIndex),
            Email: $"{slug}@jaffna.fixflow.local",
            DisplayName: displayName,
            Phone: phone,
            CategoryId: categoryId,
            Bio: $"{skill}. Based in {address}.",
            Experience: $"Serves Jaffna District from {address}.",
            Latitude: latitude,
            Longitude: longitude);
    }

    private sealed record SeedPerson(
        Guid UserId,
        Guid ProfileId,
        Guid ApplicationId,
        string Email,
        string DisplayName,
        string Phone,
        Guid CategoryId,
        string Bio,
        string Experience,
        double Latitude,
        double Longitude);
}
