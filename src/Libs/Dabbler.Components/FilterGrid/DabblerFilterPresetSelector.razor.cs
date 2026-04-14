// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A companion component for <see cref="DabblerFilterGrid{T}"/> that provides a UI
/// for saving, loading, renaming, and deleting named filter presets.
/// </summary>
/// <typeparam name="T">The row data type of the linked filter grid.</typeparam>
public partial class DabblerFilterPresetSelector<T> : ComponentBase, IDisposable
{
    private string? _selectedPresetName;
    private bool _isDirty;
    private bool _showSaveDialog;
    private bool _showRenameDialog;
    private string _newPresetName = "";
    private string _renamePresetName = "";

    /// <summary>
    /// The linked filter grid instance.
    /// </summary>
    [Parameter]
    public DabblerFilterGrid<T>? FilterGrid { get; set; }

    /// <summary>
    /// The available filter presets.
    /// </summary>
    [Parameter]
    public IReadOnlyList<FilterPreset> Presets { get; set; } = [];

    /// <summary>
    /// Raised when the preset list changes (e.g., after save or delete).
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FilterPreset>> PresetsChanged { get; set; }

    /// <summary>
    /// Fired when the user saves a new or updated preset. The consumer handles persistence.
    /// </summary>
    [Parameter]
    public EventCallback<FilterPreset> OnSavePreset { get; set; }

    /// <summary>
    /// Fired when the user deletes a preset. The consumer handles persistence.
    /// </summary>
    [Parameter]
    public EventCallback<FilterPreset> OnDeletePreset { get; set; }

    /// <summary>
    /// Whether presets can be renamed. Defaults to true.
    /// </summary>
    [Parameter]
    public bool AllowRename { get; set; } = true;

    /// <summary>
    /// Whether the "Save As" button (overwrite selected preset with current filters) is shown.
    /// Defaults to true.
    /// </summary>
    [Parameter]
    public bool AllowSaveAs { get; set; } = true;

    /// <summary>
    /// Label shown above the selector. Defaults to "Saved Filters".
    /// </summary>
    [Parameter]
    public string? Label { get; set; } = "Saved Filters";

    private FilterPreset? SelectedPreset =>
        Presets.FirstOrDefault(p =>
            string.Equals(p.Name, _selectedPresetName, StringComparison.Ordinal));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (FilterGrid is not null)
        {
            FilterGrid.Coordinator.FilterChanged -= OnCoordinatorFilterChanged;
            FilterGrid.Coordinator.FilterChanged += OnCoordinatorFilterChanged;
        }
    }

    private void OnCoordinatorFilterChanged()
    {
        _isDirty = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnPresetSelected(string? presetName)
    {
        _selectedPresetName = presetName;
        _isDirty = false;

        if (SelectedPreset is { } preset && FilterGrid is not null)
        {
            preset.LastUsedDate = DateTime.UtcNow;
            FilterGrid.ImportFilter(preset.FilterState);
            FilterGrid.ApplyColumnVisibility(preset.ColumnVisibility);
        }
    }

    private void OpenSaveDialog()
    {
        _newPresetName = "";
        _showSaveDialog = true;
    }

    private async Task SaveNewPresetAsync()
    {
        if (string.IsNullOrWhiteSpace(_newPresetName) || FilterGrid is null)
        {
            _showSaveDialog = false;
            return;
        }

        var preset = new FilterPreset
        {
            Name = _newPresetName.Trim(),
            FilterState = FilterGrid.ExportFilter(),
            ColumnVisibility = FilterGrid.GetColumnVisibilityForPreset(),
            CreatedDate = DateTime.UtcNow,
            LastUsedDate = DateTime.UtcNow,
        };

        var updated = Presets.ToList();
        updated.Add(preset);
        _selectedPresetName = preset.Name;
        _isDirty = false;
        _showSaveDialog = false;

        if (OnSavePreset.HasDelegate)
            await OnSavePreset.InvokeAsync(preset);

        if (PresetsChanged.HasDelegate)
            await PresetsChanged.InvokeAsync(updated);
    }

    private async Task SaveAsCurrentPresetAsync()
    {
        if (SelectedPreset is null || FilterGrid is null)
            return;

        SelectedPreset.FilterState = FilterGrid.ExportFilter();
        SelectedPreset.ColumnVisibility = FilterGrid.GetColumnVisibilityForPreset();
        SelectedPreset.LastUsedDate = DateTime.UtcNow;
        _isDirty = false;

        if (OnSavePreset.HasDelegate)
            await OnSavePreset.InvokeAsync(SelectedPreset);
    }

    private void OpenRenameDialog()
    {
        if (SelectedPreset is null) return;
        _renamePresetName = SelectedPreset.Name;
        _showRenameDialog = true;
    }

    private async Task ApplyRenameAsync()
    {
        if (string.IsNullOrWhiteSpace(_renamePresetName) || SelectedPreset is null)
        {
            _showRenameDialog = false;
            return;
        }

        SelectedPreset.Name = _renamePresetName.Trim();
        _selectedPresetName = SelectedPreset.Name;
        _showRenameDialog = false;

        if (OnSavePreset.HasDelegate)
            await OnSavePreset.InvokeAsync(SelectedPreset);

        if (PresetsChanged.HasDelegate)
            await PresetsChanged.InvokeAsync(Presets);
    }

    private async Task DeleteSelectedPresetAsync()
    {
        if (SelectedPreset is null) return;

        var toDelete = SelectedPreset;
        var updated = Presets.Where(p => !ReferenceEquals(p, toDelete)).ToList();
        _selectedPresetName = null;
        _isDirty = false;

        if (OnDeletePreset.HasDelegate)
            await OnDeletePreset.InvokeAsync(toDelete);

        if (PresetsChanged.HasDelegate)
            await PresetsChanged.InvokeAsync(updated);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (FilterGrid is not null)
            FilterGrid.Coordinator.FilterChanged -= OnCoordinatorFilterChanged;
    }
}
