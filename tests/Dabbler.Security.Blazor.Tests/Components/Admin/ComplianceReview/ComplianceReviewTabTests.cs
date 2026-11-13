using Bunit;
using Dabbler.Components;
using Dabbler.Components.Csv;
using Dabbler.Components.Data;
using Dabbler.Security.Blazor.Components.Admin.ComplianceReview;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Data;
using Dabbler.Security.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.ComplianceReview;

/// <summary>
/// bUnit coverage for the Compliance Review viewer (Session 4 of the
/// <c>sensitive-field-masking-audit</c> set). Verifies that the grid renders
/// audit rows from the seeded EF in-memory store, that the row expander shows
/// every other row in the same correlation cluster, and that the Export CSV
/// button drives the JS-assisted download invocation.
/// </summary>
public sealed class ComplianceReviewTabTests
{
    private sealed class FactoryWithOptions : IDbContextFactory<DabblerSecurityDbContext>
    {
        private readonly DbContextOptions<DabblerSecurityDbContext> _options;
        public FactoryWithOptions(DbContextOptions<DabblerSecurityDbContext> options) => _options = options;
        public DabblerSecurityDbContext CreateDbContext() => new(_options);
    }

    private static (TestContext Ctx, DbContextOptions<DabblerSecurityDbContext> Options) Build(
        Action<DabblerSecurityDbContext>? seed = null)
    {
        var ctx = new TestContext();

        var options = new DbContextOptionsBuilder<DabblerSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        if (seed is not null)
        {
            using var seedDb = new DabblerSecurityDbContext(options);
            seed(seedDb);
            seedDb.SaveChanges();
        }

        ctx.SetupForDabbler(configureServices: svc =>
        {
            svc.AddSingleton<IDbContextFactory<DabblerSecurityDbContext>>(
                new FactoryWithOptions(options));
            svc.AddSingleton<ICsvExportService, CsvExportService>();
        });

        return (ctx, options);
    }

    [Fact]
    public void Tab_HeaderAndScrollRegion_RenderForAnyRole()
    {
        var (ctx, _) = Build();
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ComplianceReviewTab>();

        cut.Find("[data-testid='compliance-review-tab']").Should().NotBeNull();
        cut.Find("[data-testid='compliance-review-grid-scroll-region']").Should().NotBeNull();
        cut.Find("[data-testid='compliance-review-export-button']").Should().NotBeNull();
    }

    [Fact]
    public void Grid_RendersSeededRowsWithEventChips()
    {
        var correlation = Guid.NewGuid();
        var (ctx, _) = Build(seed =>
        {
            seed.AppSensitiveAccessLogs.Add(new AppSensitiveAccessLog
            {
                EventKind = SensitiveAccessEventKind.RoleSwitch,
                Timestamp = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                CorrelationId = correlation,
                UserId = 7,
                RoleName = "SystemAdmin",
            });
            seed.AppSensitiveAccessLogs.Add(new AppSensitiveAccessLog
            {
                EventKind = SensitiveAccessEventKind.FieldUnmaskedRead,
                Timestamp = new DateTime(2026, 4, 27, 12, 0, 1, DateTimeKind.Utc),
                CorrelationId = correlation,
                UserId = 7,
                RoleName = "Customer",
                EntityKey = "Customer",
                EntityId = "42",
                ColumnKey = "SocialSecurityNumber",
            });
        });
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ComplianceReviewTab>();

        // Wait for ServerData callback to populate.
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid$='-event-chip']").Should().NotBeEmpty();
        }, timeout: TimeSpan.FromSeconds(2));

        cut.Markup.Should().Contain("RoleSwitch");
        cut.Markup.Should().Contain("FieldUnmaskedRead");
        cut.Find("[data-testid='compliance-review-count']")
            .TextContent.Should().Contain("of 2");
    }

    [Fact]
    public async Task FilterByUserId_NarrowsGridToMatchingRows()
    {
        var (ctx, _) = Build(seed =>
        {
            seed.AppSensitiveAccessLogs.AddRange(
                new AppSensitiveAccessLog
                {
                    EventKind = SensitiveAccessEventKind.RoleSwitch,
                    Timestamp = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                    CorrelationId = Guid.NewGuid(),
                    UserId = 7,
                    RoleName = "SystemAdmin",
                },
                new AppSensitiveAccessLog
                {
                    EventKind = SensitiveAccessEventKind.RoleSwitch,
                    Timestamp = new DateTime(2026, 4, 27, 12, 0, 1, DateTimeKind.Utc),
                    CorrelationId = Guid.NewGuid(),
                    UserId = 9,
                    RoleName = "CrossOrgUserAdmin",
                });
        });
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ComplianceReviewTab>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='compliance-review-count']")
                .TextContent.Should().Contain("of 2"),
            timeout: TimeSpan.FromSeconds(2));

        var filterPanel = cut.FindComponent<FilterPanel<AppSensitiveAccessLog>>();

        await cut.InvokeAsync(() =>
            filterPanel.Instance.Coordinator.SetFilter(
                0,
                nameof(AppSensitiveAccessLog.UserId),
                FilterOperators.Equals,
                7));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='compliance-review-count']")
                .TextContent.Should().Contain("of 1"),
            timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RowExpander_ListsCorrelationSiblings_ExcludingAnchor()
    {
        var correlation = Guid.NewGuid();
        var (ctx, _) = Build(seed =>
        {
            seed.AppSensitiveAccessLogs.AddRange(
                new AppSensitiveAccessLog
                {
                    Id = 100,
                    EventKind = SensitiveAccessEventKind.RoleSwitch,
                    Timestamp = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                    CorrelationId = correlation,
                    UserId = 7,
                    RoleName = "SystemAdmin",
                },
                new AppSensitiveAccessLog
                {
                    Id = 101,
                    EventKind = SensitiveAccessEventKind.FieldUnmaskedRead,
                    Timestamp = new DateTime(2026, 4, 27, 12, 0, 1, DateTimeKind.Utc),
                    CorrelationId = correlation,
                    UserId = 7,
                    RoleName = "Customer",
                    EntityKey = "Customer",
                    EntityId = "42",
                    ColumnKey = "SocialSecurityNumber",
                });
        });
        using var _1 = ctx;

        var cut = ctx.RenderComponent<ComplianceReviewTab>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='compliance-review-count']")
                .TextContent.Should().Contain("of 2"),
            timeout: TimeSpan.FromSeconds(2));

        var instance = cut.Instance;

        // Anchor: row 100. Cluster should contain row 101 only (anchor excluded).
        var clusterFor100 = ResolveClusterViaReflection(instance, anchorId: 100, correlation);
        clusterFor100.Should().HaveCount(1);
        clusterFor100[0].Id.Should().Be(101);

        // Anchor: row 101. Cluster should contain row 100 only.
        var clusterFor101 = ResolveClusterViaReflection(instance, anchorId: 101, correlation);
        clusterFor101.Should().HaveCount(1);
        clusterFor101[0].Id.Should().Be(100);
    }

    private static List<AppSensitiveAccessLog> ResolveClusterViaReflection(
        ComplianceReviewTab instance, long anchorId, Guid correlationId)
    {
        var method = typeof(ComplianceReviewTab).GetMethod(
            "ResolveCorrelationCluster",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (List<AppSensitiveAccessLog>)method.Invoke(instance, new object[] { anchorId, correlationId })!;
    }

    [Fact]
    public async Task ExportButton_InvokesDownloadFileViaJsInterop()
    {
        var (ctx, _) = Build(seed =>
        {
            seed.AppSensitiveAccessLogs.Add(new AppSensitiveAccessLog
            {
                EventKind = SensitiveAccessEventKind.ExemptionGranted,
                Timestamp = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                CorrelationId = Guid.NewGuid(),
                UserId = 5,
                RoleName = "CrossOrgUserAdmin",
                EntityKey = "Customer",
                ColumnKey = "SocialSecurityNumber",
                TargetRoleName = "Customer",
                GranterUserId = 5,
            });
        });
        using var _1 = ctx;

        // Capture the dynamic ES-module reference + downloadFile call. bUnit
        // routes JS interop through its mock; loose mode means the import call
        // returns a stub IJSObjectReference whose method invocations are
        // recorded.
        var importHandler = ctx.JSInterop.SetupModule(
            "./_content/Dabbler.Components/dabbler-download.js");
        var downloadHandler = importHandler.SetupVoid(
            "downloadFile", _ => true);

        var cut = ctx.RenderComponent<ComplianceReviewTab>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid$='-event-chip']").Should().NotBeEmpty();
        }, timeout: TimeSpan.FromSeconds(2));

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='compliance-review-export-button']").Click());

        downloadHandler.VerifyInvoke("downloadFile");
        var invocation = downloadHandler.Invocations.Single();
        invocation.Arguments.Should().HaveCount(3);
        invocation.Arguments[0].Should().BeOfType<string>()
            .Which.Should().StartWith("compliance-review-");
        invocation.Arguments[2].Should().Be("text/csv");
    }
}
