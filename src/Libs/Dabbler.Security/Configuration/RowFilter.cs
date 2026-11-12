namespace Dabbler.Security.Configuration;

/// <summary>
/// Defines a row-level security filter for data entities based on user roles.
/// </summary>
/// <remarks>
/// Legacy grant-based type. Replaced by per-role <see cref="RowRestriction"/>
/// plus the <c>IRowRestrictionBroker</c> runtime.
/// </remarks>
[Obsolete("Replaced by RowRestriction on each user-type RoleDefinition.")]
public sealed class RowFilter
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity to which this filter applies (e.g., "Order", "Product").
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the role for which this filter applies.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Filter expression, e.g. "OwnerId == @CurrentUserId" or "Status == 'Active'".
    /// Evaluated by IRowFilterService at query time.
    /// </summary>
    /// <remarks>
    /// **Security Note**: Ensure these expressions come from trusted sources or are validated
    /// to prevent injection vulnerabilities if they are user-defined.
    /// </remarks>
    public string FilterExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the filter's purpose or logic.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
