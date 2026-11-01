using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models;
public enum JobApplicationStatus
{
    Saved = 0,
    Applied = 1,
    Interviewing = 2,
    Archived = 3,       // Replaces Rejected (3) and Withdrawn (4); see migration ConsolidateArchivedStatus.
    [Display(Name = "Offer Received")]
    OfferReceived = 5,
}
