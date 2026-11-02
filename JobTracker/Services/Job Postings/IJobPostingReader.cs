using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Services.JobPostings;

// Declares a common method for parsing a given URL and returning the extractable data from a job posting.
public interface IJobPostingReader
{
    public Task<JobPostingInfo?> TryReadPostingAsync(Uri url);
}