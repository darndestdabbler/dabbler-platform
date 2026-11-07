using Dabbler.Components.MasterDetail;
using FluentAssertions;
using System;
using System.Text.Json;
using Xunit;

namespace Dabbler.Components.Tests.MasterDetail;

internal sealed class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime Created { get; set; }
    public TestColor Color { get; set; }
    public string? Description { get; set; }
}

internal enum TestColor { Red, Blue, Green }

public sealed class EditSessionTests
{
    private static TestProduct CreateInitialProduct() => new()
    {
        Id = 1,
        Name = "Test Widget",
        UnitPrice = 9.99m,
        IsAvailable = true,
        Created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        Color = TestColor.Blue,
        Description = "A widget for testing"
    };

    [Fact]
    public void Constructor_WithNonNullLive_SetsIsDirtyToFalse()
    {
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);
        session.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void NotifyChanged_AfterPropertyMutation_SetsIsDirtyToTrue()
    {
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);

        session.Live.Name = "Updated Widget";
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void NotifyChanged_AfterRoundTripMutation_KeepsIsDirtyFalse()
    {
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);
        var originalName = product.Name;

        session.Live.Name = "Temporary Name";
        session.Live.Name = originalName;
        session.NotifyChanged();

        session.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Undo_AfterMutation_RestoresAllPropertiesAndResetsIsDirty()
    {
        var initialProduct = CreateInitialProduct();
        var session = new EditSession<TestProduct>(initialProduct);

        session.Live.Name = "Changed";
        session.Live.UnitPrice = 123.45m;
        session.Live.IsAvailable = false;
        session.Live.Created = DateTime.UtcNow;
        session.Live.Color = TestColor.Red;
        session.Live.Description = null;
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();

        session.Undo();

        session.IsDirty.Should().BeFalse();
        session.Live.Should().BeEquivalentTo(CreateInitialProduct());
    }

    [Fact]
    public void Commit_AfterMutation_EstablishesNewBaseline()
    {
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);

        session.Live.Name = "New Baseline Name";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();

        session.Commit();

        session.IsDirty.Should().BeFalse();

        // Further mutation should make it dirty again against the new baseline
        session.Live.UnitPrice = 100.00m;
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void CommitFrom_WithFreshObject_OverwritesLiveAndCommits()
    {
        var originalProduct = CreateInitialProduct();
        var session = new EditSession<TestProduct>(originalProduct);

        var freshProduct = new TestProduct
        {
            Id = 2,
            Name = "Fresh Product",
            UnitPrice = 50.00m,
            IsAvailable = false,
            Created = new DateTime(2025, 1, 1),
            Color = TestColor.Green,
            Description = "Fresh description"
        };

        session.CommitFrom(freshProduct);

        session.IsDirty.Should().BeFalse();
        session.Live.Should().BeEquivalentTo(freshProduct);
        // Ensure the original object instance was updated, not replaced
        session.Live.Should().BeSameAs(originalProduct);
    }

    [Fact]
    public void Constructor_WithNullLive_ThrowsArgumentNullException()
    {
        Action act = () => new EditSession<TestProduct>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CommitFrom_WithNullFreshObject_ThrowsArgumentNullException()
    {
        var session = new EditSession<TestProduct>(CreateInitialProduct());
        Action act = () => session.CommitFrom(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Undo_WhenNotDirty_IsIdempotent()
    {
        var initialProduct = CreateInitialProduct();
        var session = new EditSession<TestProduct>(initialProduct);

        session.Undo();

        session.IsDirty.Should().BeFalse();
        session.Live.Should().BeEquivalentTo(initialProduct);
    }

    [Fact]
    public void Undo_AfterCommit_RestoresCommittedBaselineNotOriginal()
    {
        // Mutate -> Commit shifts the baseline. A subsequent mutate+Undo must
        // restore the COMMITTED state, not the original construction state.
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);

        session.Live.Name = "Phase Two Name";
        session.Live.UnitPrice = 999.00m;
        session.NotifyChanged();
        session.Commit();

        session.Live.Name = "Should-be-undone";
        session.Live.UnitPrice = 12.34m;
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();

        session.Undo();

        session.IsDirty.Should().BeFalse();
        session.Live.Name.Should().Be("Phase Two Name");
        session.Live.UnitPrice.Should().Be(999.00m);
    }

    [Fact]
    public void NotifyChanged_CalledMultipleTimes_DoesNotFalselyToggleIsDirty()
    {
        var product = CreateInitialProduct();
        var session = new EditSession<TestProduct>(product);

        session.Live.Name = "Updated Name";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue();

        session.NotifyChanged(); // Second call
        session.IsDirty.Should().BeTrue();

        session.Undo();
        session.IsDirty.Should().BeFalse();

        session.NotifyChanged(); // Call when not dirty
        session.IsDirty.Should().BeFalse();
    }
}
