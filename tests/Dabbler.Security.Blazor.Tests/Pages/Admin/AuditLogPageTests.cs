using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Security.Blazor.Tests.Pages.Admin;

public class AuditLogPageTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IAuditService> _auditSvcMock;

    public AuditLogPageTests()
    {
        _ctx = new TestContext();
        _auditSvcMock = new Mock<IAuditService>();
        _ctx.SetupForDabbler(
            roles: ["UserAdministrator"],
            configureServices: s => s.AddSingleton(_auditSvcMock.Object));
    }

    public void Dispose() => _ctx.Dispose();

    private static List<AuditEntry> MakeEntries(int count = 3) =>
        Enumerable.Range(1, count).Select(i => new AuditEntry
        {
            Timestamp = DateTime.UtcNow.AddHours(-i),
            EntityType = i % 2 == 0 ? "AppUserRole" : "AppUser",
            EntityId = i.ToString(),
            ChangedBy = "1",
            Operation = i % 2 == 0 ? "Assigned" : "Created/Updated",
            Details = $"Detail {i}"
        }).ToList();

    [Fact]
    public void AuditLogPage_RendersHeading()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        cut.Markup.Should().Contain("Audit Log");
    }

    [Fact]
    public void AuditLogPage_LoadsDataOnInit()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        _auditSvcMock.Verify(s =>
            s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100), Times.Once);
    }

    [Fact]
    public void AuditLogPage_ShowsAuditEntries()
    {
        var entries = MakeEntries(3);
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync(entries);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        cut.WaitForAssertion(() =>
        {
            foreach (var entry in entries)
                cut.Markup.Should().Contain(entry.EntityType!);
        });
    }

    [Fact]
    public void AuditLogPage_ShowsFilterControls()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        cut.Markup.Should().Contain("From Date");
        cut.Markup.Should().Contain("To Date");
    }

    [Fact]
    public void AuditLogPage_ShowsFilterAndResetButtons()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        cut.Markup.Should().Contain("Filter");
        cut.Markup.Should().Contain("Reset");
    }

    [Fact]
    public void AuditLogPage_EmptyResults_DoesNotThrow()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var act = () => _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();
        act.Should().NotThrow();
    }

    [Fact]
    public void AuditLogPage_ShowsTableColumns()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();

        cut.Markup.Should().Contain("Timestamp");
        cut.Markup.Should().Contain("Entity Type");
        cut.Markup.Should().Contain("Operation");
        cut.Markup.Should().Contain("Changed By");
    }

    [Fact]
    public async Task AuditLogPage_ClickFilter_CallsServiceAgain()
    {
        _auditSvcMock
            .Setup(s => s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100))
            .ReturnsAsync([]);

        var cut = _ctx.RenderComponent<Dabbler.Security.Blazor.Pages.Admin.AuditLogPage>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Filter"));

        var filterButton = cut.FindAll("button")
            .First(b => b.TextContent.Trim() == "Filter");
        await cut.InvokeAsync(() => filterButton.Click());

        _auditSvcMock.Verify(s =>
            s.GetAllChangesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 100),
            Times.AtLeast(2));
    }
}
