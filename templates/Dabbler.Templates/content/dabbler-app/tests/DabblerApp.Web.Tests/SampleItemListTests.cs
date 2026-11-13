using Bunit;
using DabblerApp.Web.Components.Pages;
using DabblerApp.Web.Data;
using Dabbler.Platform.Stubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace DabblerApp.Web.Tests;

/// <summary>
/// bUnit smoke test for the SampleItems page using stub providers (§4.4).
/// Proves the component renders without the API host or a running SQL database.
/// </summary>
public sealed class SampleItemListTests : BunitContext, IAsyncLifetime
{
    private readonly AppDbContext _db;

    public SampleItemListTests()
    {
        // Set up in-memory EF Core DB with seed data (A13)
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"SampleItemListTest_{Guid.NewGuid()}")
            .Options);
        _db.SampleItems.AddRange(SampleItemSeed.GetItems());
        _db.SaveChanges();

        // Register MudBlazor and EF Core DB
        Services.AddSingleton(_db);
        Services.AddMudServices();

        // Register stub platform providers (bUnit opt-in per §4.4)
        Services.AddDabblerPlatformStubs(new StubPlatformDefinition());

        // Register the grid data provider backed by the in-memory DB
        Services.AddTransient<DabblerApp.Web.Services.SampleItemProvider>();
        Services.AddTransient<Dabbler.Components.Data.IGridDataProvider<SampleItem>,
            DabblerApp.Web.Services.SampleItemProvider>();
    }

    [Fact]
    public void SampleItemsPage_RendersGrid()
    {
        // The page uses MudDataGrid — verify it renders without exceptions.
        var cut = Render<SampleItems>();
        cut.WaitForElement("[data-testid='sample-items-grid']");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        _db.Dispose();
        await DisposeAsync();
    }
}
