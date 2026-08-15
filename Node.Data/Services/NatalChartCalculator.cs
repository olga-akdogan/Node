using GeoTimeZone;
using NodaTime;
using Node.Data.Models;
using Node.Data.Models.Enums;
using SwissEphNet;

namespace Node.Data.Services;

/// <summary>
/// Berekent de geboortehoroscoop met de Swiss Ephemeris (Moshier-model, geen
/// externe ephemeris-bestanden nodig). Rekent eerst de lokale geboortetijd om
/// naar UT met de historische tijdzone van de geboorteplaats (via de
/// coördinaten), en berekent dan de posities van zon t.e.m. Pluto plus de
/// Ascendant. De huizen volgen het hele-teken-systeem (huis 1 = het teken van
/// de Ascendant, huis 2 = het volgende teken, enz.) zodat elk huis exact 30°
/// beslaat en de indeling eenvoudig en uitlegbaar blijft.
/// </summary>
public class NatalChartCalculator : INatalChartCalculator
{
    /// <summary>Hemellichamen in de volgorde waarin Swiss Ephemeris ze identificeert.</summary>
    private static readonly (CelestialBody Body, int SweId)[] Planeten =
    [
        (CelestialBody.Sun, SwissEph.SE_SUN),
        (CelestialBody.Moon, SwissEph.SE_MOON),
        (CelestialBody.Mercury, SwissEph.SE_MERCURY),
        (CelestialBody.Venus, SwissEph.SE_VENUS),
        (CelestialBody.Mars, SwissEph.SE_MARS),
        (CelestialBody.Jupiter, SwissEph.SE_JUPITER),
        (CelestialBody.Saturn, SwissEph.SE_SATURN),
        (CelestialBody.Uranus, SwissEph.SE_URANUS),
        (CelestialBody.Neptune, SwissEph.SE_NEPTUNE),
        (CelestialBody.Pluto, SwissEph.SE_PLUTO),
    ];

    public NatalChart Calculate(ApplicationUser user)
    {
        if (user.BirthLatitude is null || user.BirthLongitude is null)
        {
            throw new InvalidOperationException(
                $"Kan geen horoscoop berekenen voor gebruiker {user.Id}: geboortecoördinaten ontbreken.");
        }

        var lat = (double)user.BirthLatitude.Value;
        var lng = (double)user.BirthLongitude.Value;

        var birthMomentUtc = LokaleTijdNaarUtc(user.BirthDate, user.BirthTime, lat, lng);

        using var sw = new SwissEph();
        var flags = SwissEph.SEFLG_MOSEPH | SwissEph.SEFLG_SPEED;

        var jdUt = sw.swe_julday(
            birthMomentUtc.Year, birthMomentUtc.Month, birthMomentUtc.Day,
            birthMomentUtc.Hour + birthMomentUtc.Minute / 60.0 + birthMomentUtc.Second / 3600.0,
            SwissEph.SE_GREG_CAL);

        // Enkel de Ascendant zelf is nodig (ascmc[0], huissysteem 'P' hiervoor
        // genegeerd); de huizen volgen daarna het hele-teken-systeem hieronder.
        var cusps = new double[13];
        var ascmc = new double[10];
        sw.swe_houses(jdUt, lat, lng, 'P', cusps, ascmc);
        var ascendantLongitude = ascmc[0];
        var ascendantTeken = GraadNaarTeken(ascendantLongitude);

        var horoscoop = new NatalChart
        {
            UserId = user.Id,
            BirthMomentUtc = birthMomentUtc,
            AscendantSign = ascendantTeken,
            AscendantIsApproximate = user.BirthTimeIsUnknown,
            CalculatedAt = DateTime.UtcNow,
        };

        horoscoop.Placements.Add(new Placement
        {
            Body = CelestialBody.Ascendant,
            Sign = horoscoop.AscendantSign,
            House = 1,
            DegreeInSign = GraadInTeken(ascendantLongitude),
        });

        foreach (var (lichaam, sweId) in Planeten)
        {
            var xx = new double[6];
            var foutmelding = "";
            sw.swe_calc_ut(jdUt, sweId, flags, xx, ref foutmelding);
            var lengtegraad = xx[0];

            var teken = GraadNaarTeken(lengtegraad);

            if (lichaam == CelestialBody.Sun)
            {
                horoscoop.SunSign = teken;
            }
            else if (lichaam == CelestialBody.Moon)
            {
                horoscoop.MoonSign = teken;
            }

            horoscoop.Placements.Add(new Placement
            {
                Body = lichaam,
                Sign = teken,
                House = BepaalHuis(teken, ascendantTeken),
                DegreeInSign = GraadInTeken(lengtegraad),
            });
        }

        return horoscoop;
    }

    /// <summary>
    /// Rekent de lokale geboortedatum/-tijd om naar UT, met de historische
    /// tijdzone van de geboorteplaats (afgeleid uit de coördinaten). Bij een
    /// dubbelzinnige of niet-bestaande lokale tijd (zomertijdovergang) wordt de
    /// meest waarschijnlijke interpretatie gebruikt in plaats van een crash.
    /// </summary>
    private static DateTime LokaleTijdNaarUtc(DateOnly datum, TimeOnly tijd, double lat, double lng)
    {
        var ianaId = TimeZoneLookup.GetTimeZone(lat, lng).Result;
        var tijdzone = DateTimeZoneProviders.Tzdb[ianaId];

        var lokaal = new LocalDateTime(datum.Year, datum.Month, datum.Day, tijd.Hour, tijd.Minute, tijd.Second);
        var zonedDateTime = lokaal.InZoneLeniently(tijdzone);

        return zonedDateTime.ToDateTimeUtc();
    }

    /// <summary>Zet een eclipticale lengtegraad (0-360°) om naar het dierenriemteken.</summary>
    private static ZodiacSign GraadNaarTeken(double lengtegraad)
    {
        var genormaliseerd = Normaliseer(lengtegraad);
        return (ZodiacSign)((int)(genormaliseerd / 30) % 12);
    }

    /// <summary>Positie binnen het teken zelf (0-30°).</summary>
    private static decimal GraadInTeken(double lengtegraad)
    {
        var genormaliseerd = Normaliseer(lengtegraad);
        return (decimal)(genormaliseerd % 30);
    }

    private static double Normaliseer(double graden) => ((graden % 360) + 360) % 360;

    /// <summary>
    /// Hele-teken-huis van een plaatsing: huis 1 is het teken van de
    /// Ascendant, en elk volgend teken (in dierenriemvolgorde) is het
    /// volgende huis. Bv. Ascendant in Leeuw: Maagd = huis 2, ... Kreeft = huis 12.
    /// </summary>
    private static int BepaalHuis(ZodiacSign teken, ZodiacSign ascendantTeken)
    {
        return (((int)teken - (int)ascendantTeken + 12) % 12) + 1;
    }
}
