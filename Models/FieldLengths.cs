namespace JobTracker.Models;

public class FieldLengths
{
    public const int JobTitle = 200;
    public const int CompanyTitle = 200;
    public const int Status = 20;
    public const int Location = 200;
    public const int URL = 2048;
    // Over 4000, so SQL Server stores it as nvarchar(max); the limit is still enforced by form validation.
    public const int Notes = 10_000;
}