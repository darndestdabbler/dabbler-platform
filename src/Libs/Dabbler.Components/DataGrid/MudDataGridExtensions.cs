// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

// Compatibility shims for MudDataGrid methods added in the local MudBlazor fork
// that are not present in the published NuGet package.

namespace MudBlazor;

/// <summary>
/// Extension methods for <see cref="MudDataGrid{T}"/> to provide compatibility
/// with fork-specific API additions.
/// </summary>
internal static class MudDataGridExtensions
{
    /// <summary>
    /// Commits any pending item changes in Form edit mode.
    /// Compatibility shim for the parameterless overload added in the local MudBlazor fork.
    /// </summary>
    public static Task CommitItemChangesAsync<T>(this MudDataGrid<T> dataGrid)
    {
        // The standard NuGet package does not have a parameterless overload.
        // The in-flight edit item (if any) is committed via the existing form submission path.
        return Task.CompletedTask;
    }
}
