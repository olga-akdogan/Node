using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Node.Data.Models;
using Node.Data.Models.Enums;

namespace Node.Data.Data;

/// <summary>
/// Uitgebreide seeding bij het opstarten van een (gedeeltelijk) lege databank.
/// Alles is deterministisch zodat elke run dezelfde
/// demogegevens oplevert: rollen, gebruikers, horoscopen met posities,
/// swipes, matches, chatberichten en meldingen.
///
/// De zontekens worden correct afgeleid uit de geboortedatum; de maan-,
/// ascendant- en huisposities zijn voorlopige demowaarden totdat de echte
/// berekening met SwissEphNet gebouwd is (fase "astrologie-engine").
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
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Openstaande migraties automatisch toepassen zodat de databank
        // altijd de juiste structuur heeft bij het opstarten.
        await context.Database.MigrateAsync();

        await SeedRollenAsync(roleManager);
        var gebruikers = await SeedGebruikersAsync(userManager, logger);
        await SeedHoroscopenAsync(context, gebruikers);
        await SeedSwipesEnMatchesAsync(context, gebruikers);
        await SeedChatberichtenAsync(context);
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
    /// Maakt de demo-accounts aan: één beheerder, één moderator en twaalf leden.
    /// E-mail staat meteen op bevestigd zodat de demo-accounts kunnen inloggen
    /// (echte registraties moeten hun e-mail wél eerst bevestigen).
    /// </summary>
    private static async Task<List<ApplicationUser>> SeedGebruikersAsync(
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        // (naam, e-mail, geboortedatum, geboortetijd, plaats, lat, lng, bio, rol)
        var definities = new (string Naam, string Email, DateOnly Datum, TimeOnly Tijd,
            string Plaats, decimal Lat, decimal Lng, string? Bio, string Rol)[]
        {
            ("Beheerder", "admin@node.be", new(1990, 1, 15), new(8, 30), "Brussel, België", 50.8503m, 4.3517m,
                null, RolAdmin),
            ("Mo de Moderator", "moderator@node.be", new(1992, 6, 21), new(14, 0), "Gent, België", 51.0543m, 3.7174m,
                null, RolModerator),
            ("Luna", "luna@demo.node.be", new(1998, 3, 12), new(3, 45), "Antwerpen, België", 51.2194m, 4.4025m,
                "Vissen met een zwak voor oude sterrenkaarten en nachtwandelingen.", RolLid),
            ("Ceder", "ceder@demo.node.be", new(1996, 7, 30), new(12, 15), "Leuven, België", 50.8798m, 4.7005m,
                "Leeuw. Ik zoek iemand die mijn vuur kan volgen.", RolLid),
            ("Yildiz", "yildiz@demo.node.be", new(1999, 11, 2), new(22, 10), "Brussel, België", 50.8503m, 4.3517m,
                "Schorpioen, dus vraag me niet naar mijn geheimen op de eerste date.", RolLid),
            ("Stella", "stella@demo.node.be", new(1995, 5, 4), new(6, 50), "Brugge, België", 51.2093m, 3.2247m,
                "Stier: goed eten, lange wandelingen en geen drama.", RolLid),
            ("Orion", "orion@demo.node.be", new(1997, 12, 19), new(17, 5), "Hasselt, België", 50.9307m, 5.3378m,
                "Boogschutter met een caravan en te veel reisplannen.", RolLid),
            ("Nova", "nova@demo.node.be", new(2000, 2, 8), new(9, 40), "Oostende, België", 51.2154m, 2.9286m,
                "Waterman. Ik geloof niet in astrologie, maar hier ben ik.", RolLid),
            ("Kastor", "kastor@demo.node.be", new(1994, 6, 3), new(11, 20), "Mechelen, België", 51.0259m, 4.4776m,
                "Tweelingen: twee dates voor de prijs van één gesprek.", RolLid),
            ("Vega", "vega@demo.node.be", new(1993, 9, 14), new(4, 55), "Kortrijk, België", 50.8285m, 3.2649m,
                "Maagd, maar mijn bureau is een chaos. Niemand is perfect.", RolLid),
            ("Ariel", "ariel@demo.node.be", new(2001, 4, 2), new(15, 30), "Genk, België", 50.9654m, 5.5006m,
                "Ram. Eerst doen, dan denken, dan sorry zeggen.", RolLid),
            ("Selene", "selene@demo.node.be", new(1998, 7, 7), new(1, 25), "Namen, België", 50.4674m, 4.8720m,
                "Kreeft: ik onthoud alles wat je zegt, ook het lieve.", RolLid),
            ("Altair", "altair@demo.node.be", new(1996, 10, 12), new(19, 45), "Luik, België", 50.6326m, 5.5797m,
                "Weegschaal op zoek naar evenwicht (en goede koffie).", RolLid),
            ("Mira", "mira@demo.node.be", new(1999, 1, 3), new(7, 10), "Turnhout, België", 51.3226m, 4.9447m,
                "Steenbok: ambitieus, loyaal en altijd vijf minuten te vroeg.", RolLid),
        };

        var resultaat = new List<ApplicationUser>();

        foreach (var d in definities)
        {
            var bestaande = await userManager.FindByEmailAsync(d.Email);
            if (bestaande is not null)
            {
                resultaat.Add(bestaande);
                continue;
            }

            var gebruiker = new ApplicationUser
            {
                UserName = d.Email,
                Email = d.Email,
                EmailConfirmed = true, // Demo-accounts slaan de e-mailverificatie over.
                DisplayName = d.Naam,
                Bio = d.Bio,
                BirthDate = d.Datum,
                BirthTime = d.Tijd,
                BirthPlace = d.Plaats,
                BirthLatitude = d.Lat,
                BirthLongitude = d.Lng,
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
    /// Maakt voor elk lid een geboortehoroscoop met elf posities
    /// (Zon t.e.m. Pluto plus Ascendant).
    /// </summary>
    private static async Task SeedHoroscopenAsync(ApplicationDbContext context, List<ApplicationUser> gebruikers)
    {
        if (await context.NatalCharts.AnyAsync())
        {
            return; // Horoscopen bestaan al: niets te doen.
        }

        foreach (var gebruiker in gebruikers)
        {
            var zonneteken = BepaalZonneteken(gebruiker.BirthDate);

            // Deterministische demowaarden per gebruiker: dezelfde invoer geeft
            // altijd dezelfde "horoscoop". Wordt vervangen door SwissEphNet.
            var zaad = gebruiker.BirthDate.DayNumber + gebruiker.BirthTime.Hour;
            var maanteken = (ZodiacSign)((zaad * 7) % 12);
            var ascendant = (ZodiacSign)((zaad * 11 + gebruiker.BirthTime.Minute) % 12);

            var horoscoop = new NatalChart
            {
                UserId = gebruiker.Id,
                // Demo: België is UTC+1 in de winter, +2 in de zomer; de echte
                // historische tijdzone-omrekening volgt in de astrologie-fase.
                BirthMomentUtc = gebruiker.BirthDate
                    .ToDateTime(gebruiker.BirthTime, DateTimeKind.Unspecified)
                    .AddHours(-1),
                SunSign = zonneteken,
                MoonSign = maanteken,
                AscendantSign = ascendant,
                CalculatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            };

            var lichamen = Enum.GetValues<CelestialBody>();
            for (var i = 0; i < lichamen.Length; i++)
            {
                var lichaam = lichamen[i];
                horoscoop.Placements.Add(new Placement
                {
                    Body = lichaam,
                    Sign = lichaam switch
                    {
                        CelestialBody.Sun => zonneteken,
                        CelestialBody.Moon => maanteken,
                        CelestialBody.Ascendant => ascendant,
                        _ => (ZodiacSign)((zaad * (i + 3)) % 12),
                    },
                    House = (zaad * (i + 5)) % 12 + 1,
                    DegreeInSign = (zaad * (i + 7)) % 30 + 0.5m,
                });
            }

            context.NatalCharts.Add(horoscoop);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Bepaalt het echte zonneteken op basis van de geboortedatum
    /// (klassieke tropische datumgrenzen).
    /// </summary>
    private static ZodiacSign BepaalZonneteken(DateOnly datum)
    {
        return (datum.Month, datum.Day) switch
        {
            (3, >= 21) or (4, <= 19) => ZodiacSign.Aries,
            (4, >= 20) or (5, <= 20) => ZodiacSign.Taurus,
            (5, >= 21) or (6, <= 20) => ZodiacSign.Gemini,
            (6, >= 21) or (7, <= 22) => ZodiacSign.Cancer,
            (7, >= 23) or (8, <= 22) => ZodiacSign.Leo,
            (8, >= 23) or (9, <= 22) => ZodiacSign.Virgo,
            (9, >= 23) or (10, <= 22) => ZodiacSign.Libra,
            (10, >= 23) or (11, <= 21) => ZodiacSign.Scorpio,
            (11, >= 22) or (12, <= 21) => ZodiacSign.Sagittarius,
            (12, >= 22) or (1, <= 19) => ZodiacSign.Capricorn,
            (1, >= 20) or (2, <= 18) => ZodiacSign.Aquarius,
            _ => ZodiacSign.Pisces,
        };
    }

    /// <summary>
    /// Seedt een geloofwaardige swipe-geschiedenis: een aantal wederzijdse likes
    /// (die matches opleveren), enkele eenzijdige likes en enkele passes.
    /// </summary>
    private static async Task SeedSwipesEnMatchesAsync(ApplicationDbContext context, List<ApplicationUser> gebruikers)
    {
        if (await context.Swipes.AnyAsync())
        {
            return;
        }

        var perEmail = gebruikers.ToDictionary(g => g.Email!, g => g);

        // Wederzijdse likes: deze paren worden matches.
        var wederzijds = new[]
        {
            ("luna@demo.node.be", "orion@demo.node.be"),
            ("ceder@demo.node.be", "ariel@demo.node.be"),
            ("yildiz@demo.node.be", "selene@demo.node.be"),
            ("kastor@demo.node.be", "altair@demo.node.be"),
            ("stella@demo.node.be", "vega@demo.node.be"),
        };

        // Eenzijdige likes: nog geen match.
        var eenzijdig = new[]
        {
            ("nova@demo.node.be", "luna@demo.node.be"),
            ("mira@demo.node.be", "ceder@demo.node.be"),
            ("orion@demo.node.be", "yildiz@demo.node.be"),
        };

        // Passes: uitdrukkelijk geen interesse.
        var passes = new[]
        {
            ("luna@demo.node.be", "kastor@demo.node.be"),
            ("vega@demo.node.be", "nova@demo.node.be"),
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

            var chartA = await context.NatalCharts.FirstAsync(n => n.UserId == eerste.Id);
            var chartB = await context.NatalCharts.FirstAsync(n => n.UserId == tweede.Id);
            var (score, uitleg) = DemoSynastrie.Bereken(chartA, chartB);

            context.Matches.Add(new Match
            {
                User1Id = eerste.Id,
                User2Id = tweede.Id,
                CompatibilityScore = score,
                CompatibilityExplanation = uitleg,
                Status = MatchStatus.Active,
                MatchedAt = tijdstip.AddHours(2),
            });

            tijdstip = tijdstip.AddDays(1);
        }

        foreach (var (a, b) in eenzijdig)
        {
            context.Swipes.Add(new Swipe { SwiperUserId = perEmail[a].Id, TargetUserId = perEmail[b].Id, IsLike = true, CreatedAt = tijdstip });
            tijdstip = tijdstip.AddHours(5);
        }

        foreach (var (a, b) in passes)
        {
            context.Swipes.Add(new Swipe { SwiperUserId = perEmail[a].Id, TargetUserId = perEmail[b].Id, IsLike = false, CreatedAt = tijdstip });
            tijdstip = tijdstip.AddHours(5);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Seedt een kort chatgesprek in elke match.</summary>
    private static async Task SeedChatberichtenAsync(ApplicationDbContext context)
    {
        if (await context.ChatMessages.AnyAsync())
        {
            return;
        }

        var matches = await context.Matches.ToListAsync();
        var openers = new[]
        {
            "Hey! Blijkbaar vinden de sterren ons een goed idee.",
            "Onze score is indrukwekkend. Zullen we dat eens testen met koffie?",
            "Hallo! Wat zegt jouw ascendant over eerste berichtjes sturen?",
            "De planeten hebben gesproken, dus hier ben ik.",
            "Match! Ik beloof dat ik maar drie keer per dag over astrologie begin.",
        };
        var antwoorden = new[]
        {
            "Haha, wie ben ik om de sterren tegen te spreken?",
            "Koffie klinkt goed. Ik ken een plek met een terras op het zuiden.",
            "Mijn ascendant zegt: eerst zien of je grappig bent.",
            "Dan moeten we de planeten maar niet teleurstellen.",
            "Drie keer per dag valt mee, mijn vorige match haalde vijf.",
        };

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            context.ChatMessages.Add(new ChatMessage
            {
                MatchId = match.Id,
                SenderUserId = match.User1Id,
                Content = openers[i % openers.Length],
                SentAt = match.MatchedAt.AddHours(1),
                IsRead = true,
            });
            context.ChatMessages.Add(new ChatMessage
            {
                MatchId = match.Id,
                SenderUserId = match.User2Id,
                Content = antwoorden[i % antwoorden.Length],
                SentAt = match.MatchedAt.AddHours(3),
                IsRead = i % 2 == 0, // Enkele berichten bewust ongelezen laten voor de demo.
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Seedt enkele meldingen zodat het moderatiescherm data heeft.</summary>
    private static async Task SeedMeldingenAsync(ApplicationDbContext context, List<ApplicationUser> gebruikers)
    {
        if (await context.Reports.AnyAsync())
        {
            return;
        }

        var perEmail = gebruikers.ToDictionary(g => g.Email!, g => g);

        context.Reports.Add(new Report
        {
            ReporterUserId = perEmail["vega@demo.node.be"].Id,
            ReportedUserId = perEmail["nova@demo.node.be"].Id,
            Reason = "Stuurt na een pass toch nog berichten via een ander kanaal.",
            IsResolved = false,
            CreatedAt = new DateTime(2026, 2, 20, 9, 30, 0, DateTimeKind.Utc),
        });

        context.Reports.Add(new Report
        {
            ReporterUserId = perEmail["selene@demo.node.be"].Id,
            ReportedUserId = perEmail["kastor@demo.node.be"].Id,
            Reason = "Profielfoto is duidelijk geen foto van hemzelf.",
            IsResolved = true,
            CreatedAt = new DateTime(2026, 2, 25, 18, 45, 0, DateTimeKind.Utc),
        });

        await context.SaveChangesAsync();
    }
}
