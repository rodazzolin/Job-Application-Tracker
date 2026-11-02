namespace JobTracker.Models;

public class ErrorViewModel
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "";
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
