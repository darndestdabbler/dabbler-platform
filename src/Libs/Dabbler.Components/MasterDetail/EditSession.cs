using System.Reflection;
using System.Text.Json;

namespace Dabbler.Components.MasterDetail;

/// <summary>
/// Tracks edit state for a single entity instance: captures a snapshot when
/// the session opens, exposes a live reference the form binds to, and reports
/// dirty state by comparing the current Live value to the snapshot via JSON
/// serialization.
///
/// Pages call <see cref="NotifyChanged"/> from their MudBlazor field
/// <c>ValueChanged</c> handlers (never <c>@bind-Value</c>) to recompute
/// <see cref="IsDirty"/>. <see cref="Undo"/> restores Live to the snapshot
/// by reflecting over <strong>public, non-indexed, read/write properties</strong>
/// — fields, get-only properties, and properties on base types not exposed
/// through <c>typeof(T)</c> are not touched.
/// <see cref="Commit"/> freezes the current Live state as the new baseline
/// (e.g., after a successful save). <see cref="CommitFrom(T)"/> overwrites
/// Live with a server-returned instance and then freezes (e.g., when the
/// API returns a refreshed record with server-set timestamps).
/// </summary>
/// <typeparam name="T">
/// The DTO type being edited. Intended for flat data-transfer records
/// composed of public read/write scalar / enum / string / nullable
/// properties. Get-only collection properties, computed properties, and
/// fields are NOT round-tripped by <see cref="Undo"/> and
/// <see cref="CommitFrom(T)"/>; if a property's value is mutated through
/// such a member, dirty detection may diverge from the user's
/// expectations. Use a dedicated copy-and-compare strategy for DTOs that
/// don't fit this shape.
/// </typeparam>
public sealed class EditSession<T> where T : class
{
    private readonly JsonSerializerOptions? _options;
    private string _snapshotJson;
    private bool _isDirty;

    /// <summary>
    /// Initializes a new session bound to <paramref name="live"/>. The live
    /// instance is captured by reference; the form mutates it directly.
    /// </summary>
    /// <param name="live">The entity instance the form binds to. Must be
    /// non-null and JSON-roundtrippable.</param>
    /// <param name="options">Optional JSON options; defaults to a writer
    /// suitable for DTO comparison.</param>
    public EditSession(T live, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(live);
        Live = live;
        _options = options;
        _snapshotJson = JsonSerializer.Serialize(live, options);
    }

    /// <summary>
    /// The live entity instance. The form binds to its properties directly.
    /// Never reassigned across the session's lifetime — use
    /// <see cref="CommitFrom(T)"/> to overwrite fields from a server result.
    /// </summary>
    public T Live { get; }

    /// <summary>
    /// True if <see cref="Live"/> differs from the snapshot. Updated by
    /// <see cref="NotifyChanged"/>; cached between calls to avoid per-render
    /// JSON serialization.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Recomputes <see cref="IsDirty"/> by comparing the current
    /// <see cref="Live"/> JSON against the snapshot. Call from each form
    /// field's <c>ValueChanged</c> handler.
    /// </summary>
    public void NotifyChanged()
    {
        var current = JsonSerializer.Serialize(Live, _options);
        _isDirty = current != _snapshotJson;
    }

    /// <summary>
    /// Reverts <see cref="Live"/> to the snapshot by copying public
    /// read/write property values back. After return <see cref="IsDirty"/>
    /// is false.
    /// </summary>
    public void Undo()
    {
        var snapshot = JsonSerializer.Deserialize<T>(_snapshotJson, _options);
        if (snapshot is null)
        {
            return;
        }

        OverwriteFrom(Live, snapshot);
        _isDirty = false;
    }

    /// <summary>
    /// Freezes the current <see cref="Live"/> state as the new baseline.
    /// After return <see cref="IsDirty"/> is false.
    /// </summary>
    public void Commit()
    {
        _snapshotJson = JsonSerializer.Serialize(Live, _options);
        _isDirty = false;
    }

    /// <summary>
    /// Overwrites <see cref="Live"/>'s fields from <paramref name="fresh"/>
    /// (typically a server-returned instance with refreshed timestamps),
    /// then freezes as the new baseline.
    /// </summary>
    /// <param name="fresh">The server-returned instance to overlay onto
    /// <see cref="Live"/>.</param>
    public void CommitFrom(T fresh)
    {
        ArgumentNullException.ThrowIfNull(fresh);
        OverwriteFrom(Live, fresh);
        Commit();
    }

    /// <summary>
    /// Copies public read/write property values from <paramref name="source"/>
    /// into <paramref name="target"/>. Intended for DTOs; do not pass EF
    /// entity instances with tracked nav properties.
    /// </summary>
    private static void OverwriteFrom(T target, T source)
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            {
                property.SetValue(target, property.GetValue(source));
            }
        }
    }
}
