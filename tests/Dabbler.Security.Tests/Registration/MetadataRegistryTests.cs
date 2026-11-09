using Dabbler.Security.Options;
using Dabbler.Security.Registration;

namespace Dabbler.Security.Tests.Registration;

public sealed class MetadataRegistryTests
{
    private static MetadataRegistry BuildRegistry(Action<DabblerSecurityOptions> configure)
    {
        var opts = new DabblerSecurityOptions();
        configure(opts);
        var resource = new ResourceRegistry(
            opts.RegisteredPages,
            opts.RegisteredEntities,
            opts.RegisteredEndpoints,
            opts.RegisteredControls);
        return new MetadataRegistry(resource);
    }

    [Fact]
    public void Tables_ReflectRegisteredEntities()
    {
        var registry = BuildRegistry(opts => opts.RegisterEntities(e =>
        {
            e.Add("Customer", "Customers", f =>
            {
                f.Add("Email", "Email");
                f.Add("Phone", "Phone");
            });
            e.Add("Product", "Products");
        }));

        registry.Tables.Select(t => t.Key).Should().BeEquivalentTo(new[] { "Customer", "Product" });
    }

    [Fact]
    public void GetTable_IsCaseInsensitive()
    {
        var registry = BuildRegistry(opts => opts.RegisterEntities(e =>
            e.Add("Customer", "Customers")));

        registry.GetTable("customer").Should().NotBeNull();
        registry.GetTable("CUSTOMER").Should().NotBeNull();
        registry.GetTable("unknown").Should().BeNull();
    }

    [Fact]
    public void GetColumns_ReturnsFields()
    {
        var registry = BuildRegistry(opts => opts.RegisterEntities(e =>
            e.Add("Customer", "Customers", f =>
            {
                f.Add("Email", "Email");
                f.Add("Phone", "Phone");
            })));

        registry.GetColumns("Customer").Select(c => c.Key)
            .Should().BeEquivalentTo(new[] { "Email", "Phone" });
    }

    [Fact]
    public void GetColumns_ReturnsEmpty_ForUnknownTable()
    {
        var registry = BuildRegistry(opts => opts.RegisterEntities(e =>
            e.Add("Customer", "Customers")));

        registry.GetColumns("Unknown").Should().BeEmpty();
        registry.GetColumns(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Pages_ReflectRegisteredPages()
    {
        var registry = BuildRegistry(opts => opts.RegisterPages(p =>
        {
            p.Add("/admin", "Administration");
            p.Add("/orders", "Orders", "Sales orders");
        }));

        registry.Pages.Select(p => p.Key).Should().BeEquivalentTo(new[] { "/admin", "/orders" });
    }

    [Fact]
    public void GetControls_FiltersByPage()
    {
        var registry = BuildRegistry(opts =>
        {
            opts.RegisterPages(p =>
            {
                p.Add("/orders", "Orders");
                p.Add("/products", "Products");
            });
            opts.RegisterControls(c =>
            {
                c.Add("DeleteOrderButton", "/orders", "Delete order");
                c.Add("EditOrderButton", "/orders", "Edit order");
                c.Add("DeleteProductButton", "/products", "Delete product");
            });
        });

        registry.GetControls("/orders").Select(c => c.Key)
            .Should().BeEquivalentTo(new[] { "DeleteOrderButton", "EditOrderButton" });
        registry.GetControls("/products").Select(c => c.Key)
            .Should().BeEquivalentTo(new[] { "DeleteProductButton" });
        registry.GetControls("/unknown").Should().BeEmpty();
    }

    [Fact]
    public void Empty_ExposesEmptyCollections()
    {
        MetadataRegistry.Empty.Tables.Should().BeEmpty();
        MetadataRegistry.Empty.Pages.Should().BeEmpty();
    }

    [Fact]
    public void EntirePageWildcardConstant_IsStarCharacter()
    {
        MetadataRegistry.EntirePageControlKey.Should().Be("*");
        MetadataRegistry.EntirePageDisplayName.Should().Be("Entire Page");
    }
}
