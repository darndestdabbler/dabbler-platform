// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Groups fields in a two-column popup form under a section header.
/// Used with <see cref="PopupFormMode.TwoColumn"/> to organize fields into logical sections.
/// </summary>
public class PopupFormFieldGroup
{
    /// <summary>
    /// Optional section header text displayed above the group of fields.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// The property names of the fields to include in this group.
    /// Must match <see cref="GridColumnInfo.PropertyName"/> values.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; set; } = [];
}
