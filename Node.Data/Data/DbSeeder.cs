using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;

namespace Node.Data.Data;

/// <summary>
/// Uitgebreide seeding bij het opstarten van een lege databank.
/// Alles is deterministisch zodat elke run dezelfde
/// demogegevens oplevert: rollen, gebruikers, horoscopen met posities,
/// swipes, matches en meldingen.
///
/// De demo-leden zijn bekende publieke figuren met gepubliceerde geboortegegevens
/// (naam, datum, tijd, plaats) zodat de horoscoopberekening (Swiss Ephemeris)
/// tegen bekende, extern verifieerbare horoscopen getest kan worden. Coördinaten
/// zijn zelf opgezocht op basis van de opgegeven geboorteplaats; enkele plaatsen
/// waren dubbelzinnig (bv. enkel een staat of provincie) en zijn dan aangevuld
/// met de meest gedocumenteerde specifieke geboorteplaats van die persoon
/// (zie de opmerkingen bij Zendaya Coleman en Travis Kelce hieronder).
/// </summary>
public static class DbSeeder
{
    /// <summary>De drie actieve rollen van de applicatie.</summary>
    public const string RolAdmin = "Admin";
    public const string RolModerator = "Moderator";
    public const string RolLid = "Lid";

    /// <summary>Standaardwachtwoord voor demo-accounts (alleen voor seeding/demo).</summary>
    private const string DemoWachtwoord = "Node!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var natalChartCalculator = services.GetRequiredService<INatalChartCalculator>();
        var matchInterpretationService = services.GetRequiredService<IMatchInterpretationService>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Openstaande migraties automatisch toepassen zodat de databank
        // altijd de juiste structuur heeft bij het opstarten.
        await context.Database.MigrateAsync();

        await SeedRollenAsync(roleManager);
        var gebruikers = await SeedGebruikersAsync(userManager, logger);
        await SeedPartnerPreferencesAsync(context, gebruikers);
        await SeedHoroscopenAsync(context, gebruikers, natalChartCalculator);
        await SeedSwipesEnMatchesAsync(context, gebruikers, matchInterpretationService);
        // Chat conversations no longer live in this database since the
        // GetStream integration, so they aren't seeded here.
        await SeedMeldingenAsync(context, gebruikers);

        logger.LogInformation("Seeding afgerond.");
    }

    private static async Task SeedRollenAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var rol in new[] { RolAdmin, RolModerator, RolLid })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }
    }

    /// <summary>
    /// Maakt de demo-accounts aan: één beheerder, één moderator en 24 leden
    /// (bekende publieke figuren met gepubliceerde geboortegegevens).
    /// E-mail staat meteen op bevestigd zodat de demo-accounts kunnen inloggen
    /// (echte registraties moeten hun e-mail wél eerst bevestigen).
    /// </summary>
    private static async Task<List<ApplicationUser>> SeedGebruikersAsync(
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        // (naam, e-mail, geboortedatum, geboortetijd, plaats, lat, lng, tijd onbekend?, rol, gender)
        var definities = new (string Naam, string Email, DateOnly Datum, TimeOnly Tijd,
            string Plaats, decimal Lat, decimal Lng, bool TijdOnbekend, string Rol, Gender Gender)[]
        {
            ("Beheerder", "admin@node.be", new(1990, 1, 15), new(8, 30), "Brussel, België",
                50.850300m, 4.351700m, false, RolAdmin, Gender.Male),
            ("Mo de Moderator", "moderator@node.be", new(1992, 6, 21), new(14, 0), "Gent, België",
                51.054300m, 3.717400m, false, RolModerator, Gender.Female),

            ("Meghan Markle", "meghan.markle@demo.node.be", new(1981, 8, 4), new(4, 46),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RolLid, Gender.Female),
            ("Prince Harry", "prince.harry@demo.node.be", new(1984, 9, 15), new(16, 20),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RolLid, Gender.Male),
            ("JFK Jr", "jfk.jr@demo.node.be", new(1960, 11, 25), new(0, 22),
                "Washington D.C., Verenigde Staten", 38.907192m, -77.036873m, false, RolLid, Gender.Male),
            ("Carolyn Bessette", "carolyn.bessette@demo.node.be", new(1966, 1, 7), new(8, 45),
                "New York, Verenigde Staten", 40.712776m, -74.005974m, false, RolLid, Gender.Female),
            // Enkel "California" opgegeven (een staat, geen plaats); aangevuld met Oakland,
            // haar gedocumenteerde geboortestad.
            ("Zendaya Coleman", "zendaya.coleman@demo.node.be", new(1996, 9, 1), new(18, 1),
                "Oakland, Californië, Verenigde Staten", 37.804363m, -122.271111m, false, RolLid, Gender.Female),
            ("Tom Holland", "tom.holland@demo.node.be", new(1996, 6, 1), new(12, 0),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, true, RolLid, Gender.Male),
            ("Taylor Swift", "taylor.swift@demo.node.be", new(1989, 12, 13), new(8, 36),
                "Reading (Pennsylvania), Verenigde Staten", 40.335560m, -75.926880m, false, RolLid, Gender.Female),
            // Geen land opgegeven; Westlake, Ohio is haar/zijn gedocumenteerde geboorteplaats.
            ("Travis Kelce", "travis.kelce@demo.node.be", new(1989, 10, 5), new(5, 49),
                "Westlake (Ohio), Verenigde Staten", 41.458401m, -81.918404m, false, RolLid, Gender.Male),
            ("Barack Obama", "barack.obama@demo.node.be", new(1961, 8, 4), new(19, 24),
                "Honolulu, Verenigde Staten", 21.306944m, -157.858337m, false, RolLid, Gender.Male),
            ("Michelle Obama", "michelle.obama@demo.node.be", new(1964, 1, 17), new(12, 0),
                "Chicago, Verenigde Staten", 41.878113m, -87.629799m, true, RolLid, Gender.Female),
            ("George Clooney", "george.clooney@demo.node.be", new(1961, 5, 6), new(2, 58),
                "Lexington (Kentucky), Verenigde Staten", 38.040585m, -84.503716m, false, RolLid, Gender.Male),
            ("Amal Clooney", "amal.clooney@demo.node.be", new(1978, 2, 3), new(12, 0),
                "Beiroet, Libanon", 33.893891m, 35.501801m, true, RolLid, Gender.Female),
            ("Prince William", "prince.william@demo.node.be", new(1982, 6, 21), new(21, 3),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RolLid, Gender.Male),
            ("Kate Middleton", "kate.middleton@demo.node.be", new(1982, 1, 9), new(19, 0),
                "Reading, Engeland", 51.454264m, -0.978180m, false, RolLid, Gender.Female),
            ("JFK", "jfk@demo.node.be", new(1917, 5, 29), new(15, 0),
                "Brookline (Massachusetts), Verenigde Staten", 42.331798m, -71.121269m, false, RolLid, Gender.Male),
            ("Marilyn Monroe", "marilyn.monroe@demo.node.be", new(1926, 6, 1), new(9, 30),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RolLid, Gender.Female),
            ("Jackie Kennedy", "jackie.kennedy@demo.node.be", new(1929, 7, 28), new(14, 30),
                "New York, Verenigde Staten", 40.712776m, -74.005974m, false, RolLid, Gender.Female),
            ("Ben Affleck", "ben.affleck@demo.node.be", new(1972, 8, 15), new(2, 53),
                "Berkeley (Californië), Verenigde Staten", 37.871593m, -122.272743m, false, RolLid, Gender.Male),
            ("Jennifer Lopez", "jennifer.lopez@demo.node.be", new(1969, 7, 24), new(12, 0),
                "The Bronx (New York), Verenigde Staten", 40.844782m, -73.864827m, true, RolLid, Gender.Female),
            ("Brad Pitt", "brad.pitt@demo.node.be", new(1963, 12, 18), new(6, 31),
                "Shawnee (Oklahoma), Verenigde Staten", 35.327332m, -96.925285m, false, RolLid, Gender.Male),
            ("Jennifer Aniston", "jennifer.aniston@demo.node.be", new(1969, 2, 11), new(22, 22),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RolLid, Gender.Female),
            ("Angelina Jolie", "angelina.jolie@demo.node.be", new(1975, 6, 4), new(9, 9),
                "Los Angeles, Verenigde Staten", 34.052235m, -118.243683m, false, RolLid, Gender.Female),
            ("King Charles", "king.charles@demo.node.be", new(1948, 11, 14), new(21, 14),
                "Londen, Verenigd Koninkrijk", 51.507351m, -0.127758m, false, RolLid, Gender.Male),
            ("Princess Diana", "princess.diana@demo.node.be", new(1961, 7, 1), new(19, 45),
                "Sandringham, Verenigd Koninkrijk", 52.834721m, 0.505600m, false, RolLid, Gender.Female),
        };

        var resultaat = new List<ApplicationUser>();

        foreach (var d in definities)
        {
            var bestaande = await userManager.FindByEmailAsync(d.Email);
            if (bestaande is not null)
            {
                // Backfill accounts from a seeding run before the gender field
                // existed, so the swipe deck works for them too.
                if (bestaande.Gender != d.Gender)
                {
                    bestaande.Gender = d.Gender;
                    await userManager.UpdateAsync(bestaande);
                }

                resultaat.Add(bestaande);
                continue;
            }

            var gebruiker = new ApplicationUser
            {
                UserName = d.Email,
                Email = d.Email,
                EmailConfirmed = true, // Demo-accounts slaan de e-mailverificatie over.
                DisplayName = d.Naam,
                BirthDate = d.Datum,
                BirthTime = d.Tijd,
                BirthTimeIsUnknown = d.TijdOnbekend,
                BirthPlace = d.Plaats,
                BirthLatitude = d.Lat,
                BirthLongitude = d.Lng,
                Gender = d.Gender,
                CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            };

            var aangemaakt = await userManager.CreateAsync(gebruiker, DemoWachtwoord);
            if (!aangemaakt.Succeeded)
            {
                logger.LogError("Seeden van gebruiker {Email} mislukt: {Fouten}",
                    d.Email, string.Join(", ", aangemaakt.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(gebruiker, d.Rol);
            resultaat.Add(gebruiker);
        }

        return resultaat;
    }

    /// <summary>
    /// Sets a partner preference for every demo user: the opposite gender,
    /// consistent with the seeded couples below (all man-woman).
    /// </summary>
    private static async Task SeedPartnerPreferencesAsync(ApplicationDbContext context, List<ApplicationUser> gebruikers)
    {
        if (await context.PartnerPreferences.AnyAsync())
        {
            return; // Preferences already exist: nothing to do.
        }

        foreach (var gebruiker in gebruikers)
        {
            var oppositeGender = gebruiker.Gender == Gender.Male ? Gender.Female : Gender.Male;
            context.PartnerPreferences.Add(new PartnerPreference { UserId = gebruiker.Id, Gender = oppositeGender });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Berekent en bewaart voor elk lid de echte geboortehoroscoop (Swiss
    /// Ephemeris) met alle posities (Zon t.e.m. Pluto plus Ascendant).
    /// </summary>
    private static async Task SeedHoroscopenAsync(
        ApplicationDbContext context, List<ApplicationUser> gebruikers, INatalChartCalculator calculator)
    {
        if (await context.NatalCharts.AnyAsync())
        {
            return; // Horoscopen bestaan al: niets te doen.
        }

        foreach (var gebruiker in gebruikers)
        {
            var horoscoop = calculator.Calculate(gebruiker);
            context.NatalCharts.Add(horoscoop);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seedt wederzijdse likes (die matches opleveren) enkel voor paren die
    /// een gedocumenteerde echte relatie hebben (huwelijk of publieke relatie).
    /// Er wordt bewust geen eenzijdige interesse of afwijzing tussen specifieke
    /// publieke figuren verzonnen: Marilyn Monroe en Angelina Jolie blijven
    /// daarom ongekoppeld in deze demo, net als bij een echte swipe-app.
    /// </summary>
    private static async Task SeedSwipesEnMatchesAsync(
        ApplicationDbContext context, List<ApplicationUser> gebruikers, IMatchInterpretationService matchInterpretationService)
    {
        if (await context.Swipes.AnyAsync())
        {
            return;
        }

        var perEmail = gebruikers.ToDictionary(g => g.Email!, g => g);

        // Wederzijdse likes: gedocumenteerde koppels, worden matches.
        var wederzijds = new[]
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

        var tijdstip = new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc);

        foreach (var (a, b) in wederzijds)
        {
            var userA = perEmail[a];
            var userB = perEmail[b];

            context.Swipes.Add(new Swipe { SwiperUserId = userA.Id, TargetUserId = userB.Id, IsLike = true, CreatedAt = tijdstip });
            context.Swipes.Add(new Swipe { SwiperUserId = userB.Id, TargetUserId = userA.Id, IsLike = true, CreatedAt = tijdstip.AddHours(2) });

            // Afspraak: User1Id alfabetisch vóór User2Id zodat een paar uniek is.
            var (eerste, tweede) = string.CompareOrdinal(userA.Id, userB.Id) < 0 ? (userA, userB) : (userB, userA);

            var chartA = await context.NatalCharts.Include(n => n.Placements).FirstAsync(n => n.UserId == eerste.Id);
            var chartB = await context.NatalCharts.Include(n => n.Placements).FirstAsync(n => n.UserId == tweede.Id);
            var (score, _) = DemoSynastrie.Bereken(chartA, chartB);

            // De uitlegtekst komt van Claude op basis van de volledige horoscopen;
            // de score zelf blijft hierboven deterministisch berekend.
            var uitleg = await matchInterpretationService.SchrijfInterpretatieAsync(eerste, chartA, tweede, chartB, score, "nl");

            context.Matches.Add(new Match
            {
                User1Id = eerste.Id,
                User2Id = tweede.Id,
                CompatibilityScore = score,
                CompatibilityExplanation = uitleg,
                CompatibilityExplanationLanguage = "nl",
                Status = MatchStatus.Active,
                MatchedAt = tijdstip.AddHours(2),
            });

            tijdstip = tijdstip.AddDays(1);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seedt twee meldingen zodat het moderatiescherm data heeft. De reden is
    /// bewust neutrale testtekst (geen echte klacht) om geen gedrag toe te
    /// schrijven aan de publieke figuren die als demogebruiker dienen.
    /// </summary>
    private static async Task SeedMeldingenAsync(ApplicationDbContext context, List<ApplicationUser> gebruikers)
    {
        if (await context.Reports.AnyAsync())
        {
            return;
        }

        var perEmail = gebruikers.ToDictionary(g => g.Email!, g => g);

        context.Reports.Add(new Report
        {
            ReporterUserId = perEmail["marilyn.monroe@demo.node.be"].Id,
            ReportedUserId = perEmail["angelina.jolie@demo.node.be"].Id,
            Reason = "Testmelding voor de moderatiedemo (geen echte klacht).",
            IsResolved = false,
            CreatedAt = new DateTime(2026, 2, 20, 9, 30, 0, DateTimeKind.Utc),
        });

        context.Reports.Add(new Report
        {
            ReporterUserId = perEmail["jackie.kennedy@demo.node.be"].Id,
            ReportedUserId = perEmail["ben.affleck@demo.node.be"].Id,
            Reason = "Testmelding voor de moderatiedemo (geen echte klacht).",
            IsResolved = true,
            CreatedAt = new DateTime(2026, 2, 25, 18, 45, 0, DateTimeKind.Utc),
        });

        await context.SaveChangesAsync();
    }
}
