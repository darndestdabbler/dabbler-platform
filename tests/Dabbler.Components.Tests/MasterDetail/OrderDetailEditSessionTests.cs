namespace Dabbler.Components.Tests.MasterDetail;

using System;
using System.Collections.Generic;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Xunit;

// ── Local test model mirroring OrderDto fields used by OrderDetail.razor ─────

internal enum OrderTestStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }

internal sealed class OrderEditTestDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ShipDate { get; set; }
    public OrderTestStatus Status { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public List<OrderItemTestDto> OrderItems { get; set; } = new();
}

internal sealed class OrderItemTestDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class OrderDetailEditSessionTests
{
    private static OrderEditTestDto CreateTestOrder() => new()
    {
        Id = 1,
        CustomerName = "Test Customer",
        OrderDate = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
        ShipDate = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc),
        Status = OrderTestStatus.Pending,
        Total = 150.75m,
        Notes = "Initial order notes.",
        ModifiedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void New_Session_Starts_With_IsDirty_False()
    {
        var order = CreateTestOrder();
        var session = new EditSession<OrderEditTestDto>(order);

        session.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Changing_Notes_And_Calling_NotifyChanged_Flips_IsDirty_To_True()
    {
        var order = CreateTestOrder();
        var session = new EditSession<OrderEditTestDto>(order);

        session.Live.Notes = "Updated notes";
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Changing_ShipDate_And_Calling_NotifyChanged_Flips_IsDirty_To_True()
    {
        var order = CreateTestOrder();
        var session = new EditSession<OrderEditTestDto>(order);

        session.Live.ShipDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Undo_After_Notes_Change_Restores_Notes_And_IsDirty_Is_False()
    {
        var order = CreateTestOrder();
        var originalNotes = order.Notes;
        var session = new EditSession<OrderEditTestDto>(order);

        session.Live.Notes = "This change will be undone.";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();

        session.Undo();

        session.IsDirty.Should().BeFalse();
        session.Live.Notes.Should().Be(originalNotes);
    }

    [Fact]
    public void CommitFrom_Freezes_Server_Returned_Values_As_New_Baseline()
    {
        var order = CreateTestOrder();
        var session = new EditSession<OrderEditTestDto>(order);

        var serverReturned = CreateTestOrder();
        serverReturned.ModifiedDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        serverReturned.ModifiedBy = "test_user";
        serverReturned.Status = OrderTestStatus.Confirmed;

        session.CommitFrom(serverReturned);

        session.IsDirty.Should().BeFalse();
        session.Live.ModifiedBy.Should().Be("test_user");
        session.Live.Status.Should().Be(OrderTestStatus.Confirmed);

        // A new change is dirty against the committed baseline
        session.Live.Notes = "Another change.";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Commit_After_Item_Add_Resets_Baseline_For_Subsequent_Header_Changes()
    {
        var order = CreateTestOrder();
        var session = new EditSession<OrderEditTestDto>(order);

        // Simulate item added in-place (as done in OrderDetail.razor add-item handler)
        session.Live.OrderItems.Add(new OrderItemTestDto { ProductId = 99, Quantity = 1 });
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();

        // Commit commits the add as new baseline (as done after add-item server call)
        session.Commit();
        session.IsDirty.Should().BeFalse();

        // Subsequent Notes change is dirty against new baseline
        session.Live.Notes = "Notes after item add.";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();
    }
}
