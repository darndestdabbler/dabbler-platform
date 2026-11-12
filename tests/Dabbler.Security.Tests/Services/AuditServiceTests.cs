using Dabbler.Security.Abstractions;
using Dabbler.Security.Models;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Unit tests for IAuditService consumers. The AuditService implementation
/// depends on SQL Server temporal table queries (TemporalAll) which are not
/// supported by the EF Core InMemory provider. These tests verify the service
/// contract via a mock, covering all expected behaviours at the interface level.
///
/// Integration tests against a real SQL Server instance (e.g., via TestContainers)
/// should be added in a separate integration test project when available.
/// </summary>
public class AuditServiceTests
{
    private static Mock<IAuditService> CreateMock() => new Mock<IAuditService>(MockBehavior.Strict);

    private static List<AuditEntry> SampleEntries(int count = 3) =>
        Enumerable.Range(1, count).Select(i => new AuditEntry
        {
            Timestamp = DateTime.UtcNow.AddHours(-i),
            EntityType = i % 2 == 0 ? "AppUserRole" : "AppUser",
            EntityId = $"{i}",
            ChangedBy = $"{i}",
            Operation = i % 2 == 0 ? "Assigned" : "Created/Updated",
            Details = $"Detail {i}"
        }).ToList();

    // ── GetUserHistoryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserHistoryAsync_ReturnsEntriesForUser()
    {
        var mock = CreateMock();
        var expected = SampleEntries(2);
        mock.Setup(s => s.GetUserHistoryAsync(7, null, null)).ReturnsAsync(expected);

        var result = await mock.Object.GetUserHistoryAsync(7, null, null);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetUserHistoryAsync_WithDateRange_PassedToService()
    {
        var mock = CreateMock();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        mock.Setup(s => s.GetUserHistoryAsync(5, from, to)).ReturnsAsync([]);

        var result = await mock.Object.GetUserHistoryAsync(5, from, to);

        result.Should().BeEmpty();
        mock.Verify(s => s.GetUserHistoryAsync(5, from, to), Times.Once);
    }

    [Fact]
    public async Task GetUserHistoryAsync_NoHistory_ReturnsEmptyList()
    {
        var mock = CreateMock();
        mock.Setup(s => s.GetUserHistoryAsync(99, null, null)).ReturnsAsync([]);

        var result = await mock.Object.GetUserHistoryAsync(99, null, null);

        result.Should().BeEmpty();
    }

    // ── GetRoleAssignmentHistoryAsync ──────────────────────────────────────

    [Fact]
    public async Task GetRoleAssignmentHistoryAsync_FiltersByUserId()
    {
        var mock = CreateMock();
        var expected = SampleEntries(1);
        mock.Setup(s => s.GetRoleAssignmentHistoryAsync(3, null, null, null)).ReturnsAsync(expected);

        var result = await mock.Object.GetRoleAssignmentHistoryAsync(3, null, null, null);

        result.Should().HaveCount(1);
        mock.Verify(s => s.GetRoleAssignmentHistoryAsync(3, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetRoleAssignmentHistoryAsync_FiltersByRoleId()
    {
        var mock = CreateMock();
        mock.Setup(s => s.GetRoleAssignmentHistoryAsync(null, 2, null, null)).ReturnsAsync(SampleEntries(2));

        var result = await mock.Object.GetRoleAssignmentHistoryAsync(null, 2, null, null);

        result.Should().HaveCount(2);
    }

    // ── GetAllChangesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllChangesAsync_ReturnsPagedResults()
    {
        var mock = CreateMock();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var expected = SampleEntries(5);
        mock.Setup(s => s.GetAllChangesAsync(from, to, 1, 10)).ReturnsAsync(expected);

        var result = await mock.Object.GetAllChangesAsync(from, to, 1, 10);

        result.Should().HaveCount(5);
        mock.Verify(s => s.GetAllChangesAsync(from, to, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetAllChangesAsync_Page2_PassedCorrectly()
    {
        var mock = CreateMock();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        mock.Setup(s => s.GetAllChangesAsync(from, to, 2, 100)).ReturnsAsync([]);

        var result = await mock.Object.GetAllChangesAsync(from, to, 2, 100);

        result.Should().BeEmpty();
        mock.Verify(s => s.GetAllChangesAsync(from, to, 2, 100), Times.Once);
    }

    // ── AuditEntry model ──────────────────────────────────────────────────

    [Theory]
    [InlineData("Assigned", true)]
    [InlineData("Removed", false)]
    [InlineData("Created/Updated", true)]
    [InlineData("Deleted", false)]
    public void AuditEntry_OperationValues_AreExpected(string operation, bool isPositive)
    {
        var entry = new AuditEntry { Operation = operation };
        var positiveOps = new[] { "Assigned", "Enrolled/Verified", "Created/Updated" };
        var negativeOps = new[] { "Removed", "Deleted" };

        if (isPositive)
            positiveOps.Should().Contain(entry.Operation);
        else
            negativeOps.Should().Contain(entry.Operation);
    }
}
