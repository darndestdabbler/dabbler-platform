// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// Represents a report or dashboard entry in the navigation panel.
/// These are non-entity items displayed as flat clickable lists.
/// </summary>
/// <param name="Name">The internal name or identifier for the report.</param>
/// <param name="DisplayName">The display label shown in the navigation panel.</param>
/// <param name="IconName">Optional MudBlazor icon name.</param>
public record NavigationReportItem(string Name, string DisplayName, string? IconName = null);
