using System.Security.Claims;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

public sealed class ExternalClaimMapperTests
{
    // --- Entra ID claim extraction ---

    [Fact]
    public void ExtractUserInfo_EntraId_UsesOidClaim()
    {
        var principal = CreatePrincipal(
            ("http://schemas.microsoft.com/identity/claims/objectidentifier", "entra-oid-123"),
            (ClaimTypes.Email, "alice@contoso.local"),
            (ClaimTypes.Name, "Alice Smith"));

        var info = ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        info.SubjectId.Should().Be("entra-oid-123");
        info.Email.Should().Be("alice@contoso.local");
        info.DisplayName.Should().Be("Alice Smith");
    }

    [Fact]
    public void ExtractUserInfo_EntraId_FallsBackToShortOidClaim()
    {
        var principal = CreatePrincipal(
            ("oid", "short-oid-456"),
            ("preferred_username", "bob@contoso.local"),
            (ClaimTypes.Name, "Bob Jones"));

        var info = ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        info.SubjectId.Should().Be("short-oid-456");
        info.Email.Should().Be("bob@contoso.local");
        info.DisplayName.Should().Be("Bob Jones");
    }

    [Fact]
    public void ExtractUserInfo_EntraId_FallsBackToNameIdentifier()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.NameIdentifier, "sub-fallback"),
            (ClaimTypes.Email, "fallback@contoso.local"),
            (ClaimTypes.Name, "Fallback User"));

        var info = ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        info.SubjectId.Should().Be("sub-fallback");
    }

    [Fact]
    public void ExtractUserInfo_EntraId_EmailFromPreferredUsername()
    {
        var principal = CreatePrincipal(
            ("oid", "oid-789"),
            ("preferred_username", "pref@contoso.local"),
            (ClaimTypes.Name, "Pref User"));

        var info = ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        info.Email.Should().Be("pref@contoso.local");
    }

    [Fact]
    public void ExtractUserInfo_EntraId_NoSubjectId_Throws()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.Email, "noid@contoso.local"));

        var act = () => ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*subject ID*Entra ID*");
    }

    // --- Generic OAuth claim extraction ---

    [Fact]
    public void ExtractUserInfo_GenericOAuth_UsesNameIdentifier()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.NameIdentifier, "google-sub-123"),
            (ClaimTypes.Email, "user@gmail.com"),
            (ClaimTypes.Name, "Google User"));

        var info = ExternalClaimMapper.ExtractUserInfo("Google", principal);

        info.SubjectId.Should().Be("google-sub-123");
        info.Email.Should().Be("user@gmail.com");
        info.DisplayName.Should().Be("Google User");
    }

    [Fact]
    public void ExtractUserInfo_GenericOAuth_FallsBackToSubClaim()
    {
        var principal = CreatePrincipal(
            ("sub", "github-sub-456"),
            ("email", "user@github.com"),
            ("name", "GitHub User"));

        var info = ExternalClaimMapper.ExtractUserInfo("GitHub", principal);

        info.SubjectId.Should().Be("github-sub-456");
    }

    [Fact]
    public void ExtractUserInfo_GenericOAuth_NoSubjectId_Throws()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.Email, "noid@gmail.com"));

        var act = () => ExternalClaimMapper.ExtractUserInfo("Google", principal);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*subject ID*external*");
    }

    // --- Display name fallback ---

    [Fact]
    public void ExtractUserInfo_NoName_FallsBackToEmail()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.NameIdentifier, "sub-noname"),
            (ClaimTypes.Email, "noname@gmail.com"));

        var info = ExternalClaimMapper.ExtractUserInfo("Google", principal);

        info.DisplayName.Should().Be("noname@gmail.com");
    }

    [Fact]
    public void ExtractUserInfo_NoNameOrEmail_FallsBackToUnknown()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.NameIdentifier, "sub-bare"));

        var info = ExternalClaimMapper.ExtractUserInfo("Google", principal);

        info.DisplayName.Should().Be("Unknown");
        info.Email.Should().BeNull();
    }

    // --- Email extraction ---

    [Fact]
    public void ExtractUserInfo_GenericOAuth_EmailFromEmailClaim()
    {
        var principal = CreatePrincipal(
            (ClaimTypes.NameIdentifier, "sub-email"),
            ("email", "shortclaim@gmail.com"),
            (ClaimTypes.Name, "Email User"));

        var info = ExternalClaimMapper.ExtractUserInfo("Google", principal);

        info.Email.Should().Be("shortclaim@gmail.com");
    }

    [Fact]
    public void ExtractUserInfo_EntraId_PrefersStandardEmailOverPreferredUsername()
    {
        var principal = CreatePrincipal(
            ("oid", "oid-pref"),
            (ClaimTypes.Email, "standard@contoso.local"),
            ("preferred_username", "preferred@contoso.local"),
            (ClaimTypes.Name, "Pref Test"));

        var info = ExternalClaimMapper.ExtractUserInfo("EntraId", principal);

        info.Email.Should().Be("standard@contoso.local");
    }

    // --- Helper ---

    private static ClaimsPrincipal CreatePrincipal(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(c => new Claim(c.type, c.value)),
            "TestScheme");
        return new ClaimsPrincipal(identity);
    }
}
