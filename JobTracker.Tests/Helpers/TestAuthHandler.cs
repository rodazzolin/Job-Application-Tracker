using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
namespace JobTracker.Tests.Helpers;

// A test authentication handler that allows tests to simulate authenticated users by providing a user ID in the request headers.
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public new const string Scheme = "Test";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If the request does not contain the test user header, treat it as an unauthenticated request.
        if (!Request.Headers.TryGetValue(UserHeader, out var userID))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Otherwise, build a claim identity stating "this is the user's ID"
        // and wrap it in a principal inside a ticket to return a successful authentication result.
        List<Claim> claims = [ new Claim(ClaimTypes.NameIdentifier, userID!) ];
        ClaimsIdentity identity = new(claims, Scheme);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}