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
/// Stelt de horoscooppagina samen uit de opgeslagen NatalChart en Placements.
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

    public async Task<HoroscoopViewModel?> GetHoroscoopAsync(string userId)
    {
        var chart = await _context.NatalCharts
            .Include(n => n.User)
            .Include(n => n.Placements)
            .FirstOrDefaultAsync(n => n.UserId == userId);

        if (chart?.User is null)
        {
            return null; // Nog geen horoscoop berekend (bv. vers account).
        }

        // Datumnotatie volgt de taal die de gebruiker koos (taalkiezer), niet
        // een vast Belgisch-Nederlandse cultuur — zo blijft de pagina ook
        // qua datumformaat consistent in het Engels of Frans.
        var cultuur = CultureInfo.CurrentUICulture;
        var currentLanguage = cultuur.TwoLetterISOLanguageName;

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

        return new HoroscoopViewModel
        {
            DisplayName = chart.User.DisplayName,
            GeboorteInfo = string.Join(" · ",
                chart.User.BirthDate.ToString("d MMM yyyy", cultuur),
                chart.User.BirthTime.ToString("HH:mm", cultuur),
                chart.User.BirthPlace),
            SunSign = chart.SunSign,
            MoonSign = chart.MoonSign,
            AscendantSign = chart.AscendantSign,
            AscendantIsApproximate = chart.AscendantIsApproximate,
            Placements = chart.Placements
                .OrderBy(p => p.Body) // Vaste volgorde: Zon, Maan, ... Ascendant.
                .Select(p => new PlaatsingRegel
                {
                    Body = p.Body,
                    Sign = p.Sign,
                    Huis = p.House,
                    Graad = FormatteerGraad(p.DegreeInSign),
                    LengteGraden = (int)p.Sign * 30 + (double)p.DegreeInSign,
                })
                .ToList(),
            Signatuur = AstroWeergave.Signatuur(chart.SunSign, chart.MoonSign, _localizer),
            Interpretation = chart.InterpretationText,
            PartnerPreferenceText = chart.PartnerLookingForText,
        };
    }

    /// <summary>Zet 14,5° om naar de klassieke notatie "14°30′".</summary>
    private static string FormatteerGraad(decimal graadInTeken)
    {
        var graden = (int)graadInTeken;
        var minuten = (int)Math.Round((graadInTeken - graden) * 60);
        if (minuten == 60)
        {
            graden++;
            minuten = 0;
        }

        return $"{graden:00}°{minuten:00}′";
    }
}
