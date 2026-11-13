using Dabbler.Components.Data;
using Dabbler.Components.Data.Metadata;
using Dabbler.Platform.Stubs;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Components.Tests.StubPlatform;

public class StubPlatformRegistrationTests
{
    [Fact]
    public void AddDabblerPlatformStubs_RegistersMetadataNavigationAndAuth()
    {
        var services = new ServiceCollection();
        var definition = new StubPlatformDefinition
        {
            Entities =
            [
                new EntityMetadata
                {
                    EntityName = "Customer",
                    DisplayName = "Customers",
                    DisplayNameSingular = "Customer"
                }
            ]
        };

        definition.Navigation.AddRecords("Customer", "Recent", [new NavigationRecord(1, "Ada")]);

        services.AddDabblerPlatformStubs(definition);
        services.AddStubGridDataProvider([new SampleRow(1, "Ada")]);
        services.AddStubAuthentication(new StubUserContext
        {
            UserId = "stub-user",
            Roles = ["BackofficeUser"],
            OrganizationId = "org-1"
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEntityMetadataProvider>()
            .GetEntityMetadata("Customer")
            .Should()
            .NotBeNull();
        provider.GetRequiredService<INavigationDataProvider>().Should().BeOfType<StubNavigationDataProvider>();
        provider.GetRequiredService<IGridDataProvider<SampleRow>>().Should().BeOfType<StubGridDataProvider<SampleRow>>();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>()
            .Should()
            .BeOfType<StubAuthenticationStateProvider>();
    }

    private sealed record SampleRow(int Id, string Name);
}
