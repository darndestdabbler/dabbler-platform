using Dabbler.Security.Options;
using Dabbler.Security.Registration;

namespace Dabbler.Security.Tests.Registration;

public sealed class ResourceRegistryTests
{
    // ------------------------------------------------------------------ pages

    [Fact]
    public void RegisterPages_BuildsCorrectRegistry()
    {
        var opts = new DabblerSecurityOptions();
        opts.RegisterPages(p =>
        {
            p.Add("widgets", "Widgets");
            p.Add("admin", "Administration", "Admin area");
        });

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Pages.Should().HaveCount(2);

        var widgets = registry.Pages.Single(p => p.Key == "widgets");
        widgets.DisplayName.Should().Be("Widgets");
        widgets.Description.Should().BeNull();

        var admin = registry.Pages.Single(p => p.Key == "admin");
        admin.DisplayName.Should().Be("Administration");
        admin.Description.Should().Be("Admin area");
    }

    // ---------------------------------------------------------------- entities

    [Fact]
    public void RegisterEntities_WithFields_BuildsCorrectRegistry()
    {
        var opts = new DabblerSecurityOptions();
        opts.RegisterEntities(e =>
        {
            e.Add("widget", "Widget", f =>
            {
                f.Add("name", "Name");
                f.Add("color", "Color");
            });
            e.Add("order", "Order");
        });

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Entities.Should().HaveCount(2);

        var widget = registry.Entities.Single(e => e.Key == "widget");
        widget.DisplayName.Should().Be("Widget");
        widget.Fields.Should().HaveCount(2);
        widget.Fields.Select(f => f.Key).Should().BeEquivalentTo(["name", "color"]);

        var order = registry.Entities.Single(e => e.Key == "order");
        order.Fields.Should().BeEmpty();
    }

    // --------------------------------------------------------------- endpoints

    [Fact]
    public void RegisterEndpoints_BuildsCorrectRegistry()
    {
        var opts = new DabblerSecurityOptions();
        opts.RegisterEndpoints(e =>
        {
            e.Add("get-widgets", "Get Widgets", httpMethod: "GET", route: "/api/widgets");
            e.Add("delete-widget", "Delete Widget", httpMethod: "DELETE");
        });

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Endpoints.Should().HaveCount(2);

        var getWidgets = registry.Endpoints.Single(e => e.Key == "get-widgets");
        getWidgets.DisplayName.Should().Be("Get Widgets");
        getWidgets.HttpMethod.Should().Be("GET");
        getWidgets.Route.Should().Be("/api/widgets");

        var deleteWidget = registry.Endpoints.Single(e => e.Key == "delete-widget");
        deleteWidget.HttpMethod.Should().Be("DELETE");
        deleteWidget.Route.Should().BeNull();
    }

    // --------------------------------------------------------------- controls

    [Fact]
    public void RegisterControls_BuildsCorrectRegistry()
    {
        var opts = new DabblerSecurityOptions();
        opts.RegisterControls(c =>
        {
            c.Add("delete-btn", "widgets", "Delete Button");
            c.Add("export-btn", "widgets", "Export Button");
        });

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Controls.Should().HaveCount(2);

        var deleteBtn = registry.Controls.Single(c => c.Key == "delete-btn");
        deleteBtn.PageKey.Should().Be("widgets");
        deleteBtn.DisplayName.Should().Be("Delete Button");
    }

    // ---------------------------------------------------- empty registration

    [Fact]
    public void NoRegistrations_ProducesEmptyRegistry()
    {
        var opts = new DabblerSecurityOptions();

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Pages.Should().BeEmpty();
        registry.Entities.Should().BeEmpty();
        registry.Endpoints.Should().BeEmpty();
        registry.Controls.Should().BeEmpty();
    }

    // -------------------------------------------------- static Empty instance

    [Fact]
    public void Empty_HasAllEmptyCollections()
    {
        ResourceRegistry.Empty.Pages.Should().BeEmpty();
        ResourceRegistry.Empty.Entities.Should().BeEmpty();
        ResourceRegistry.Empty.Endpoints.Should().BeEmpty();
        ResourceRegistry.Empty.Controls.Should().BeEmpty();
    }

    // -------------------------------------------------- duplicate keys retained

    [Fact]
    public void DuplicatePageKeys_AreRetained()
    {
        // Duplicate detection is the caller's responsibility; the registry stores what is given.
        var opts = new DabblerSecurityOptions();
        opts.RegisterPages(p =>
        {
            p.Add("home", "Home");
            p.Add("home", "Home (duplicate)");
        });

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Pages.Should().HaveCount(2);
    }

    // ----------------------------------------------- UnregisteredResourcePolicy

    [Fact]
    public void UnregisteredResourcePolicy_DefaultsToFailOpen()
    {
        var opts = new DabblerSecurityOptions();
        opts.UnregisteredResourcePolicy.Should().Be(UnregisteredResourcePolicy.FailOpen);
    }

    [Fact]
    public void UnregisteredResourcePolicy_CanBeSetToFailClosed()
    {
        var opts = new DabblerSecurityOptions
        {
            UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailClosed
        };
        opts.UnregisteredResourcePolicy.Should().Be(UnregisteredResourcePolicy.FailClosed);
    }

    // ----------------------------------------- multiple RegisterX calls accumulate

    [Fact]
    public void MultipleRegisterCalls_Accumulate()
    {
        var opts = new DabblerSecurityOptions();
        opts.RegisterPages(p => p.Add("home", "Home"));
        opts.RegisterPages(p => p.Add("about", "About"));

        var registry = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);

        registry.Pages.Should().HaveCount(2);
        registry.Pages.Select(p => p.Key).Should().BeEquivalentTo(["home", "about"]);
    }
}
