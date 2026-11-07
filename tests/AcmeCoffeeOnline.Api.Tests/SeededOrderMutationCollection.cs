namespace AcmeCoffeeOnline.Api.Tests;

/// <summary>
/// xUnit collection for tests that mutate the first-seeded Order's columns
/// (Notes, ShipDate, etc.) and need to read those mutations back. Tests in
/// this collection run serially relative to each other AND relative to other
/// collections (<see cref="DisableParallelization"/> = true) so a concurrent
/// test cannot stomp on the row between the write and the read-back.
///
/// Originally introduced 2026-05-15 in the packaging-and-template-readiness
/// Session 4 closeout, after the full-suite test run surfaced that
/// <c>OrderUpdateTests.SystemAdmin_CanUpdateOrderNotesAndShipDateAsync</c>
/// and
/// <c>TemporalTablesTests.Order_TemporalAll_ReturnsCurrentAndHistoricalVersionsAsync</c>
/// both target <c>GetFirstSeededOrderAsync()</c>'s <c>Notes</c> column and
/// raced under the default xUnit cross-class parallelism. Both tests pass
/// alone; the failure is purely a test-design data contention.
///
/// A test belongs here when it: (a) reads-modifies-writes-reads-back a row
/// that another test in the assembly also mutates, AND (b) cannot easily be
/// rewritten to use a per-test row. The proper long-term fix is per-test
/// row isolation, but that requires test-data refactoring out of scope for
/// the session where the failure was discovered.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SeededOrderMutationCollection
{
    public const string Name = "seeded-order-mutation";
}
