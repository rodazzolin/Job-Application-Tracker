using Microsoft.AspNetCore.Mvc.ModelBinding;
using JobTracker.Models;
namespace JobTracker.ViewModels;

// Input for JobApplicationsController.UpdateStatus, posted by the board's drag-and-drop script.
public class JobStatusUpdateViewModel
{
    // [BindRequired] rather than [Required]: a missing int/enum would silently default to 0,
    // which [Required] can't detect. [BindRequired] fails validation if the field isn't posted.
    [BindRequired]
    public int ID {get; set;}
    [BindRequired]
    public JobApplicationStatus Status {get; set;}
}
