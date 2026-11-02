using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JobTracker.Models;
namespace JobTracker.ViewModels;

public class JobApplicationFormViewModel : IValidatableObject
{
    [Required]
    [MaxLength(FieldLengths.JobTitle)]
    [DisplayName("Job Title")]
    public string Title {get; set;} = "";
    [Required]
    [MaxLength(FieldLengths.CompanyTitle)]
    [DisplayName("Company")]
    public string CompanyName {get; set;} = "";
    [MaxLength(FieldLengths.Location)]
    public string? Location {get; set;} = "";
    public JobApplicationStatus Status {get; set;} = JobApplicationStatus.Saved;
    [MaxLength(FieldLengths.URL)]
    [Url]
    [DisplayName("Posting URL")]
    public string? PostingURL {get; set;} = "";
    [DataType(DataType.Date)]
    [DisplayName("Applied At")]
    public DateOnly? AppliedAt {get; set;}
    [DataType(DataType.Date)]
    [DisplayName("Posted At")]
    public DateOnly? PostedAt {get; set;}
    [MaxLength(FieldLengths.Notes)]
    [DataType(DataType.MultilineText)]
    public string? Notes {get; set;}

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status != JobApplicationStatus.Saved
            && AppliedAt is not null
            && PostedAt is not null
            && AppliedAt < PostedAt)
        {
            yield return new ValidationResult(
                "Application date can't be before the posting date.",
                [nameof(AppliedAt)]);
        }
    }
}