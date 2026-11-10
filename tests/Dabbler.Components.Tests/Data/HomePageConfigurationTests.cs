using Dabbler.Components.Data.Metadata;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.Data;

public class HomePageConfigurationTests
{
    [Fact]
    public void ResolveHomePage_UsesFirstMatchingRoleOverride()
    {
        var configuration = new HomePageConfiguration
        {
            DefaultHomePage = "Form:Customer",
            HomePagePerRole =
            {
                ["Manager"] = "Dashboard:ops",
                ["BackofficeUser"] = "Form:Order"
            }
        };

        configuration.ResolveHomePage(["Manager", "BackofficeUser"]).Should().Be("Dashboard:ops");
    }

    [Fact]
    public void ResolveHomePage_FallsBackToDefaultWhenNoRoleOverrideMatches()
    {
        var configuration = new HomePageConfiguration
        {
            DefaultHomePage = "Form:Customer",
            HomePagePerRole =
            {
                ["Manager"] = "Dashboard:ops"
            }
        };

        configuration.ResolveHomePage(["Customer"]).Should().Be("Form:Customer");
    }
}
