namespace Dabbler.Components.Tests.MasterDetail;

using System;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Xunit;

public class CustomerDetailEditSessionTests
{
    private sealed class CustomerTestRecord
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public bool IsActive { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    private static CustomerTestRecord CreateTestRecord() => new()
    {
        Id = 42,
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@example.com",
        Phone = "555-1234",
        City = "Metropolis",
        State = "NY",
        IsActive = true,
        ModifiedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Initial_IsDirty_Is_False()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);

        session.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Edit_LastName_Makes_IsDirty_True()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);

        session.Live.LastName = "Smith";
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Edit_Email_Makes_IsDirty_True()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);

        session.Live.Email = "jane.smith@example.com";
        session.NotifyChanged();

        session.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Undo_Restores_All_Fields_And_IsDirty_Is_False()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);
        var originalLastName = record.LastName;
        var originalEmail = record.Email;
        var originalIsActive = record.IsActive;

        session.Live.LastName = "Changed";
        session.Live.Email = "changed@example.com";
        session.Live.IsActive = false;
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue("should be dirty after edits");

        session.Undo();

        session.IsDirty.Should().BeFalse("should be clean after Undo");
        session.Live.LastName.Should().Be(originalLastName);
        session.Live.Email.Should().Be(originalEmail);
        session.Live.IsActive.Should().Be(originalIsActive);
    }

    [Fact]
    public void CommitFrom_Sets_New_Baseline_And_IsDirty_Is_False()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);

        var serverReturned = CreateTestRecord();
        serverReturned.ModifiedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        serverReturned.ModifiedBy = "admin";

        session.CommitFrom(serverReturned);

        session.IsDirty.Should().BeFalse("CommitFrom should clear dirty flag");
        session.Live.ModifiedBy.Should().Be("admin");
        session.Live.ModifiedDate.Should().Be(serverReturned.ModifiedDate);

        // Subsequent change should re-dirty
        session.Live.City = "Gotham";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue("should be dirty after new change post-CommitFrom");
    }

    [Fact]
    public void Commit_Freezes_Current_State_And_IsDirty_Is_False()
    {
        var record = CreateTestRecord();
        var session = new EditSession<CustomerTestRecord>(record);

        session.Live.LastName = "Jones";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue("should be dirty after edit");

        session.Commit();

        session.IsDirty.Should().BeFalse("Commit should clear dirty flag");
        session.Live.LastName.Should().Be("Jones");

        // Subsequent change should re-dirty
        session.Live.City = "Star City";
        session.NotifyChanged();
        session.IsDirty.Should().BeTrue("should be dirty after change post-Commit");
    }
}
