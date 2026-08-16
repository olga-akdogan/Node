using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;

namespace Node.Data.Data;

/// <summary>
/// Extensive seeding at startup of an empty database.
/// Everything is deterministic so every run produces the same
/// demo data: roles, users, natal charts with placements, swipes,
/// matches and reports.
///
/// The demo members are well-known public figures with published birth data
/// (name, date, time, place) so the chart calculation (Swiss Ephemeris) can
/// be tested against known, externally verifiable charts. Coordinates were
/// looked up based on the given birth place.
/// </summary>
public static class DbSeeder
{
    /// <summary>The three active roles of the application.</summary>
    public const string RoleAdmin = "Admin";
    public const string RoleModerator = "Moderator";
    public const string RoleMember = "Lid";

    /// <summary>Default password for demo accounts (seeding/demo only).</summary>
    private const string DemoPassword = "Node!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var natalChartCalculator = services.GetRequiredService<INatalChartCalculator>();
        var matchInterpretationService = services.GetRequiredService<IMatchInterpretationService>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Automatically apply pending migrations so the database always has
        // the correct structure at startup.
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        var users = await SeedUsersAsync(userManager, logger);
        await SeedPartnerPreferencesAsync(context, users);
        await SeedNatalChartsAsync(context, users, natalChartCalculator);
        await SeedSwipesAndMatchesAsync(context, users, matchInterpretationService);
        // Chat conversations no longer live in this database
        // GetStream integration, so they aren't seeded here
        await SeedReportsAsync(context, users);

        logger.LogInformation("Seeding complete.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { RoleAdmin, RoleModerator, RoleMember })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// Creates the demo accounts: one admin, one moderator and 24 members
    /// (well-known public figures with published birth data).
    /// Email is confirmed right away so the demo accounts can log in
    /// (real registrations do need to confirm their email first).
    /// </summary>
    private static async Task<List<ApplicationUser>> SeedUsersAsync(
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        // (name, email, birth date, birth time, place, lat, lng, time unknown?, role, gender)
        var definitions = new (string Name, string Email, DateOnly Date, TimeOnly Time,
            string Place, decimal Lat, decimal Lng, bool TimeUnknown, string Role, Gender Gender)[]
        {
            ("Beheerder", "admin@node.be", new(1990, 1, 15), new(8, 30), "Brussel, België",
                50.850300m, 4.351700m, false, RoleAdmin, Gender.Male),
            ("Mo de Moderator", "moderator@node.be", new(1992, 6, 21), new(14, 0), "Gent, België",
                51.054300m, 3.717400m, false, RoleModerator, Gender.Female),

            ("Meghan Markle", "meghan.markle@demo.node.be", new(1981, 8, 4), new(4, 46),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RoleMember, Gender.Female),
            ("Prince Harry", "prince.harry@demo.node.be", new(1984, 9, 15), new(16, 20),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RoleMember, Gender.Male),
            ("JFK Jr", "jfk.jr@demo.node.be", new(1960, 11, 25), new(0, 22),
                "Washington D.C., Verenigde Staten", 38.907192m, -77.036873m, false, RoleMember, Gender.Male),
            ("Carolyn Bessette", "carolyn.bessette@demo.node.be", new(1966, 1, 7), new(8, 45),
                "New York, Verenigde Staten", 40.712776m, -74.005974m, false, RoleMember, Gender.Female),
            ("Zendaya Coleman", "zendaya.coleman@demo.node.be", new(1996, 9, 1), new(18, 1),
                "Oakland, Californië, Verenigde Staten", 37.804363m, -122.271111m, false, RoleMember, Gender.Female),
            ("Tom Holland", "tom.holland@demo.node.be", new(1996, 6, 1), new(12, 0),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, true, RoleMember, Gender.Male),
            ("Taylor Swift", "taylor.swift@demo.node.be", new(1989, 12, 13), new(8, 36),
                "Reading (Pennsylvania), Verenigde Staten", 40.335560m, -75.926880m, false, RoleMember, Gender.Female),
            ("Travis Kelce", "travis.kelce@demo.node.be", new(1989, 10, 5), new(5, 49),
                "Westlake (Ohio), Verenigde Staten", 41.458401m, -81.918404m, false, RoleMember, Gender.Male),
            ("Barack Obama", "barack.obama@demo.node.be", new(1961, 8, 4), new(19, 24),
                "Honolulu, Verenigde Staten", 21.306944m, -157.858337m, false, RoleMember, Gender.Male),
            ("Michelle Obama", "michelle.obama@demo.node.be", new(1964, 1, 17), new(12, 0),
                "Chicago, Verenigde Staten", 41.878113m, -87.629799m, true, RoleMember, Gender.Female),
            ("George Clooney", "george.clooney@demo.node.be", new(1961, 5, 6), new(2, 58),
                "Lexington (Kentucky), Verenigde Staten", 38.040585m, -84.503716m, false, RoleMember, Gender.Male),
            ("Amal Clooney", "amal.clooney@demo.node.be", new(1978, 2, 3), new(12, 0),
                "Beiroet, Libanon", 33.893891m, 35.501801m, true, RoleMember, Gender.Female),
            ("Prince William", "prince.william@demo.node.be", new(1982, 6, 21), new(21, 3),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RoleMember, Gender.Male),
            ("Kate Middleton", "kate.middleton@demo.node.be", new(1982, 1, 9), new(19, 0),
                "Reading, Engeland", 51.454264m, -0.978180m, false, RoleMember, Gender.Female),
            ("JFK", "jfk@demo.node.be", new(1917, 5, 29), new(15, 0),
                "Brookline (Massachusetts), Verenigde Staten", 42.331798m, -71.121269m, false, RoleMember, Gender.Male),
            ("Marilyn Monroe", "marilyn.monroe@demo.node.be", new(1926, 6, 1), new(9, 30),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RoleMember, Gender.Female),
            ("Jackie Kennedy", "jackie.kennedy@demo.node.be", new(1929, 7, 28), new(14, 30),
                "New York, Verenigde Staten", 40.712776m, -74.005974m, false, RoleMember, Gender.Female),
            ("Ben Affleck", "ben.affleck@demo.node.be", new(1972, 8, 15), new(2, 53),
                "Berkeley (Californië), Verenigde Staten", 37.871593m, -122.272743m, false, RoleMember, Gender.Male),
            ("Jennifer Lopez", "jennifer.lopez@demo.node.be", new(1969, 7, 24), new(12, 0),
                "The Bronx (New York), Verenigde Staten", 40.844782m, -73.864827m, true, RoleMember, Gender.Female),
            ("Brad Pitt", "brad.pitt@demo.node.be", new(1963, 12, 18), new(6, 31),
                "Shawnee (Oklahoma), Verenigde Staten", 35.327332m, -96.925285m, false, RoleMember, Gender.Male),
            ("Jennifer Aniston", "jennifer.aniston@demo.node.be", new(1969, 2, 11), new(22, 22),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RoleMember, Gender.Female),
            ("Angelina Jolie", "angelina.jolie@demo.node.be", new(1975, 6, 4), new(9, 9),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RoleMember, Gender.Female),
            ("King Charles", "king.charles@demo.node.be", new(1948, 11, 14), new(21, 14),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RoleMember, Gender.Male),
            ("Princess Diana", "princess.diana@demo.node.be", new(1961, 7, 1), new(19, 45),
                "Sandringham, Verenigd Koninkrijk", 52.834721m, 0.505600m, false, RoleMember, Gender.Female),
        };

        var result = new List<ApplicationUser>();

        foreach (var d in definitions)
        {
            var existing = await userManager.FindByEmailAsync(d.Email);
            if (existing is not null)
            {
                // Backfill accounts from a seeding run before the gender field
                // existed, so the swipe deck works for them too.
                if (existing.Gender != d.Gender)
                {
                    existing.Gender = d.Gender;
                    await userManager.UpdateAsync(existing);
                }

                result.Add(existing);
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = d.Email,
                Email = d.Email,
                EmailConfirmed = true, // Demo accounts skip email verification.
                DisplayName = d.Name,
                BirthDate = d.Date,
                BirthTime = d.Time,
                BirthTimeIsUnknown = d.TimeUnknown,
                BirthPlace = d.Place,
                BirthLatitude = d.Lat,
                BirthLongitude = d.Lng,
                Gender = d.Gender,
                CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            };

            var created = await userManager.CreateAsync(user, DemoPassword);
            if (!created.Succeeded)
            {
                logger.LogError("Seeding user {Email} failed: {Errors}",
                    d.Email, string.Join(", ", created.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, d.Role);
            result.Add(user);
        }

        return result;
    }

    /// <summary>
    /// Sets a partner preference for every demo user: the opposite gender,
    /// consistent with the seeded couples below (all man-woman).
    /// </summary>
    private static async Task SeedPartnerPreferencesAsync(ApplicationDbContext context, List<ApplicationUser> users)
    {
        if (await context.PartnerPreferences.AnyAsync())
        {
            return;
        }

        foreach (var user in users)
        {
            var oppositeGender = user.Gender == Gender.Male ? Gender.Female : Gender.Male;
            context.PartnerPreferences.Add(new PartnerPreference { UserId = user.Id, Gender = oppositeGender });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Calculates and saves the real birth chart (Swiss Ephemeris) for every
    /// member, with all placements (Sun through Pluto plus Ascendant).
    /// </summary>
    private static async Task SeedNatalChartsAsync(
        ApplicationDbContext context, List<ApplicationUser> users, INatalChartCalculator calculator)
    {
        if (await context.NatalCharts.AnyAsync())
        {
            return; 
        }

        foreach (var user in users)
        {
            var chart = calculator.Calculate(user);
            context.NatalCharts.Add(chart);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds mutual likes (which produce matches) only for pairs with a
    /// documented real relationship (marriage or public relationship).
    /// </summary>
    private static async Task SeedSwipesAndMatchesAsync(
        ApplicationDbContext context, List<ApplicationUser> users, IMatchInterpretationService matchInterpretationService)
    {
        if (await context.Swipes.AnyAsync())
        {
            return;
        }

        var byEmail = users.ToDictionary(g => g.Email!, g => g);

        // Mutual likes: documented couples, become matches.
        var mutualLikes = new[]
        {
            ("meghan.markle@demo.node.be", "prince.harry@demo.node.be"),
            ("jfk.jr@demo.node.be", "carolyn.bessette@demo.node.be"),
            ("zendaya.coleman@demo.node.be", "tom.holland@demo.node.be"),
            ("taylor.swift@demo.node.be", "travis.kelce@demo.node.be"),
            ("barack.obama@demo.node.be", "michelle.obama@demo.node.be"),
            ("george.clooney@demo.node.be", "amal.clooney@demo.node.be"),
            ("prince.william@demo.node.be", "kate.middleton@demo.node.be"),
            ("jfk@demo.node.be", "jackie.kennedy@demo.node.be"),
            ("ben.affleck@demo.node.be", "jennifer.lopez@demo.node.be"),
            ("brad.pitt@demo.node.be", "jennifer.aniston@demo.node.be"),
            ("king.charles@demo.node.be", "princess.diana@demo.node.be"),
        };

        var timestamp = new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc);

        foreach (var (a, b) in mutualLikes)
        {
            var userA = byEmail[a];
            var userB = byEmail[b];

            context.Swipes.Add(new Swipe { SwiperUserId = userA.Id, TargetUserId = userB.Id, IsLike = true, CreatedAt = timestamp });
            context.Swipes.Add(new Swipe { SwiperUserId = userB.Id, TargetUserId = userA.Id, IsLike = true, CreatedAt = timestamp.AddHours(2) });

            var (first, second) = string.CompareOrdinal(userA.Id, userB.Id) < 0 ? (userA, userB) : (userB, userA);

            var chartA = await context.NatalCharts.Include(n => n.Placements).FirstAsync(n => n.UserId == first.Id);
            var chartB = await context.NatalCharts.Include(n => n.Placements).FirstAsync(n => n.UserId == second.Id);
            var (score, _) = DemoSynastry.Calculate(chartA, chartB);

            // The explanation text comes from Claude API based on the full natal charts;
            // the score itself stays deterministically calculated above.
            var explanation = await matchInterpretationService.WriteMatchInterpretationAsync(first, chartA, second, chartB, score, "nl");

            context.Matches.Add(new Match
            {
                User1Id = first.Id,
                User2Id = second.Id,
                CompatibilityScore = score,
                CompatibilityExplanation = explanation,
                CompatibilityExplanationLanguage = "nl",
                Status = MatchStatus.Active,
                MatchedAt = timestamp.AddHours(2),
            });

            timestamp = timestamp.AddDays(1);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds two reports so the moderation screen has data. The reason is
    /// deliberately neutral test text (not a real complaint)
    /// </summary>
    private static async Task SeedReportsAsync(ApplicationDbContext context, List<ApplicationUser> users)
    {
        if (await context.Reports.AnyAsync())
        {
            return;
        }

        var byEmail = users.ToDictionary(g => g.Email!, g => g);

        context.Reports.Add(new Report
        {
            ReporterUserId = byEmail["marilyn.monroe@demo.node.be"].Id,
            ReportedUserId = byEmail["angelina.jolie@demo.node.be"].Id,
            Reason = "Testmelding voor de moderatiedemo (geen echte klacht).",
            IsResolved = false,
            CreatedAt = new DateTime(2026, 2, 20, 9, 30, 0, DateTimeKind.Utc),
        });

        context.Reports.Add(new Report
        {
            ReporterUserId = byEmail["jackie.kennedy@demo.node.be"].Id,
            ReportedUserId = byEmail["ben.affleck@demo.node.be"].Id,
            Reason = "Testmelding voor de moderatiedemo (geen echte klacht).",
            IsResolved = true,
            CreatedAt = new DateTime(2026, 2, 25, 18, 45, 0, DateTimeKind.Utc),
        });

        await context.SaveChangesAsync();
    }
}
