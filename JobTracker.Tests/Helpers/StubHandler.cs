using System.Net;
using System.Text;
namespace JobTracker.Tests.Helpers;

// A stub HttpMessageHandler that returns a predefined status code and body for testing purposes.
class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}