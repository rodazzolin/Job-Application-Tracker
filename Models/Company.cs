namespace JobTracker.Models;
public class Company
{
    public int ID {get; set;}
    public required string Name {get; set;}
    public List<JobApplication> Applications {get; set;} = [];
}