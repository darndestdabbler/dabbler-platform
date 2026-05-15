// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components.Compat;

/// <summary>
/// Represents a single tab in the <see cref="DabblerTabbedWorkspace"/>.
/// </summary>
public class WorkspaceTab
{
    /// <summary>Unique tab identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display title shown in the tab header.</summary>
    public string Title { get; set; } = "";

    /// <summary>The kind of content this tab hosts.</summary>
    public WorkspaceTabType TabType { get; set; }

    /// <summary>Entity name for Detail and Search tabs.</summary>
    public string? EntityName { get; set; }

    /// <summary>Primary key value for Detail tabs.</summary>
    public object? RecordKey { get; set; }

    /// <summary>Whether the tab has unsaved changes.</summary>
    public bool IsDirty { get; set; } = false;

    /// <summary>When the tab was opened.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The tab's rendered content.</summary>
    public RenderFragment? Content { get; set; }

    /// <summary>Arbitrary state bag for tab-specific data.</summary>
    public Dictionary<string, object?> State { get; set; } = new();
}
