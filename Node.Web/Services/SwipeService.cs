using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;
using Node.Web.Models.Swiping;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// De swipe-stapel: kandidaten zoeken en likes/passes verwerken.
/// </summary>
public class SwipeService : ISwipeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchInterpretationService _matchInterpretationService;
    private readonly ILogger<SwipeService> _logger;

    public SwipeService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMatchInterpretationService matchInterpretationService,
        ILogger<SwipeService> logger)
    {
        _context = context;
        _userManager = userManager;
        _matchInterpretationService = matchInterpretationService;
        _logger = logger;
    }

    public async Task<SwipeCardViewModel?> GetVolgendeKandidaatAsync(string userId)
    {
        // Alleen gewone leden verschijnen in de stapel (geen beheerders of
        // moderatoren, die accounts zijn geen datingprofielen).
        var ledenIds = (await _userManager.GetUsersInRoleAsync(DbSeeder.RolLid))
            .Select(u => u.Id)
            .ToHashSet();

        // Wie ik al beoordeeld heb, komt niet opnieuw langs.
        var beoordeeldeIds = await _context.Swipes
            .Where(s => s.SwiperUserId == userId)
            .Select(s => s.TargetUserId)
            .ToListAsync();

        // Mutual gender preference: I only see candidates of a gender I'm
        // interested in, who are themselves interested in my gender too.
        var currentUser = await _context.Users
            .Include(u => u.PartnerPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (currentUser is null)
        {
            return null;
        }

        var myPreferences = currentUser.PartnerPreferences.Select(p => p.Gender).ToHashSet();

        var kandidaat = await _context.Users
            .Include(u => u.NatalChart)
            .Where(u => u.Id != userId
                        && !u.IsBlocked
                        && u.EmailConfirmed
                        && ledenIds.Contains(u.Id)
                        && !beoordeeldeIds.Contains(u.Id)
                        && myPreferences.Contains(u.Gender)
                        && u.PartnerPreferences.Any(p => p.Gender == currentUser.Gender))
            .OrderBy(u => u.DisplayName) // Deterministische volgorde voor de demo.
            .FirstOrDefaultAsync();

        if (kandidaat is null)
        {
            return null; // Stapel is leeg.
        }

        // Score alleen berekenbaar wanneer beide horoscopen bestaan.
        var mijnChart = await _context.NatalCharts.FirstOrDefaultAsync(n => n.UserId == userId);
        (int Score, string Uitleg)? synastrie = mijnChart is not null && kandidaat.NatalChart is not null
            ? DemoSynastrie.Bereken(mijnChart, kandidaat.NatalChart)
            : null;

        return new SwipeCardViewModel
        {
            UserId = kandidaat.Id,
            DisplayName = kandidaat.DisplayName,
            Age = BerekenLeeftijd(kandidaat.BirthDate),
            Bio = kandidaat.Bio,
            BirthPlace = kandidaat.BirthPlace,
            SunSign = kandidaat.NatalChart?.SunSign.ToString(),
            MoonSign = kandidaat.NatalChart?.MoonSign.ToString(),
            AscendantSign = kandidaat.NatalChart?.AscendantSign.ToString(),
            CompatibilityScore = synastrie?.Score,
            CompatibilityExplanation = synastrie?.Uitleg,
        };
    }

    public async Task<(bool IsMatch, int? MatchId)> BeoordeelAsync(string swiperUserId, string targetUserId, bool isLike)
    {
        // Dubbele beoordelingen negeren (bv. twee keer snel klikken).
        var bestaat = await _context.Swipes.AnyAsync(
            s => s.SwiperUserId == swiperUserId && s.TargetUserId == targetUserId);
        if (bestaat)
        {
            return (false, null);
        }

        _context.Swipes.Add(new Swipe
        {
            SwiperUserId = swiperUserId,
            TargetUserId = targetUserId,
            IsLike = isLike,
        });

        if (!isLike)
        {
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // Wederzijdse like? Dan ontstaat er een match.
        var wederzijds = await _context.Swipes.AnyAsync(
            s => s.SwiperUserId == targetUserId && s.TargetUserId == swiperUserId && s.IsLike);
        if (!wederzijds)
        {
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // Afspraak: User1Id alfabetisch vóór User2Id zodat een paar uniek is.
        var (eerste, tweede) = string.CompareOrdinal(swiperUserId, targetUserId) < 0
            ? (swiperUserId, targetUserId)
            : (targetUserId, swiperUserId);

        var chart1 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == eerste);
        var chart2 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == tweede);

        var currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        int score;
        string uitleg;
        if (chart1 is not null && chart2 is not null)
        {
            var (berekendeScore, _) = DemoSynastrie.Bereken(chart1, chart2);
            score = berekendeScore;

            // De uitlegtekst komt van Claude (op basis van de volledige horoscopen);
            // de score zelf blijft deterministisch berekend hierboven.
            var gebruiker1 = await _userManager.FindByIdAsync(eerste);
            var gebruiker2 = await _userManager.FindByIdAsync(tweede);
            uitleg = gebruiker1 is not null && gebruiker2 is not null
                ? await _matchInterpretationService.SchrijfInterpretatieAsync(gebruiker1, chart1, gebruiker2, chart2, score, currentLanguage)
                : "Score volgt zodra beide horoscopen berekend zijn.";
        }
        else
        {
            score = 50;
            uitleg = "Score volgt zodra beide horoscopen berekend zijn.";
        }

        var match = new Match
        {
            User1Id = eerste,
            User2Id = tweede,
            CompatibilityScore = score,
            CompatibilityExplanation = uitleg,
            CompatibilityExplanationLanguage = currentLanguage,
            Status = MatchStatus.Active,
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nieuwe match {MatchId} tussen {User1} en {User2}.",
            match.Id, eerste, tweede);

        return (true, match.Id);
    }

    /// <summary>Berekent de leeftijd in volle jaren uit de geboortedatum.</summary>
    private static int BerekenLeeftijd(DateOnly geboortedatum)
    {
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        var leeftijd = vandaag.Year - geboortedatum.Year;
        if (geboortedatum > vandaag.AddYears(-leeftijd))
        {
            leeftijd--; // Verjaardag van dit jaar is nog niet voorbij.
        }

        return leeftijd;
    }
}
