using Dabbler.Security.Authorization;
using Dabbler.Security.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Dabbler.Security.Tests.Authorization;

public class DabblerAuthorizationPolicyProviderTests
{
    private static DabblerAuthorizationPolicyProvider CreateProvider()
    {
        var options = MicrosoftOptions.Create(new AuthorizationOptions());
        return new DabblerAuthorizationPolicyProvider(options);
    }

    [Fact]
    public async Task GetPolicyAsync_DabblerPagePolicy_ReturnsPolicyWithRequirement()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("DabblerPage:WidgetList");

        policy.Should().NotBeNull();
        var req = policy!.Requirements.OfType<PagePermissionRequirement>().SingleOrDefault();
        req.Should().NotBeNull();
        req!.PageKey.Should().Be("WidgetList");
    }

    [Fact]
    public async Task GetPolicyAsync_DabblerEndpointPolicy_ReturnsPolicyWithRequirement()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("DabblerEndpoint:widgets/list");

        policy.Should().NotBeNull();
        var req = policy!.Requirements.OfType<EndpointPermissionRequirement>().SingleOrDefault();
        req.Should().NotBeNull();
        req!.EndpointKey.Should().Be("widgets/list");
    }

    [Fact]
    public async Task GetPolicyAsync_NonDabblerPolicy_ReturnsNull()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync("SomeOtherPolicy");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_MalformedDabblerPagePolicy_ReturnsNull()
    {
        var provider = CreateProvider();

        // Empty pageKey after prefix
        var policy = await provider.GetPolicyAsync("DabblerPage:");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_RegisteredPolicy_ReturnedFromBase()
    {
        var authOptions = new AuthorizationOptions();
        authOptions.AddPolicy("CustomPolicy", pb => pb.RequireAuthenticatedUser());
        var provider = new DabblerAuthorizationPolicyProvider(MicrosoftOptions.Create(authOptions));

        var policy = await provider.GetPolicyAsync("CustomPolicy");

        policy.Should().NotBeNull();
    }
}
