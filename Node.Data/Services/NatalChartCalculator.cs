using GeoTimeZone;
using NodaTime;
using Node.Data.Models;
using Node.Data.Models.Enums;
using SwissEphNet;

namespace Node.Data.Services;

/// <summary>
/// Calculates the birth chart using the Swiss Ephemeris (Moshier model, no
/// external ephemeris files needed). First converts the local birth time to
/// UT using the historical timezone of the birth place (via the
/// coordinates), then calculates the positions of Sun through Pluto plus the
/// Ascendant. Houses follow the whole-sign system (house 1 = the Ascendant's
/// sign, house 2 = the next sign, etc.) so each house spans exactly 30° and
/// the layout stays simple and explainable.
/// </summary>
public class NatalChartCalculator : INatalChartCalculator
{
    /// <summary>Celestial bodies in the order Swiss Ephemeris identifies them.</summary>
    private static readonly (CelestialBody Body, int SweId)[] Planets =
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
                $"Cannot calculate a natal chart for user {user.Id}: birth coordinates are missing.");
        }

        var lat = (double)user.BirthLatitude.Value;
        var lng = (double)user.BirthLongitude.Value;

        var birthMomentUtc = LocalTimeToUtc(user.BirthDate, user.BirthTime, lat, lng);

        using var sw = new SwissEph();
        var flags = SwissEph.SEFLG_MOSEPH | SwissEph.SEFLG_SPEED;

        var jdUt = sw.swe_julday(
            birthMomentUtc.Year, birthMomentUtc.Month, birthMomentUtc.Day,
            birthMomentUtc.Hour + birthMomentUtc.Minute / 60.0 + birthMomentUtc.Second / 3600.0,
            SwissEph.SE_GREG_CAL);

        // Only the Ascendant itself is needed (ascmc[0], house system 'P' is
        // ignored for this); the houses below then follow the whole-sign system.
        var cusps = new double[13];
        var ascmc = new double[10];
        sw.swe_houses(jdUt, lat, lng, 'P', cusps, ascmc);
        var ascendantLongitude = ascmc[0];
        var ascendantSign = DegreeToSign(ascendantLongitude);

        var chart = new NatalChart
        {
            UserId = user.Id,
            BirthMomentUtc = birthMomentUtc,
            AscendantSign = ascendantSign,
            AscendantIsApproximate = user.BirthTimeIsUnknown,
            CalculatedAt = DateTime.UtcNow,
        };

        chart.Placements.Add(new Placement
        {
            Body = CelestialBody.Ascendant,
            Sign = chart.AscendantSign,
            House = 1,
            DegreeInSign = DegreeWithinSign(ascendantLongitude),
        });

        foreach (var (body, sweId) in Planets)
        {
            var xx = new double[6];
            var errorMessage = "";
            sw.swe_calc_ut(jdUt, sweId, flags, xx, ref errorMessage);
            var longitude = xx[0];

            var sign = DegreeToSign(longitude);

            if (body == CelestialBody.Sun)
            {
                chart.SunSign = sign;
            }
            else if (body == CelestialBody.Moon)
            {
                chart.MoonSign = sign;
            }

            chart.Placements.Add(new Placement
            {
                Body = body,
                Sign = sign,
                House = DetermineHouse(sign, ascendantSign),
                DegreeInSign = DegreeWithinSign(longitude),
            });
        }

        return chart;
    }

    /// <summary>
    /// Converts the local birth date/time to UT, using the historical
    /// timezone of the birth place (derived from the coordinates). For an
    /// ambiguous or nonexistent local time (a DST transition), the most
    /// likely interpretation is used instead of crashing.
    /// </summary>
    private static DateTime LocalTimeToUtc(DateOnly date, TimeOnly time, double lat, double lng)
    {
        var ianaId = TimeZoneLookup.GetTimeZone(lat, lng).Result;
        var timeZone = DateTimeZoneProviders.Tzdb[ianaId];

        var local = new LocalDateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        var zonedDateTime = local.InZoneLeniently(timeZone);

        return zonedDateTime.ToDateTimeUtc();
    }

    /// <summary>Converts an ecliptic longitude (0-360°) to the zodiac sign.</summary>
    private static ZodiacSign DegreeToSign(double longitude)
    {
        var normalized = Normalize(longitude);
        return (ZodiacSign)((int)(normalized / 30) % 12);
    }

    /// <summary>Position within the sign itself (0-30°).</summary>
    private static decimal DegreeWithinSign(double longitude)
    {
        var normalized = Normalize(longitude);
        return (decimal)(normalized % 30);
    }

    private static double Normalize(double degrees) => ((degrees % 360) + 360) % 360;

    /// <summary>
    /// Whole-sign house of a placement: house 1 is the Ascendant's sign, and
    /// each following sign (in zodiac order) is the next house. E.g.
    /// Ascendant in Leo: Virgo = house 2, ... Cancer = house 12.
    /// </summary>
    private static int DetermineHouse(ZodiacSign sign, ZodiacSign ascendantSign)
    {
        return (((int)sign - (int)ascendantSign + 12) % 12) + 1;
    }
}
