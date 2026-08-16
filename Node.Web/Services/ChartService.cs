using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Services;
using Node.Web.Models.Chart;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Assembles the natal chart page from the saved NatalChart and Placements.
/// </summary>
public class ChartService : IChartService
{
    private readonly ApplicationDbContext _context;
    private readonly IChartInterpretationService _chartInterpretationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ChartService(
        ApplicationDbContext context,
        IChartInterpretationService chartInterpretationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _chartInterpretationService = chartInterpretationService;
        _localizer = localizer;
    }

    public async Task<HoroscopeViewModel?> GetHoroscopeAsync(string userId)
    {
        var chart = await _context.NatalCharts
            .Include(n => n.User)
            .Include(n => n.Placements)
            .FirstOrDefaultAsync(n => n.UserId == userId);

        if (chart?.User is null)
        {
            return null; // No chart calculated yet (e.g. a brand-new account).
        }

        // Date formatting follows the language the user chose (language
        // picker), not a fixed Belgian-Dutch culture — so the page also
        // stays consistent in date format when viewed in English or French.
        var culture = CultureInfo.CurrentUICulture;
        var currentLanguage = culture.TwoLetterISOLanguageName;

        // No interpretation yet, or written earlier in a different language
        // than the current selection: (re)request it from Claude and save it.
        if (chart.InterpretationText is null || chart.InterpretationLanguage != currentLanguage)
        {
            var (interpretation, partnerPreferenceText) =
                await _chartInterpretationService.WriteInterpretationAsync(chart.User, chart, currentLanguage);

            chart.InterpretationText = interpretation;
            chart.PartnerLookingForText = partnerPreferenceText;
            chart.InterpretationLanguage = currentLanguage;
            await _context.SaveChangesAsync();
        }

        return new HoroscopeViewModel
        {
            DisplayName = chart.User.DisplayName,
            BirthInfo = string.Join(" · ",
                chart.User.BirthDate.ToString("d MMM yyyy", culture),
                chart.User.BirthTime.ToString("HH:mm", culture),
                chart.User.BirthPlace),
            SunSign = chart.SunSign,
            MoonSign = chart.MoonSign,
            AscendantSign = chart.AscendantSign,
            AscendantIsApproximate = chart.AscendantIsApproximate,
            Placements = chart.Placements
                .OrderBy(p => p.Body) // Fixed order: Sun, Moon, ... Ascendant.
                .Select(p => new PlacementRow
                {
                    Body = p.Body,
                    Sign = p.Sign,
                    House = p.House,
                    DegreeText = FormatDegree(p.DegreeInSign),
                    AbsoluteDegrees = (int)p.Sign * 30 + (double)p.DegreeInSign,
                })
                .ToList(),
            Signature = AstroDisplay.Signature(chart.SunSign, chart.MoonSign, _localizer),
            Interpretation = chart.InterpretationText,
            PartnerPreferenceText = chart.PartnerLookingForText,
        };
    }

    /// <summary>Converts 14.5° to the classic notation "14°30′".</summary>
    private static string FormatDegree(decimal degreeInSign)
    {
        var degrees = (int)degreeInSign;
        var minutes = (int)Math.Round((degreeInSign - degrees) * 60);
        if (minutes == 60)
        {
            degrees++;
            minutes = 0;
        }

        return $"{degrees:00}°{minutes:00}′";
    }
}
