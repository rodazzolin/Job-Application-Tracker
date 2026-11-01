using System.ComponentModel.DataAnnotations;
namespace JobTracker.ViewModels;

// How far back the board looks, based on each job's UpdatedAt.
public enum JobBoardRange
{
    [Display(Name = "Last 14 days")]  Last14Days,
    [Display(Name = "Last month")]    LastMonth,
    [Display(Name = "Last 3 months")] Last3Months,
    [Display(Name = "Last 6 months")] Last6Months,
    [Display(Name = "All time")]      AllTime,
}

public static class JobBoardRangeExtensions
{
    // The earliest UpdatedAt shown for this range, or null for no limit.
    public static DateTime? GetCutoff(this JobBoardRange range, DateTime nowUtc) => range switch
    {
        JobBoardRange.Last14Days  => nowUtc.AddDays(-14),
        JobBoardRange.LastMonth   => nowUtc.AddMonths(-1),
        JobBoardRange.Last3Months => nowUtc.AddMonths(-3),
        JobBoardRange.Last6Months => nowUtc.AddMonths(-6),
        _ => null,
    };
}
