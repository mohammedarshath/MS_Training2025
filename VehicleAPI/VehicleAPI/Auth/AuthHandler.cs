using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace VehicleAPI.Auth
{
    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Scheme = "TestBypass";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Opt-in guard: only authenticate if header is present
            if (!Request.Headers.TryGetValue("X-Test-Bypass", out var bypass) || bypass != "1")
                return Task.FromResult(AuthenticateResult.NoResult());

            // Optional headers to shape the identity
            var user = Request.Headers.TryGetValue("X-Test-User", out var u) ? u.ToString() : "test-user";
            var roles = Request.Headers.TryGetValue("X-Test-Roles", out var r) ? r.ToString().Split(',') : Array.Empty<string>();
            var scopes = Request.Headers.TryGetValue("X-Test-Scopes", out var s) ? s.ToString() : "openid profile";

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user),
            new(ClaimTypes.Name, user),
            // space-separated scopes (fits many policy patterns)
            new("scope", scopes)
        };

            // Add role claims so [Authorize(Roles="...")] works
            foreach (var role in roles.Select(x => x.Trim()).Where(x => x.Length > 0))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, Scheme, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
