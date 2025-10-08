using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace PersonalAgent.Plugin;

public class TimePlugin
{
    [KernelFunction]
    [Description("Gets the current date and time in the local timezone in a human-readable format")]
    public string GetCurrentDateTime()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        var timezone = TimeZoneInfo.Local;
        return $"{localTime.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt")} ({timezone.StandardName})";
    }

    [KernelFunction]
    [Description("Gets the current time in the local timezone in 24-hour format")]
    public string GetCurrentTime()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.ToString("HH:mm:ss");
    }

    [KernelFunction]
    [Description("Gets the current date in the local timezone")]
    public string GetCurrentDate()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.ToString("dddd, MMMM dd, yyyy");
    }

    [KernelFunction]
    [Description("Gets the current day of the week in the local timezone")]
    public string GetDayOfWeek()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.DayOfWeek.ToString();
    }

    [KernelFunction]
    [Description("Gets the current timezone information")]
    public string GetTimezone()
    {
        var timezone = TimeZoneInfo.Local;
        return $"{timezone.DisplayName} (UTC{timezone.BaseUtcOffset.Hours:+00;-00}:{timezone.BaseUtcOffset.Minutes:00})";
    }

    [KernelFunction]
    [Description("Gets the current UTC time")]
    public string GetUtcTime()
    {
        var utcNow = DateTime.UtcNow;
        return utcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    }

    [KernelFunction]
    [Description("Calculates how many days until a specific date")]
    public string DaysUntil(
        [Description("The target date in format yyyy-MM-dd (e.g., 2025-12-25)")] string targetDate)
    {
        if (DateTime.TryParse(targetDate, out var target))
        {
            var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
            var today = localTime.Date;
            var days = (target.Date - today).Days;
            
            if (days < 0)
                return $"That date was {Math.Abs(days)} days ago";
            else if (days == 0)
                return "That date is today!";
            else if (days == 1)
                return "That date is tomorrow";
            else
                return $"There are {days} days until {target.ToString("MMMM dd, yyyy")}";
        }
        
        return "Invalid date format. Please use yyyy-MM-dd format (e.g., 2025-12-25)";
    }

    [KernelFunction]
    [Description("Gets the current week number of the year in the local timezone")]
    public string GetWeekNumber()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            localTime, 
            System.Globalization.CalendarWeekRule.FirstDay, 
            DayOfWeek.Monday);
        return $"Week {weekNumber} of {localTime.Year}";
    }

    [KernelFunction]
    [Description("Gets the current time in a specific timezone")]
    public string GetTimeInTimezone(
        [Description("The timezone ID (e.g., 'America/New_York', 'Europe/London', 'Asia/Tokyo', 'Pacific/Auckland')")] string timezoneId)
    {
        try
        {
            var targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var timeInZone = TimeZoneInfo.ConvertTime(DateTime.UtcNow, targetTimezone);
            return $"{timeInZone.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt")} ({targetTimezone.StandardName})";
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Timezone '{timezoneId}' not found. Common timezone IDs: America/New_York, Europe/London, Asia/Tokyo, Pacific/Auckland";
        }
    }

    [KernelFunction]
    [Description("Lists common timezone IDs that can be used with GetTimeInTimezone function")]
    public string ListCommonTimezones()
    {
        return """
               Common Timezone IDs:
               - America/New_York (Eastern Time)
               - America/Chicago (Central Time)
               - America/Denver (Mountain Time)
               - America/Los_Angeles (Pacific Time)
               - Europe/London (GMT/BST)
               - Europe/Paris (Central European Time)
               - Asia/Tokyo (Japan Standard Time)
               - Asia/Shanghai (China Standard Time)
               - Pacific/Auckland (New Zealand Time)
               - Australia/Sydney (Australian Eastern Time)
               """;
    }
}
