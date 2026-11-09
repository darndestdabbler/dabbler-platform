namespace Dabbler.Security.Configuration;

/// <summary>
/// Restricts a user-type role to only see records on the named entity that match
/// the given filter expression. The filter is applied automatically to all relevant
/// grids and data-bound pages via <c>IRowRestrictionBroker</c> (wired in Session 4
/// of the Foundations set).
/// </summary>
/// <remarks>
/// <para>
/// Replaces the legacy <c>RowFilter</c> shape. The expression grammar is the same
/// one honoured by the prior <c>RowFilterService</c> — simple equality of a column
/// against a string literal, integer literal, or the <c>@CurrentUserId</c> token —
/// and it will be ported into a new broker service in the Session 4 step.
/// </para>
/// <para>
/// User-type roles only. Baked-in roles have compiled-in row-scoping semantics
/// (SystemAdmin and CrossOrgMonitor see everything; UserAdmin and User are
/// org-scoped via the global EF query filter delivered in Session Set C).
/// </para>
/// </remarks>
public sealed class RowRestriction
{
    /// <summary>
    /// The entity (table) whose row visibility is restricted for this role.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// The filter expression the role's queries are narrowed to. Example:
    /// <c>CustomerId == @CurrentUserId</c>.
    /// </summary>
    /// <remarks>
    /// <b>Security note:</b> Only SystemAdmin can configure
    /// <see cref="RowRestriction"/> values — they are never user-supplied at
    /// runtime — so the expression is trusted. The runtime broker still validates
    /// each expression at parse time and falls back to a deny-all predicate if
    /// parsing fails, matching the prior <c>RowFilterService</c> behaviour.
    /// </remarks>
    public string FilterExpression { get; set; } = string.Empty;
}
