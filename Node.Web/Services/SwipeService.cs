using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;
using Node.Web.Models.Swiping;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// The swipe stack: finding candidates and processing likes/passes.
/// </summary>
public class SwipeService : ISwipeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchInterpretationService _matchInterpretationService;
    private readonly ISwipeTeaserService _swipeTeaserService;
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<SwipeService> _logger;

    public SwipeService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMatchInterpretationService matchInterpretationService,
        ISwipeTeaserService swipeTeaserService,
        INotificationService notificationService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<SwipeService> logger)
    {
        _context = context;
        _userManager = userManager;
        _matchInterpretationService = matchInterpretationService;
        _swipeTeaserService = swipeTeaserService;
        _notificationService = notificationService;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<SwipeCardViewModel?> GetNextCandidateAsync(string userId)
    {
        // Only regular members appear in the stack (no admins or moderators,
        // those accounts aren't dating profiles).
        var memberIds = (await _userManager.GetUsersInRoleAsync(DbSeeder.RoleMember))
            .Select(u => u.Id)
            .ToHashSet();

        // Anyone I already rated doesn't come up again.
        var ratedIds = await _context.Swipes
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

        var candidate = await _context.Users
            .Include(u => u.NatalChart).ThenInclude(n => n!.Placements)
            .Where(u => u.Id != userId
                        && !u.IsBlocked
                        && u.EmailConfirmed
                        && memberIds.Contains(u.Id)
                        && !ratedIds.Contains(u.Id)
                        && myPreferences.Contains(u.Gender)
                        && u.PartnerPreferences.Any(p => p.Gender == currentUser.Gender))
            .OrderBy(u => u.DisplayName) // Deterministic order for the demo.
            .FirstOrDefaultAsync();

        if (candidate is null)
        {
            return null; // Stack is empty.
        }

        // Score only calculable when both natal charts exist.
        var myChart = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == userId);
        (int Score, SynastryConclusion Conclusion)? synastry = myChart is not null && candidate.NatalChart is not null
            ? DemoSynastry.Calculate(myChart, candidate.NatalChart)
            : null;

        string? compatibilityTest = null;
        string? dateIdea = null;
        if (synastry is not null && myChart is not null && candidate.NatalChart is not null)
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            (compatibilityTest, dateIdea) = await _swipeTeaserService.WriteTeaserAsync(
                currentUser, myChart, candidate, candidate.NatalChart, synastry.Value.Score, language);
        }

        return new SwipeCardViewModel
        {
            UserId = candidate.Id,
            DisplayName = candidate.DisplayName,
            ProfilePictureUrl = candidate.ProfilePictureUrl,
            Age = CalculateAge(candidate.BirthDate),
            Bio = candidate.Bio,
            BirthPlace = candidate.BirthPlace,
            CompatibilityScore = synastry?.Score,
            CompatibilityExplanation = compatibilityTest,
            DateIdea = dateIdea,
        };
    }

    public async Task<(bool IsMatch, int? MatchId)> RateAsync(string swiperUserId, string targetUserId, bool isLike)
    {
        // Ignore duplicate ratings (e.g. clicking twice quickly).
        var alreadyExists = await _context.Swipes.AnyAsync(
            s => s.SwiperUserId == swiperUserId && s.TargetUserId == targetUserId);
        if (alreadyExists)
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

        await _context.SaveChangesAsync();
        await _notificationService.NotifyLikeAsync(recipientUserId: targetUserId, actorUserId: swiperUserId);

        // Mutual like? Then a match is born.
        var isMutual = await _context.Swipes.AnyAsync(
            s => s.SwiperUserId == targetUserId && s.TargetUserId == swiperUserId && s.IsLike);
        if (!isMutual)
        {
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // Convention: User1Id alphabetically before User2Id so a pair is unique.
        var (first, second) = string.CompareOrdinal(swiperUserId, targetUserId) < 0
            ? (swiperUserId, targetUserId)
            : (targetUserId, swiperUserId);

        var chart1 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == first);
        var chart2 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == second);

        var currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        int score;
        string explanation;
        if (chart1 is not null && chart2 is not null)
        {
            var (calculatedScore, _) = DemoSynastry.Calculate(chart1, chart2);
            score = calculatedScore;

            // The explanation text comes from Claude (based on the full natal charts);
            // the score itself stays deterministically calculated above.
            var user1 = await _userManager.FindByIdAsync(first);
            var user2 = await _userManager.FindByIdAsync(second);
            explanation = user1 is not null && user2 is not null
                ? await _matchInterpretationService.WriteMatchInterpretationAsync(user1, chart1, user2, chart2, score, currentLanguage)
                : _localizer["SwipeCard_ScorePending"];
        }
        else
        {
            score = 50;
            explanation = _localizer["SwipeCard_ScorePending"];
        }

        var match = new Match
        {
            User1Id = first,
            User2Id = second,
            CompatibilityScore = score,
            CompatibilityExplanation = explanation,
            CompatibilityExplanationLanguage = currentLanguage,
            Status = MatchStatus.Active,
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New match {MatchId} between {User1} and {User2}.",
            match.Id, first, second);

        return (true, match.Id);
    }

    /// <summary>Calculates the age in full years from the birth date.</summary>
    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--; // This year's birthday hasn't happened yet.
        }

        return age;
    }
}
