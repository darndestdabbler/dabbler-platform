using System.Linq.Expressions;
using System.Reflection;

namespace Dabbler.Components.MasterDetail;

/// <summary>
/// Declarative configuration for a filter panel bound to <typeparamref name="TEntity"/>.
/// </summary>
public sealed record FilterConfig<TEntity>(
    FilterMode Mode,
    IReadOnlyList<FilterProperty<TEntity>> Properties);

/// <summary>Determines how the filter panel handles state persistence and data loading.</summary>
public enum FilterMode
{
    /// <summary>Filter state is persisted to the URL query string on change; reload restores it.</summary>
    ServerSide,

    /// <summary>Filter state is transient (not persisted to the URL).</summary>
    ClientSide
}

/// <summary>
/// Discriminated union describing a single filterable property on <typeparamref name="TEntity"/>.
/// Consumers choose the concrete subtype to control rendering.
/// </summary>
public abstract record FilterProperty<TEntity>
{
    /// <summary>Expression that selects the property to filter on. Used to extract the property name and type at runtime.</summary>
    public required Expression<Func<TEntity, object?>> Property { get; init; }

    /// <summary>Display label shown above or beside the control. Falls back to the property name when null.</summary>
    public string? Label { get; init; }

    /// <summary>Free-text filter rendered as a <c>MudTextField</c>.</summary>
    public sealed record Text : FilterProperty<TEntity>;

    /// <summary>
    /// Numeric filter rendered as a <c>MudNumericField&lt;decimal?&gt;</c>.
    /// When <see cref="AsRange"/> is <c>true</c>, two fields (min/max) are rendered.
    /// </summary>
    public sealed record Numeric : FilterProperty<TEntity>
    {
        /// <summary>Render a min + max pair instead of a single value field.</summary>
        public bool AsRange { get; init; }

        /// <summary>Placeholder for the single-value or min field. Defaults to <c>null</c>.</summary>
        public string? Placeholder { get; init; }

        /// <summary>Placeholder for the min field when <see cref="AsRange"/> is <c>true</c>.</summary>
        public string? MinPlaceholder { get; init; }

        /// <summary>Placeholder for the max field when <see cref="AsRange"/> is <c>true</c>.</summary>
        public string? MaxPlaceholder { get; init; }
    }

    /// <summary>Date filter rendered as a <c>MudDatePicker</c>; optionally a start/end pair.</summary>
    public sealed record Date : FilterProperty<TEntity>
    {
        /// <summary>Render a start + end date pair instead of a single date picker.</summary>
        public bool AsRange { get; init; }
    }

    /// <summary>
    /// Boolean tri-state filter rendered as a <c>MudCheckBox&lt;bool?&gt;</c> with
    /// <c>TriState="true"</c> (unset / true / false).
    /// </summary>
    public sealed record Bool : FilterProperty<TEntity>;

    /// <summary>
    /// Enum filter rendered as a <c>MudSelect&lt;object&gt;</c> populated from
    /// <see cref="Enum.GetValues(Type)"/> of the property's declared type.
    /// </summary>
    public sealed record EnumVal : FilterProperty<TEntity>;

    /// <summary>
    /// Foreign-key filter rendered as a <c>MudSelect</c> populated from a
    /// consumer-supplied <see cref="LookupSource{TForeign}"/>.
    /// </summary>
    public sealed record ForeignKey<TForeign> : FilterProperty<TEntity>
    {
        /// <summary>Data source for the lookup list.</summary>
        public required LookupSource<TForeign> Lookup { get; init; }

        /// <summary>Expression selecting the display member on <typeparamref name="TForeign"/>.</summary>
        public required Expression<Func<TForeign, object>> DisplayProperty { get; init; }

        internal override bool IsForeignKey => true;

        internal override async Task<IReadOnlyList<(object Value, string Display)>?> LoadLookupItemsAsync(
            CancellationToken ct = default)
        {
            IReadOnlyList<TForeign> items = Lookup switch
            {
                LookupSource<TForeign>.Static s => s.Items,
                LookupSource<TForeign>.AsyncLoader a => await a.Load(ct),
                _ => []
            };

            var getter = DisplayProperty.Compile();
            return items
                .Select(item => ((object)item!, getter(item)?.ToString() ?? string.Empty))
                .ToList();
        }
    }

    // ── Internal helpers ────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when this property is a <see cref="ForeignKey{TForeign}"/> instance.
    /// Used by the component to branch rendering without requiring a generic type check.
    /// </summary>
    internal virtual bool IsForeignKey => false;

    /// <summary>
    /// Optionally loads a list of (value, display) pairs for lookup-based filter types.
    /// Returns <c>null</c> for non-lookup property types.
    /// </summary>
    internal virtual Task<IReadOnlyList<(object Value, string Display)>?> LoadLookupItemsAsync(
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(object Value, string Display)>?>(null);

    /// <summary>Extracts the property name from the <see cref="Property"/> expression.</summary>
    internal static string GetPropertyName(Expression<Func<TEntity, object?>> expr)
    {
        if (expr.Body is MemberExpression member)
            return member.Member.Name;

        if (expr.Body is UnaryExpression { Operand: MemberExpression boxedMember })
            return boxedMember.Member.Name;

        throw new ArgumentException($"Cannot extract property name from expression: {expr}");
    }

    /// <summary>Extracts the CLR type of the property from the <see cref="Property"/> expression.</summary>
    internal static Type GetPropertyType(Expression<Func<TEntity, object?>> expr)
    {
        if (expr.Body is MemberExpression member)
            return ((PropertyInfo)member.Member).PropertyType;

        if (expr.Body is UnaryExpression { Operand: MemberExpression boxedMember })
            return ((PropertyInfo)boxedMember.Member).PropertyType;

        throw new ArgumentException($"Cannot extract property type from expression: {expr}");
    }
}
