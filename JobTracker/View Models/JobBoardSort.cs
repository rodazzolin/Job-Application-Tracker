using System.ComponentModel.DataAnnotations;
using JobTracker.Models;
namespace JobTracker.ViewModels;

// How cards are ordered within each board list. Names appear in the URL (?sort=Applied)
// and in job-board.js, which places moved cards using the same rules.
public enum JobBoardSort
{
    [Display(Name = "Last updated")] Updated,
    [Display(Name = "Date applied")] Applied,
    [Display(Name = "Title (A–Z)")]  Title,
}

public static class JobBoardSortExtensions
{
    // Every sort falls back to most recently updated first, so ties have a stable order.
    public static IQueryable<JobApplication> ApplySort(this IQueryable<JobApplication> query, JobBoardSort sort) => sort switch
    {
        // Newest application first; jobs never applied to (Saved) go last.
        JobBoardSort.Applied => query
            .OrderBy(x => x.AppliedAt == null)
            .ThenByDescending(x => x.AppliedAt)
            .ThenByDescending(x => x.UpdatedAt),
        JobBoardSort.Title => query
            .OrderBy(x => x.Title)
            .ThenByDescending(x => x.UpdatedAt),
        _ => query.OrderByDescending(x => x.UpdatedAt),
    };
}
