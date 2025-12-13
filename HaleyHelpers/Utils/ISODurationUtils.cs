using System;
using System.Globalization;
using System.Text.RegularExpressions;

//Weeks is always 7 days.. Day is always 24 hours.. One hour is always 60 minutes... These are fixed length
//Year can be 365 or 366 days.. Month can be 28,29,30,31 days.. These are not fixed length
public static class ISODurationUtils {
    private static readonly Regex _isoDuration =
        new Regex(
            @"^(?<sign>[+-])?P" +
            @"(?:(?<years>\d+(?:[.,]\d+)?)Y)?" +
            @"(?:(?<months>\d+(?:[.,]\d+)?)M)?" +
            @"(?:(?<weeks>\d+(?:[.,]\d+)?)W)?" +
            @"(?:(?<days>\d+(?:[.,]\d+)?)D)?" +
            @"(?:T" +
                @"(?:(?<hours>\d+(?:[.,]\d+)?)H)?" +
                @"(?:(?<minutes>\d+(?:[.,]\d+)?)M)?" +
                @"(?:(?<seconds>\d+(?:[.,]\d+)?)S)?" +
            @")?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

    /// <summary>
    /// Converts an ISO-8601 duration string (e.g., "PT15M", "P2DT3H") to seconds.
    /// Years/months are rejected by default because they're not fixed length.
    /// </summary>
    public static bool TryToSeconds(
        string iso,
        out long seconds,
        bool roundUp = true,
        bool allowCalendarUnits = false,
        double daysPerMonth = 30,
        double daysPerYear = 365
    ) {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(iso)) return false;

        var m = _isoDuration.Match(iso.Trim());
        if (!m.Success) return false;

        double years = Read(m, "years");
        double months = Read(m, "months");

        if (!allowCalendarUnits && (years != 0 || months != 0))
            return false;

        double weeks = Read(m, "weeks");
        double days = Read(m, "days");
        double hours = Read(m, "hours");
        double minutes = Read(m, "minutes");
        double secs = Read(m, "seconds");

        // Total seconds
        double totalSeconds =
            (years * daysPerYear * 24 * 60 * 60) +
            (months * daysPerMonth * 24 * 60 * 60) +
            (weeks * 7 * 24 * 60 * 60) +
            (days * 24 * 60 * 60) +
            (hours * 60 * 60) +
            (minutes * 60) +
            secs;

        // Sign
        var sign = m.Groups["sign"].Value;
        if (sign == "-") totalSeconds = -totalSeconds;

        // For timeouts, negative durations typically don't make sense
        if (totalSeconds < 0) return false;

        // Round
        double rounded = roundUp ? Math.Ceiling(totalSeconds) : Math.Floor(totalSeconds);

        if (rounded > long.MaxValue) return false;
        seconds = (long)rounded;
        return true;
    }

    /// <summary>
    /// Converts an ISO-8601 duration string to minutes.
    /// Uses TryToSeconds internally and then rounds minutes from seconds.
    /// </summary>
    public static bool TryToMinutes(
        string iso,
        out int minutes,
        bool roundUp = true,
        bool allowCalendarUnits = false,
        double daysPerMonth = 30,
        double daysPerYear = 365
    ) {
        minutes = 0;

        if (!TryToSeconds(
                iso,
                out var totalSeconds,
                roundUp: roundUp,
                allowCalendarUnits: allowCalendarUnits,
                daysPerMonth: daysPerMonth,
                daysPerYear: daysPerYear))
            return false;

        // Convert seconds -> minutes (keep same rounding intent)
        double min = totalSeconds / 60.0;
        double rounded = roundUp ? Math.Ceiling(min) : Math.Floor(min);

        if (rounded > int.MaxValue) return false;
        minutes = (int)rounded;
        return true;
    }

    private static double Read(Match m, string groupName) {
        var g = m.Groups[groupName];
        if (!g.Success || string.IsNullOrWhiteSpace(g.Value)) return 0;

        var s = g.Value.Replace(',', '.'); // accept "1,5" too
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}
