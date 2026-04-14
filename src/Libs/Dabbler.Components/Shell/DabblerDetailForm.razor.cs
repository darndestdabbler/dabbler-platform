// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Reflection;
using Dabbler.Components.Data;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Utilities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A metadata-driven detail form for editing a single record.
/// Renders parent fields in a responsive grid and child-relationship tabs with lazy loading.
/// Supports dirty state tracking, undo, and popup-vs-tab navigation on child row click.
/// </summary>
public partial class DabblerDetailForm<T> : DabblerComponentBase where T : class, new()
{
    // ── Dirty-state snapshot ─────────────────────────────────────────
    private Dictionary<string, object?> _originalValues = new();
    private bool _isDirty;
    private bool _previousDirtyState;
    private T? _lastSeenRecord;

    // ── Child tab state ──────────────────────────────────────────────
    private string? _activeChildTabId;
    private readonly HashSet<string> _loadedChildTabs = new();

    // ── M:M child grid state ────────────────────────────────────────
    private readonly Dictionary<string, IReadOnlyList<Dictionary<string, object?>>> _childGridData = new();
    private readonly Dictionary<string, bool> _childGridLoading = new();
    private readonly Dictionary<string, int> _selectedChildRowIndex = new();

    // ── M:M Add dialog state ────────────────────────────────────────
    private bool _showM2MAddDialog;
    private ChildRelationshipMetadata? _m2mAddRelationship;
    private IReadOnlyList<NavigationRecord>? _m2mFarSideItems;
    private NavigationRecord? _m2mSelectedFarSideRecord;
    private readonly Dictionary<string, string> _m2mJunctionValues = new();

    // ── Parameters ───────────────────────────────────────────────────

    /// <summary>
    /// Metadata describing the entity's fields, relationships, and display rules.
    /// </summary>
    [Parameter, EditorRequired]
    public EntityMetadata EntityMetadata { get; set; } = null!;

    /// <summary>
    /// The record being edited.
    /// </summary>
    [Parameter, EditorRequired]
    public T Record { get; set; } = null!;

    /// <summary>
    /// Two-way binding callback for <see cref="Record"/>.
    /// </summary>
    [Parameter]
    public EventCallback<T> RecordChanged { get; set; }

    /// <summary>
    /// Whether this is a new (unsaved) record.
    /// </summary>
    [Parameter]
    public bool IsNew { get; set; }

    /// <summary>
    /// Data provider for child grids.
    /// </summary>
    [Parameter]
    public IGridDataProvider<T>? DataProvider { get; set; }

    /// <summary>
    /// Raised when the user clicks Save.
    /// </summary>
    [Parameter]
    public EventCallback<T> OnSave { get; set; }

    /// <summary>
    /// Raised when the user clicks Delete.
    /// </summary>
    [Parameter]
    public EventCallback<T> OnDelete { get; set; }

    /// <summary>
    /// Raised when dirty state changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnDirtyChanged { get; set; }

    /// <summary>
    /// Raised when a child row is clicked and should open in a new workspace tab.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, object PrimaryKey)> OnChildRecordSelected { get; set; }

    /// <summary>
    /// Raised when a child row should open in a popup dialog.
    /// </summary>
    [Parameter]
    public EventCallback<(string EntityName, object PrimaryKey, ChildRelationshipMetadata Relationship)> OnChildRecordPopup { get; set; }

    /// <summary>
    /// Additional toolbar buttons rendered after the default Save/Undo/Delete buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// Custom field rendering override. When provided, replaces the auto-generated editor
    /// for each field.
    /// </summary>
    [Parameter]
    public RenderFragment<FieldMetadata>? FieldTemplate { get; set; }

    /// <summary>
    /// Number of columns in the parent field layout (responsive: 1 on mobile).
    /// </summary>
    [Parameter]
    public int FormColumns { get; set; } = 3;

    /// <summary>
    /// Makes all fields read-only.
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Provides entity metadata for child entities (used for popup-vs-tab decisions).
    /// </summary>
    [Parameter]
    public IEntityMetadataProvider? MetadataProvider { get; set; }

    /// <summary>
    /// Factory for loading child grid data. Returns row dictionaries keyed by field name.
    /// For M:M relationships, rows contain merged junction + far-side values.
    /// </summary>
    [Parameter]
    public Func<ChildRelationshipMetadata, object?, Task<IReadOnlyList<Dictionary<string, object?>>>>? ChildDataFactory { get; set; }

    /// <summary>
    /// Factory for loading far-side entity lookup items for M:M Add workflow.
    /// </summary>
    [Parameter]
    public Func<string, Task<IReadOnlyList<NavigationRecord>>>? FarSideLookupFactory { get; set; }

    /// <summary>
    /// Raised when a new M:M junction row should be created.
    /// </summary>
    [Parameter]
    public EventCallback<(ChildRelationshipMetadata Relationship, object FarSideKey, Dictionary<string, string> JunctionValues)> OnMergedViewAdd { get; set; }

    /// <summary>
    /// Raised when an M:M junction row should be deleted.
    /// </summary>
    [Parameter]
    public EventCallback<(ChildRelationshipMetadata Relationship, object JunctionKey)> OnMergedViewDelete { get; set; }

    /// <summary>
    /// Raised when a new 1:M child record Add is requested.
    /// </summary>
    [Parameter]
    public EventCallback<ChildRelationshipMetadata> OnChildAddRequested { get; set; }

    /// <summary>
    /// Raised when a 1:M child record Delete is requested.
    /// </summary>
    [Parameter]
    public EventCallback<(ChildRelationshipMetadata Relationship, object ChildKey)> OnChildDeleteRequested { get; set; }

    // ── Public members ──────────��────────────────────────────────────

    /// <summary>
    /// Whether any field has been modified since the last save or initialization.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Whether all required fields have values.
    /// </summary>
    public bool IsValid => CheckValidity();

    /// <summary>
    /// Programmatic save — raises the <see cref="OnSave"/> callback.
    /// </summary>
    public async Task SaveAsync()
    {
        if (Record is not null && OnSave.HasDelegate)
        {
            await OnSave.InvokeAsync(Record);
            SnapshotRecord();
            await UpdateDirtyStateAsync();
        }
    }

    /// <summary>
    /// Reverts all field values to the last-saved (or initial) state.
    /// </summary>
    public void Undo()
    {
        if (Record is null) return;

        foreach (var kvp in _originalValues)
        {
            PropertyAccessor.SetValue(Record, kvp.Key, kvp.Value);
        }

        _ = InvokeAsync(async () =>
        {
            await UpdateDirtyStateAsync();
            StateHasChanged();
        });
    }

    /// <summary>
    /// Returns a dictionary of only the fields that changed since the last snapshot.
    /// </summary>
    public Dictionary<string, object?> GetModifiedFields()
    {
        var modified = new Dictionary<string, object?>();
        if (Record is null) return modified;

        foreach (var kvp in _originalValues)
        {
            var currentValue = PropertyAccessor.GetValue(Record, kvp.Key);
            if (!Equals(currentValue, kvp.Value))
            {
                modified[kvp.Key] = currentValue;
            }
        }

        return modified;
    }

    // ── Lifecycle ────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _lastSeenRecord = Record;
        SnapshotRecord();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // If the record instance changed externally, re-snapshot
        if (Record is not null && !ReferenceEquals(_lastSeenRecord, Record))
        {
            _lastSeenRecord = Record;
            SnapshotRecord();
            await UpdateDirtyStateAsync();
        }
    }

    // ── Snapshot / Dirty tracking ───────────��────────────────────────

    private void SnapshotRecord()
    {
        _originalValues.Clear();
        if (Record is null) return;

        var detailFields = GetDetailFieldMetadata();
        foreach (var field in detailFields)
        {
            var value = PropertyAccessor.GetValue(Record, field.FieldName);
            _originalValues[field.FieldName] = value;
        }

        _isDirty = false;
        _previousDirtyState = false;
    }

    private async Task UpdateDirtyStateAsync()
    {
        _isDirty = ComputeDirty();

        if (_isDirty != _previousDirtyState)
        {
            _previousDirtyState = _isDirty;
            if (OnDirtyChanged.HasDelegate)
            {
                await OnDirtyChanged.InvokeAsync(_isDirty);
            }
        }
    }

    private bool ComputeDirty()
    {
        if (Record is null) return false;

        foreach (var kvp in _originalValues)
        {
            var currentValue = PropertyAccessor.GetValue(Record, kvp.Key);
            if (!Equals(currentValue, kvp.Value))
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckValidity()
    {
        if (Record is null) return false;

        var detailFields = GetDetailFieldMetadata();
        foreach (var field in detailFields)
        {
            if (!field.IsRequired) continue;

            var value = PropertyAccessor.GetValue(Record, field.FieldName);
            if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                return false;
            }
        }

        return true;
    }

    // ── Field value change handler ───────────────────────────────────

    private async Task HandleFieldChangedAsync(string fieldName, object? newValue)
    {
        if (Record is null) return;

        PropertyAccessor.SetValue(Record, fieldName, newValue);

        if (RecordChanged.HasDelegate)
        {
            await RecordChanged.InvokeAsync(Record);
        }

        await UpdateDirtyStateAsync();
        StateHasChanged();
    }

    // ── Toolbar handlers ─────────────────────────────────────────────

    private async Task HandleSaveClickAsync()
    {
        await SaveAsync();
    }

    private void HandleUndoClick()
    {
        Undo();
    }

    private async Task HandleDeleteClickAsync()
    {
        if (Record is not null && OnDelete.HasDelegate)
        {
            await OnDelete.InvokeAsync(Record);
        }
    }

    // ── Child tab handlers ───────────────────────────────────────────

    private async Task HandleChildTabChangedAsync(string tabId)
    {
        _activeChildTabId = tabId;
        _loadedChildTabs.Add(tabId);

        // Load child grid data if we have a factory
        var rel = GetVisibleChildRelationships()
            .FirstOrDefault(r => r.RelationshipName == tabId);
        if (rel is not null
            && !_childGridData.ContainsKey(rel.RelationshipName)
            && ChildDataFactory is not null)
        {
            await LoadChildGridDataAsync(rel);
        }

        StateHasChanged();
    }

    private bool IsChildTabLoaded(string relationshipName)
    {
        return _loadedChildTabs.Contains(relationshipName);
    }

    // ── M:M data loading ────────────────────────────────────────────

    private object? GetParentPrimaryKey()
    {
        if (Record is null || EntityMetadata?.Fields is null) return null;
        var pkField = EntityMetadata.Fields.FirstOrDefault(f => f.IsPrimaryKey);
        if (pkField is null) return null;
        return PropertyAccessor.GetValue(Record, pkField.FieldName);
    }

    private async Task LoadChildGridDataAsync(ChildRelationshipMetadata rel)
    {
        if (ChildDataFactory is null) return;
        if (_childGridLoading.TryGetValue(rel.RelationshipName, out var loading) && loading) return;

        _childGridLoading[rel.RelationshipName] = true;
        StateHasChanged();

        try
        {
            var parentKey = GetParentPrimaryKey();
            var data = await ChildDataFactory(rel, parentKey);
            _childGridData[rel.RelationshipName] = data;
        }
        finally
        {
            _childGridLoading[rel.RelationshipName] = false;
            StateHasChanged();
        }
    }

    private List<(string FieldName, string DisplayLabel)> GetMergedViewColumns(
        ChildRelationshipMetadata rel)
    {
        var columns = new List<(string FieldName, string DisplayLabel)>();
        if (MetadataProvider is null) return columns;

        // Far-side columns first (the "real" entity data)
        if (rel.FarSideColumns is not null && rel.FarSideEntityName is not null)
        {
            var farSideMeta = MetadataProvider.GetEntityMetadata(rel.FarSideEntityName);
            if (farSideMeta is not null)
            {
                foreach (var colName in rel.FarSideColumns)
                {
                    var field = farSideMeta.Fields.FirstOrDefault(f =>
                        string.Equals(f.FieldName, colName, StringComparison.OrdinalIgnoreCase));
                    if (field is not null && !field.IsForeignKey && !field.IsPrimaryKey)
                    {
                        columns.Add((field.FieldName, field.DisplayLabel));
                    }
                }
            }
        }

        // Junction columns (e.g., Quantity, Role)
        if (rel.JunctionColumns is not null)
        {
            var junctionMeta = MetadataProvider.GetEntityMetadata(rel.ChildEntityName);
            if (junctionMeta is not null)
            {
                foreach (var colName in rel.JunctionColumns)
                {
                    var field = junctionMeta.Fields.FirstOrDefault(f =>
                        string.Equals(f.FieldName, colName, StringComparison.OrdinalIgnoreCase));
                    if (field is not null && !field.IsForeignKey && !field.IsPrimaryKey)
                    {
                        columns.Add((field.FieldName, field.DisplayLabel));
                    }
                }
            }
        }

        return columns;
    }

    private List<(string FieldName, string DisplayLabel)> GetChildEntityColumns(
        ChildRelationshipMetadata rel)
    {
        var columns = new List<(string FieldName, string DisplayLabel)>();
        if (MetadataProvider is null) return columns;

        var childMeta = MetadataProvider.GetEntityMetadata(rel.ChildEntityName);
        if (childMeta is null) return columns;

        var parentedColumns = childMeta.ParentedColumns;
        if (parentedColumns is not null && parentedColumns.Count > 0)
        {
            foreach (var colName in parentedColumns)
            {
                var field = childMeta.Fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, colName, StringComparison.OrdinalIgnoreCase));
                if (field is not null)
                {
                    columns.Add((field.FieldName, field.DisplayLabel));
                }
            }
        }
        else
        {
            foreach (var field in childMeta.Fields
                .Where(f => !f.IsPrimaryKey && !f.IsForeignKey)
                .OrderBy(f => f.DisplayOrder))
            {
                columns.Add((field.FieldName, field.DisplayLabel));
            }
        }

        return columns;
    }

    private string? GetJunctionPrimaryKeyField(ChildRelationshipMetadata rel)
    {
        if (MetadataProvider is null) return null;
        var junctionMeta = MetadataProvider.GetEntityMetadata(rel.ChildEntityName);
        return junctionMeta?.Fields.FirstOrDefault(f => f.IsPrimaryKey)?.FieldName;
    }

    private List<FieldMetadata> GetJunctionEditableFields(ChildRelationshipMetadata rel)
    {
        if (MetadataProvider is null) return new();
        var junctionMeta = MetadataProvider.GetEntityMetadata(rel.ChildEntityName);
        if (junctionMeta is null) return new();

        return junctionMeta.Fields
            .Where(f => !f.IsPrimaryKey && !f.IsForeignKey
                && (rel.JunctionColumns is null
                    || rel.JunctionColumns.Contains(f.FieldName, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(f => f.DisplayOrder)
            .ToList();
    }

    // ── M:M Add dialog ──────────────────────────────────────────────

    private async Task HandleM2MAddClickAsync(ChildRelationshipMetadata rel)
    {
        if (FarSideLookupFactory is null || rel.FarSideEntityName is null) return;

        _m2mAddRelationship = rel;
        _m2mJunctionValues.Clear();
        _m2mSelectedFarSideRecord = null;
        _m2mFarSideItems = await FarSideLookupFactory(rel.FarSideEntityName);
        _showM2MAddDialog = true;
        StateHasChanged();
    }

    private async Task HandleM2MAddConfirmAsync()
    {
        if (_m2mAddRelationship is null || _m2mSelectedFarSideRecord is null) return;

        if (OnMergedViewAdd.HasDelegate)
        {
            await OnMergedViewAdd.InvokeAsync((
                _m2mAddRelationship,
                _m2mSelectedFarSideRecord.PrimaryKeyValue,
                new Dictionary<string, string>(_m2mJunctionValues)));
        }

        _showM2MAddDialog = false;

        // Reload the grid data
        await LoadChildGridDataAsync(_m2mAddRelationship);
    }

    private string GetM2MJunctionFieldValue(string fieldName)
    {
        return _m2mJunctionValues.TryGetValue(fieldName, out var v) ? v : string.Empty;
    }

    private void SetM2MJunctionFieldValue(string fieldName, string? value)
    {
        _m2mJunctionValues[fieldName] = value ?? string.Empty;
    }

    private void HandleM2MAddCancel()
    {
        _showM2MAddDialog = false;
        _m2mAddRelationship = null;
    }

    // ── M:M Delete ──────────────────────────────────────────────────

    private async Task HandleM2MDeleteClickAsync(ChildRelationshipMetadata rel)
    {
        if (!_selectedChildRowIndex.TryGetValue(rel.RelationshipName, out var index)) return;
        if (!_childGridData.TryGetValue(rel.RelationshipName, out var rows)
            || index >= rows.Count) return;

        var pkField = GetJunctionPrimaryKeyField(rel);
        if (pkField is null) return;

        var row = rows[index];
        if (!row.TryGetValue(pkField, out var pkValue) || pkValue is null) return;

        if (OnMergedViewDelete.HasDelegate)
        {
            await OnMergedViewDelete.InvokeAsync((rel, pkValue));
        }

        _selectedChildRowIndex.Remove(rel.RelationshipName);
        await LoadChildGridDataAsync(rel);
    }

    // ── 1:M Add/Delete handlers ─────────────────────────────────────

    private async Task HandleChildAddClickAsync(ChildRelationshipMetadata rel)
    {
        if (OnChildAddRequested.HasDelegate)
        {
            await OnChildAddRequested.InvokeAsync(rel);
        }
    }

    private async Task HandleChildDeleteClickAsync(ChildRelationshipMetadata rel)
    {
        if (!_selectedChildRowIndex.TryGetValue(rel.RelationshipName, out var index)) return;
        if (!_childGridData.TryGetValue(rel.RelationshipName, out var rows)
            || index >= rows.Count) return;

        var childMeta = MetadataProvider?.GetEntityMetadata(rel.ChildEntityName);
        var pkField = childMeta?.Fields.FirstOrDefault(f => f.IsPrimaryKey)?.FieldName;
        if (pkField is null) return;

        var row = rows[index];
        if (!row.TryGetValue(pkField, out var pkValue) || pkValue is null) return;

        if (OnChildDeleteRequested.HasDelegate)
        {
            await OnChildDeleteRequested.InvokeAsync((rel, pkValue));
        }

        _selectedChildRowIndex.Remove(rel.RelationshipName);
        await LoadChildGridDataAsync(rel);
    }

    // ── Child row selection ─────────────────────────────────────────

    private void HandleChildRowClick(string relationshipName, int index)
    {
        _selectedChildRowIndex[relationshipName] = index;
        StateHasChanged();
    }

    private bool IsChildRowSelected(string relationshipName, int index)
    {
        return _selectedChildRowIndex.TryGetValue(relationshipName, out var selected)
            && selected == index;
    }

    private static string FormatCellValue(object? value)
    {
        return value?.ToString() ?? "";
    }

    // ── Child row click → popup vs. tab ──────────────────────────────

    private async Task HandleChildRowClickAsync(ChildRelationshipMetadata relationship, object primaryKey)
    {
        var action = DetermineChildClickAction(relationship);

        if (action == ChildClickAction.OpenPopup)
        {
            if (OnChildRecordPopup.HasDelegate)
            {
                await OnChildRecordPopup.InvokeAsync((relationship.ChildEntityName, primaryKey, relationship));
            }
        }
        else
        {
            if (OnChildRecordSelected.HasDelegate)
            {
                await OnChildRecordSelected.InvokeAsync((relationship.ChildEntityName, primaryKey));
            }
        }
    }

    private ChildClickAction DetermineChildClickAction(ChildRelationshipMetadata relationship)
    {
        // Explicit override
        if (relationship.ClickAction.HasValue)
        {
            return relationship.ClickAction.Value;
        }

        // Default rules: check child entity metadata
        if (MetadataProvider is not null)
        {
            var childMeta = MetadataProvider.GetEntityMetadata(relationship.ChildEntityName);
            if (childMeta is not null
                && childMeta.IsLeafEntity
                && childMeta.Fields.Count <= childMeta.PopupFieldThreshold)
            {
                return ChildClickAction.OpenPopup;
            }
        }

        // Default: open in a tab
        return ChildClickAction.OpenTab;
    }

    // ── Field metadata helpers ───────────────────────────────────────

    private List<FieldMetadata> GetDetailFieldMetadata()
    {
        if (EntityMetadata?.Fields is null || EntityMetadata.DetailFormFields is null)
            return new();

        var fieldLookup = EntityMetadata.Fields.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);
        var result = new List<FieldMetadata>();

        foreach (var fieldName in EntityMetadata.DetailFormFields)
        {
            if (fieldLookup.TryGetValue(fieldName, out var field))
            {
                result.Add(field);
            }
        }

        return result;
    }

    private IReadOnlyList<ChildRelationshipMetadata> GetVisibleChildRelationships()
    {
        if (EntityMetadata?.ChildRelationships is null)
            return [];

        return EntityMetadata.ChildRelationships
            .Where(r => r.ShowAsTab)
            .OrderBy(r => r.DisplayOrder)
            .ToList();
    }

    private object? GetFieldValue(string fieldName)
    {
        if (Record is null) return null;
        return PropertyAccessor.GetValue(Record, fieldName);
    }

    private string GetFieldStringValue(string fieldName)
    {
        var value = GetFieldValue(fieldName);
        return value?.ToString() ?? "";
    }

    // ── Editor rendering helpers ─────────────────────────────────────

    private string ResolveEditorType(FieldMetadata field)
    {
        // Explicit override
        if (!string.IsNullOrEmpty(field.EditorType))
        {
            return field.EditorType;
        }

        // FK fields → combobox
        if (field.IsForeignKey)
        {
            return "combobox";
        }

        // Infer from DataType
        return field.DataType.ToLowerInvariant() switch
        {
            "string" => "text",
            "int" or "int32" or "int64" or "long" => "numeric",
            "decimal" or "double" or "float" or "single" => "numeric",
            "datetime" or "datetimeoffset" => "date",
            "bool" or "boolean" => "checkbox",
            _ => "text"
        };
    }

    private bool IsFieldReadOnly(FieldMetadata field)
    {
        return IsReadOnly || field.IsReadOnly || field.IsPrimaryKey;
    }

    // ── Validation helpers ─────────────────────────────────────────────

    private string? GetFieldValidationError(string fieldName)
    {
        if (Record is null) return null;

        var detailFields = GetDetailFieldMetadata();
        var field = detailFields.FirstOrDefault(f =>
            string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

        if (field is null || !field.IsRequired) return null;

        var value = PropertyAccessor.GetValue(Record, fieldName);
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
        {
            return $"{field.DisplayLabel} is required";
        }

        return null;
    }

    // ── CSS helpers ──────────────────────────────────────────────────

    private string GetFormCssClass()
    {
        var css = "dabbler-detail-form";
        if (!string.IsNullOrEmpty(Class))
            css += " " + Class;
        return css;
    }
}
