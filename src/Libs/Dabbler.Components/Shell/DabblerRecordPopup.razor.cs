// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A popup dialog wrapper around <see cref="DabblerDetailForm{T}"/> for editing
/// leaf entity records or junction entity records in a modal dialog.
/// Closing with unsaved changes prompts the user to save.
/// </summary>
public partial class DabblerRecordPopup<T> : DabblerComponentBase where T : class, new()
{
    private DabblerDetailForm<T>? _detailForm;
    private bool _isVisible;
    private bool _isDirty;
    private bool _isSaving;
    private bool _showDirtyCloseDialog;

    // ── Parameters ───────────────────────────────────────────────────

    /// <summary>
    /// Metadata describing the entity's fields and display rules.
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
    /// Makes all fields read-only.
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Raised when the user clicks Save.
    /// </summary>
    [Parameter]
    public EventCallback<T> OnSave { get; set; }

    /// <summary>
    /// Raised when the dialog is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Provides entity metadata for child entities.
    /// </summary>
    [Parameter]
    public IEntityMetadataProvider? MetadataProvider { get; set; }

    // ── Public members ───────────────────────────────────────────────

    /// <summary>
    /// Opens the popup dialog.
    /// </summary>
    public void Show()
    {
        _isVisible = true;
        _isDirty = false;
        StateHasChanged();
    }

    /// <summary>
    /// Closes the popup dialog. If dirty, prompts the user first.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_isDirty)
        {
            _showDirtyCloseDialog = true;
            StateHasChanged();
            return;
        }

        await ExecuteCloseAsync();
    }

    // ── Internal handlers ────────────────────────────────────────────

    private void HandleDirtyChanged(bool isDirty)
    {
        _isDirty = isDirty;
    }

    private async Task HandleSaveAsync(T record)
    {
        _isSaving = true;
        StateHasChanged();

        try
        {
            if (OnSave.HasDelegate)
            {
                await OnSave.InvokeAsync(record);
            }

            _isDirty = false;
            await ExecuteCloseAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task HandleSaveClickAsync()
    {
        if (_detailForm is not null)
        {
            await _detailForm.SaveAsync();
        }
    }

    private async Task HandleCancelClickAsync()
    {
        await CloseAsync();
    }

    private async Task ExecuteCloseAsync()
    {
        _isVisible = false;
        _isDirty = false;
        StateHasChanged();

        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    private void HandleDirtyCloseCancelAsync()
    {
        _showDirtyCloseDialog = false;
    }

    private async Task HandleDirtyCloseConfirmAsync()
    {
        _showDirtyCloseDialog = false;
        _isDirty = false;
        await ExecuteCloseAsync();
    }
}
