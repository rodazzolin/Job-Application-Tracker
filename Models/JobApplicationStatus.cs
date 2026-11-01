using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models;
public enum JobApplicationStatus
{
    Saved = 0,
    Applied = 1,
    Interviewing = 2,
    Rejected = 3,
    Withdrawn = 4,
    [Display(Name = "Offer Received")]
    OfferReceived = 5,
}
