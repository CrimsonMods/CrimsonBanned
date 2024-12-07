using System;
using System.Linq;

namespace CrimsonBanned.Utilities;

public static class TimeUtility
{
    public static string FormatRemainder(DateTime date)
    {
        TimeSpan remainder = date - DateTime.Now;
        return FormatRemainder(remainder);
    }

    public static string FormatRemainder(TimeSpan remainder)
    {
        string formattedRemainder = string.Empty;
        if (remainder.Days > 0)
            formattedRemainder += $"{remainder.Days} days, ";
        if (remainder.Hours > 0)
            formattedRemainder += $"{remainder.Hours} hours, ";
        if (remainder.Minutes > 0)
            formattedRemainder += $"{remainder.Minutes} minutes";

        if (formattedRemainder.EndsWith(", "))
            formattedRemainder = formattedRemainder.Substring(0, formattedRemainder.Length - 2);

        return formattedRemainder;
    }

    public static TimeSpan LengthParse(int length, string denomination)
    {
        denomination = denomination.ToLower();

        var minuteAliases = new[] { "minute", "minutes", "min", "mins", "m" };
        var hourAliases = new[] { "hour", "hours", "hrs", "hr", "h" };
        var dayAliases = new[] { "day", "days", "d" };

        if (minuteAliases.Contains(denomination))
            denomination = "m";
        else if (hourAliases.Contains(denomination))
            denomination = "h";
        else if (dayAliases.Contains(denomination))
            denomination = "d";
        else
            denomination = "m";

        return denomination switch
        {
            "m" => TimeSpan.FromMinutes(length),
            "h" => TimeSpan.FromHours(length),
            "d" => TimeSpan.FromDays(length),
            _ => TimeSpan.FromMinutes(length)
        };
    }

    public static bool IsPermanent(DateTime date)
    {
        if(date == DateTime.MinValue) return true;
        if(date == DateTime.MinValue.ToUniversalTime()) return true;
        if(date ==  DateTime.MinValue.ToLocalTime()) return true;   
        return false;
    }
}