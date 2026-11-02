namespace JobTracker.Helpers;

// Human-friendly date text shared by the board cards and the Details page.
// Timestamps are stored in UTC and shown in the server's local time (the app runs on your machine).
public static class DateDisplay
{
    // Whole days since a UTC timestamp, never negative.
    public static int DaysSince(DateTime utc) => Math.Max(0, (DateTime.UtcNow - utc).Days);

    // "1 day", "12 days".
    public static string DayCount(int days) => Plural(days, "day");

    // "Today" or "12 days": the card's time-in-status label.
    public static string StatusAge(DateTime utc)
    {
        int days = DaysSince(utc);
        return days == 0 ? "Today" : DayCount(days);
    }

    // Relative text for a UTC timestamp: "just now", "5 minutes ago", "yesterday", "3 weeks ago".
    public static string Ago(DateTime utc)
    {
        TimeSpan elapsed = DateTime.UtcNow - utc;
        if(elapsed.TotalMinutes < 1)
            return "just now";
        if(elapsed.TotalHours < 1)
            return $"{Plural((int)elapsed.TotalMinutes, "minute")} ago";
        if(elapsed.TotalDays < 1)
            return $"{Plural((int)elapsed.TotalHours, "hour")} ago";
        return DaysAgo((int)elapsed.TotalDays);
    }

    // Relative text for a calendar date: "today", "yesterday", "2 months ago".
    public static string Ago(DateOnly date)
    {
        int days = DateOnly.FromDateTime(DateTime.Today).DayNumber - date.DayNumber;
        if(days < 0)
            return $"in {DayCount(-days)}";
        return days == 0 ? "today" : DaysAgo(days);
    }

    // Exact values, for tooltips next to relative text.
    public static string Full(DateOnly date) => date.ToString("MMM d, yyyy");
    public static string Full(DateTime utc) => utc.ToLocalTime().ToString("MMM d, yyyy h:mm tt");

    private static string DaysAgo(int days) => days switch
    {
        1 => "yesterday",
        < 7 => $"{days} days ago",
        < 30 => $"{Plural(days / 7, "week")} ago",
        < 365 => $"{Plural(days / 30, "month")} ago",
        _ => $"{Plural(days / 365, "year")} ago",
    };

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
