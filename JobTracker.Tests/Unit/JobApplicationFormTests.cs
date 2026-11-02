using JobTracker.ViewModels;
using System.ComponentModel.DataAnnotations;
using JobTracker.Models;
namespace JobTracker.Tests.Unit;

public class JobApplicationFormTests
{
    private const string earlyDate = "2026-01-10";
    private const string laterDate = "2026-01-11";

    [Theory]
    [InlineData(JobApplicationStatus.Saved, null, null)]
    [InlineData(JobApplicationStatus.Applied, null, null)]
    [InlineData(JobApplicationStatus.Saved, earlyDate, earlyDate)]
    [InlineData(JobApplicationStatus.Applied, earlyDate, earlyDate)]
    [InlineData(JobApplicationStatus.Saved, earlyDate, laterDate)]
    [InlineData(JobApplicationStatus.Applied, earlyDate, laterDate)]
    [InlineData(JobApplicationStatus.Saved, laterDate, earlyDate)]
    public void SuccessOnValidDateInput(JobApplicationStatus status, string? postedAt, string? appliedAt)
    {
        JobApplicationFormViewModel form = new()
        {
            Title = "Software Engineer",
            CompanyName = "Acme Inc.",
            Status = status,
            AppliedAt = appliedAt is not null ? DateOnly.Parse(appliedAt) : null,
            PostedAt = postedAt is not null ? DateOnly.Parse(postedAt) : null
        };

        var results = form.Validate(new ValidationContext(form)).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void FailureOnAppliedBeforePosted()
    {
        JobApplicationFormViewModel form = new()
        {
            Title = "Software Engineer",
            CompanyName = "Acme Inc.",
            Status = JobApplicationStatus.Applied,
            AppliedAt = DateOnly.Parse(earlyDate),
            PostedAt = DateOnly.Parse(laterDate)
        };

        var results = form.Validate(new ValidationContext(form)).ToList();
        var result = Assert.Single(results);
        Assert.Contains(nameof(form.AppliedAt), result.MemberNames);
    }
}