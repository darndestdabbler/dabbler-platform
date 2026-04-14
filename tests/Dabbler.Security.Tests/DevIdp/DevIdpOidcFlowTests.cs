extern alias devidp;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DevIdpDbContext = devidp::Dabbler.Security.DevIdp.Data.DevIdpDbContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dabbler.Security.Tests.DevIdp;

/// <summary>
/// Integration tests for the DevIdp OpenID Connect provider.
/// Uses WebApplicationFactory to start the DevIdp in-process and exercises
/// the full authorization code flow with PKCE, discovery endpoints, and error handling.
/// </summary>
public class DevIdpOidcFlowTests : IDisposable
{
    private readonly DevIdpFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ── Discovery & JWKS ─────────────────────────────────────────────────────

    [Fact]
    public async Task DiscoveryEndpoint_ReturnsValidOidcConfiguration()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/.well-known/openid-configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("issuer").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("authorization_endpoint").GetString().Should().Contain("/connect/authorize");
        json.GetProperty("token_endpoint").GetString().Should().Contain("/connect/token");
        json.GetProperty("jwks_uri").GetString().Should().NotBeNullOrEmpty();

        var scopes = json.GetProperty("scopes_supported")
            .EnumerateArray().Select(s => s.GetString()).ToList();
        scopes.Should().Contain("openid");
        scopes.Should().Contain("profile");
        scopes.Should().Contain("email");
        scopes.Should().Contain("roles");

        var codeChallenges = json.GetProperty("code_challenge_methods_supported")
            .EnumerateArray().Select(s => s.GetString()).ToList();
        codeChallenges.Should().Contain("S256");
    }

    [Fact]
    public async Task JwksEndpoint_ReturnsSigningKeys()
    {
        var client = _factory.CreateClient();

        // Get JWKS URI from discovery
        var discovery = await client.GetFromJsonAsync<JsonElement>(
            "/.well-known/openid-configuration");
        var jwksUri = discovery.GetProperty("jwks_uri").GetString()!;
        var jwksPath = new Uri(jwksUri).PathAndQuery;

        var response = await client.GetAsync(jwksPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jwks = await response.Content.ReadFromJsonAsync<JsonElement>();
        var keys = jwks.GetProperty("keys").EnumerateArray().ToList();
        keys.Should().NotBeEmpty("JWKS should contain at least one signing key");
        keys[0].GetProperty("kty").GetString().Should().Be("RSA");
        keys[0].GetProperty("kid").GetString().Should().NotBeNullOrEmpty();
    }

    // ── Authorization Code Flow ──────────────────────────────────────────────

    [Theory]
    [InlineData("admin@contoso.local", "Pass123!", "UserAdministrator,Manager")]
    [InlineData("jane@contoso.local", "Pass123!", "Editor")]
    [InlineData("viewer@contoso.local", "Pass123!", "Viewer")]
    public async Task AuthorizationCodeFlow_SeededUser_ReturnsValidTokens(
        string email, string password, string expectedRolesCsv)
    {
        var expectedRoles = expectedRolesCsv.Split(',');
        using var client = _factory.CreateClientWithCookies();

        var tokens = await ExecuteOidcFlowAsync(client, email, password);

        tokens.IdToken.Should().NotBeNullOrEmpty("flow should return an id_token");
        tokens.AccessToken.Should().NotBeNullOrEmpty("flow should return an access_token");

        // Validate roles in the ID token
        var idClaims = DecodeJwtPayload(tokens.IdToken);
        var idRoles = ExtractRoles(idClaims);
        idRoles.Should().BeEquivalentTo(expectedRoles);
    }

    [Fact]
    public async Task IdToken_AdminUser_ContainsEntraCompatibleClaims()
    {
        using var client = _factory.CreateClientWithCookies();
        var tokens = await ExecuteOidcFlowAsync(client, "admin@contoso.local", "Pass123!");

        var claims = DecodeJwtPayload(tokens.IdToken);

        // Standard OIDC claims
        claims.GetProperty("sub").GetString().Should().NotBeNullOrEmpty();
        claims.GetProperty("name").GetString().Should().Be("Admin User");
        claims.GetProperty("email").GetString().Should().Be("admin@contoso.local");
        claims.GetProperty("preferred_username").GetString().Should().Be("admin@contoso.local");

        // Entra ID-compatible claims
        claims.GetProperty("oid").GetString().Should().Be("11111111-1111-1111-1111-111111111111");
        claims.GetProperty("tid").GetString().Should().Be("00000000-0000-0000-0000-000000000001");

        // Roles
        var roles = ExtractRoles(claims);
        roles.Should().BeEquivalentTo(["UserAdministrator", "Manager"]);
    }

    [Fact]
    public async Task AccessToken_AdminUser_ContainsEntraCompatibleClaims()
    {
        using var client = _factory.CreateClientWithCookies();
        var tokens = await ExecuteOidcFlowAsync(client, "admin@contoso.local", "Pass123!");

        var claims = DecodeJwtPayload(tokens.AccessToken);

        claims.GetProperty("oid").GetString().Should().Be("11111111-1111-1111-1111-111111111111");
        claims.GetProperty("tid").GetString().Should().Be("00000000-0000-0000-0000-000000000001");
        claims.GetProperty("name").GetString().Should().Be("Admin User");
        claims.GetProperty("preferred_username").GetString().Should().Be("admin@contoso.local");

        var roles = ExtractRoles(claims);
        roles.Should().BeEquivalentTo(["UserAdministrator", "Manager"]);
    }

    [Fact]
    public async Task AuthorizationCodeFlow_StateParameter_IsPreserved()
    {
        using var client = _factory.CreateClientWithCookies();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var expectedState = "my-unique-state-" + Guid.NewGuid().ToString("N");

        var authorizeUrl = "/connect/authorize?" +
            "client_id=dabbler-test-client&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            "response_type=code&" +
            "scope=openid%20profile%20email%20roles&" +
            $"state={expectedState}&" +
            $"code_challenge={codeChallenge}&" +
            "code_challenge_method=S256";

        // Drive the flow through login
        var resp1 = await client.GetAsync(authorizeUrl);
        var loginUrl = resp1.Headers.Location!.ToString();
        var resp2 = await client.GetAsync(loginUrl);
        var html = await resp2.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(html);
        var returnUrl = ExtractQueryParam(loginUrl, "ReturnUrl");

        var form = BuildLoginForm("admin@contoso.local", "Pass123!", returnUrl, antiforgeryToken);
        var resp3 = await client.PostAsync("/Login", new FormUrlEncodedContent(form));
        var resp4 = await client.GetAsync(resp3.Headers.Location!.ToString());

        // Verify state is echoed back in the redirect
        var redirectLocation = resp4.Headers.Location!.ToString();
        var returnedState = ExtractQueryParam(redirectLocation, "state");
        returnedState.Should().Be(expectedState);
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidCredentials_LoginDisplaysError()
    {
        using var client = _factory.CreateClientWithCookies();

        // Initiate authorize flow to get to the login page
        var authorizeUrl = BuildAuthorizeUrl();
        var resp1 = await client.GetAsync(authorizeUrl);
        var loginUrl = resp1.Headers.Location!.ToString();

        // Get login page and extract antiforgery token
        var resp2 = await client.GetAsync(loginUrl);
        var html = await resp2.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(html);
        var returnUrl = ExtractQueryParam(loginUrl, "ReturnUrl");

        // POST with wrong password
        var form = BuildLoginForm("admin@contoso.local", "WrongPassword!", returnUrl, antiforgeryToken);
        var loginResponse = await client.PostAsync("/Login", new FormUrlEncodedContent(form));

        // Login failure re-renders the page (200) with error message
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseHtml = await loginResponse.Content.ReadAsStringAsync();
        responseHtml.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task UnknownClientId_AuthorizeReturnsError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            "/connect/authorize?" +
            "client_id=unknown-client&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            "response_type=code&" +
            "scope=openid&" +
            "state=test&" +
            "code_challenge=test-challenge&" +
            "code_challenge_method=S256");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvalidRedirectUri_AuthorizeReturnsError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            "/connect/authorize?" +
            "client_id=dabbler-test-client&" +
            $"redirect_uri={Uri.EscapeDataString("https://evil.com/callback")}&" +
            "response_type=code&" +
            "scope=openid&" +
            "state=test&" +
            "code_challenge=test-challenge&" +
            "code_challenge_method=S256");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── OIDC Flow Helper ─────────────────────────────────────────────────────

    private const string TestRedirectUri = "https://localhost:5001/signin-oidc-test";

    private static string BuildAuthorizeUrl()
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        return "/connect/authorize?" +
            "client_id=dabbler-test-client&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            "response_type=code&" +
            "scope=openid%20profile%20email%20roles&" +
            "state=test-state&" +
            $"code_challenge={codeChallenge}&" +
            "code_challenge_method=S256";
    }

    private async Task<TokenResponse> ExecuteOidcFlowAsync(
        HttpClient client, string email, string password)
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var authorizeUrl = "/connect/authorize?" +
            "client_id=dabbler-test-client&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            "response_type=code&" +
            "scope=openid%20profile%20email%20roles&" +
            "state=test-state&" +
            $"code_challenge={codeChallenge}&" +
            "code_challenge_method=S256";

        // Step 1: Initiate authorize → redirects to /Login
        var resp1 = await client.GetAsync(authorizeUrl);
        resp1.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var loginUrl = resp1.Headers.Location!.ToString();

        // Step 2: GET login page → extract antiforgery token
        var resp2 = await client.GetAsync(loginUrl);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp2.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(html);
        var returnUrl = ExtractQueryParam(loginUrl, "ReturnUrl");

        // Step 3: POST login credentials
        var form = BuildLoginForm(email, password, returnUrl, antiforgeryToken);
        var resp3 = await client.PostAsync("/Login", new FormUrlEncodedContent(form));
        resp3.StatusCode.Should().Be(HttpStatusCode.Redirect, "login should redirect on success");

        // Step 4: Follow redirect back to authorize → redirects to redirect_uri with code
        var authorizeCallback = resp3.Headers.Location!.ToString();
        var resp4 = await client.GetAsync(authorizeCallback);
        resp4.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Step 5: Extract authorization code (don't follow — redirect_uri is external)
        var redirectLocation = resp4.Headers.Location!.ToString();
        redirectLocation.Should().StartWith(TestRedirectUri);
        var code = ExtractQueryParam(redirectLocation, "code");
        code.Should().NotBeNullOrEmpty("authorize should return an authorization code");

        // Step 6: Exchange code for tokens
        var tokenForm = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = "dabbler-test-client",
            ["client_secret"] = "test-secret",
            ["code_verifier"] = codeVerifier
        };
        var resp5 = await client.PostAsync("/connect/token", new FormUrlEncodedContent(tokenForm));
        resp5.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenJson = await resp5.Content.ReadFromJsonAsync<JsonElement>();
        return new TokenResponse(
            tokenJson.GetProperty("id_token").GetString()!,
            tokenJson.GetProperty("access_token").GetString()!);
    }

    // ── Utility Methods ──────────────────────────────────────────────────────

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(html,
                @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        }

        match.Success.Should().BeTrue("login page should contain an antiforgery token");
        return match.Groups[1].Value;
    }

    private static string ExtractQueryParam(string url, string paramName)
    {
        var queryStart = url.IndexOf('?');
        if (queryStart < 0) return string.Empty;

        var query = url[(queryStart + 1)..];
        foreach (var pair in query.Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) continue;
            if (Uri.UnescapeDataString(pair[..eq]) == paramName)
                return Uri.UnescapeDataString(pair[(eq + 1)..]);
        }
        return string.Empty;
    }

    private static Dictionary<string, string> BuildLoginForm(
        string email, string password, string returnUrl, string antiforgeryToken)
    {
        return new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = returnUrl,
            ["__RequestVerificationToken"] = antiforgeryToken
        };
    }

    private static JsonElement DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        parts.Length.Should().BeGreaterOrEqualTo(3, "JWT should have at least 3 parts");

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        return JsonDocument.Parse(bytes).RootElement;
    }

    private static string[] ExtractRoles(JsonElement claims)
    {
        if (!claims.TryGetProperty("roles", out var rolesElement))
            return [];

        return rolesElement.ValueKind switch
        {
            JsonValueKind.Array => rolesElement.EnumerateArray()
                .Select(r => r.GetString()!).ToArray(),
            JsonValueKind.String => [rolesElement.GetString()!],
            _ => []
        };
    }

    // ── Inner Types ──────────────────────────────────────────────────────────

    private record TokenResponse(string IdToken, string AccessToken);

    /// <summary>
    /// WebApplicationFactory for the DevIdp. Uses DevIdpDbContext as the
    /// assembly locator (avoids global Program class collision with SampleApp.Api).
    /// </summary>
    internal sealed class DevIdpFactory : WebApplicationFactory<DevIdpDbContext>
    {
        private static readonly Uri HttpsBase = new("https://localhost");

        private readonly string _dbPath = Path.Combine(
            Path.GetTempPath(), $"devidp-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DevIdp",
                $"Data Source={_dbPath};Pooling=false");
        }

        /// <summary>
        /// Creates a client with HTTPS base address (OpenIddict requires HTTPS)
        /// and cookie persistence for multi-step OIDC flows.
        /// </summary>
        public HttpClient CreateClientWithCookies()
        {
            var handler = new CookieHandler(Server.CreateHandler());
            return new HttpClient(handler) { BaseAddress = HttpsBase };
        }

        /// <summary>
        /// Creates a default client with HTTPS base address.
        /// </summary>
        public new HttpClient CreateClient()
        {
            return base.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = HttpsBase
            });
        }

        /// <summary>
        /// Creates a client with specified options, forcing HTTPS base address.
        /// </summary>
        public new HttpClient CreateClient(WebApplicationFactoryClientOptions options)
        {
            options.BaseAddress = HttpsBase;
            return base.CreateClient(options);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); }
            catch { /* best-effort temp file cleanup */ }
        }
    }

    /// <summary>
    /// Maintains cookies across HTTP requests through the test server handler.
    /// Required for the multi-step OIDC flow (Identity cookie must persist
    /// between login POST and authorize GET).
    /// </summary>
    internal sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly CookieContainer _cookies = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cookieHeader = _cookies.GetCookieHeader(request.RequestUri!);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.Add("Cookie", cookieHeader);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var cookie in setCookies)
                    _cookies.SetCookies(request.RequestUri!, cookie);
            }

            return response;
        }
    }
}
