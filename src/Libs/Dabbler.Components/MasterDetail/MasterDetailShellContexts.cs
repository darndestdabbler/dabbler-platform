namespace Dabbler.Components.MasterDetail;

/// <summary>Controls how the target entity's fields are rendered.</summary>
public enum TargetEditability
{
    /// <summary>All target fields are editable (default).</summary>
    Editable,
    /// <summary>Target fields are permanently read-only; no toggle affordance rendered.</summary>
    ReadOnly,
    /// <summary>Target fields start read-only; an "Edit record" button in the details header unlocks them.</summary>
    ReadOnlyWithToggle,
}

/// <summary>
/// Context passed to <see cref="MasterDetailExpansionShell{TRow,TEntity,TLink}.DetailsContent"/>
/// for target-entity field bindings and dirty-state management.
/// </summary>
/// <typeparam name="TEntity">The entity type being edited.</typeparam>
public sealed class DetailsContext<TEntity>
{
    /// <summary>Whether the target entity fields are currently read-only.</summary>
    public bool IsTargetReadOnly { get; internal set; }

    /// <summary>Whether any target field has been changed since the last save.</summary>
    public bool IsDirty { get; internal set; }

    /// <summary>
    /// Toggles target editing (for <see cref="TargetEditability.ReadOnlyWithToggle"/> mode).
    /// Call from the consumer's "Edit record" button if overriding the default toggle button.
    /// </summary>
    public Action ToggleTargetEditing { get; init; } = () => { };

    /// <summary>
    /// Marks the target entity as dirty. Call from MudForm's <c>FieldChanged</c> or
    /// individual field <c>ValueChanged</c> events.
    /// </summary>
    public Action SetDirty { get; init; } = () => { };

    internal void SetDirtyInternal() => IsDirty = true;
    internal void ResetDirty() => IsDirty = false;
}

/// <summary>
/// Context passed to <see cref="MasterDetailExpansionShell{TRow,TEntity,TLink}.LinkDataContent"/>
/// for link-payload field bindings and dirty-state management.
/// </summary>
/// <typeparam name="TLink">The link/association-data type.</typeparam>
public sealed class LinkContext<TLink>
{
    /// <summary>Whether any link-data field has been changed since the last save.</summary>
    public bool IsLinkDirty { get; internal set; }

    /// <summary>
    /// Marks the link data as dirty. Call from field <c>ValueChanged</c> events.
    /// </summary>
    public Action SetLinkDirty { get; init; } = () => { };

    internal void SetLinkDirtyInternal() => IsLinkDirty = true;
    internal void ResetLinkDirty() => IsLinkDirty = false;
}

/// <summary>
/// Context passed to <see cref="MasterDetailExpansionShell{TRow,TEntity,TLink}.DetailsActions"/>
/// for rendering action buttons with full knowledge of both dirty states.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
/// <typeparam name="TLink">The link/association-data type.</typeparam>
public sealed class DetailsActionsContext<TEntity, TLink>
{
    /// <summary>Whether the entity form has unsaved changes.</summary>
    public bool EntityDirty { get; init; }

    /// <summary>Whether the link-data form has unsaved changes.</summary>
    public bool LinkDirty { get; init; }

    /// <summary>True when either form is dirty.</summary>
    public bool AnyDirty => EntityDirty || LinkDirty;
}

/// <summary>
/// Carry type for <c>OnSaveRequested</c> / <c>OnSaveAndUseRequested</c>.
/// Bundles both entity and link buffers with their dirty flags so the save
/// handler can decide which EF writes to issue.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
/// <typeparam name="TLink">The link/association-data type.</typeparam>
public sealed class SaveRequest<TEntity, TLink>
{
    /// <summary>The target entity buffer (populated by consumer's form binding).</summary>
    public TEntity? Entity { get; init; }

    /// <summary>The link-data buffer (populated by consumer's link form binding).</summary>
    public TLink? Link { get; init; }

    /// <summary>Whether the entity form was dirty at save time.</summary>
    public bool EntityDirty { get; init; }

    /// <summary>Whether the link-data form was dirty at save time.</summary>
    public bool LinkDirty { get; init; }
}

/// <summary>
/// Result returned via <c>OnSaveAndUseRequested</c>. Carry type for both
/// buffers and their dirty flags; the shell passes this to the dialog close
/// handler or the parent form's FK update logic.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
/// <typeparam name="TLink">The link/association-data type.</typeparam>
public sealed class SaveAndUseResult<TEntity, TLink>
{
    /// <summary>The entity that was saved (or linked).</summary>
    public TEntity? Entity { get; init; }

    /// <summary>The link-data that was saved.</summary>
    public TLink? Link { get; init; }

    /// <summary>Whether the entity was dirty at save time.</summary>
    public bool EntityDirty { get; init; }

    /// <summary>Whether the link-data was dirty at save time.</summary>
    public bool LinkDirty { get; init; }
}
