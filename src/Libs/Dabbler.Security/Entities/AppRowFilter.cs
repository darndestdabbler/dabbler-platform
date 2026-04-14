namespace Dabbler.Security.Entities;

/// <summary>
/// Defines a row-level data scoping expression for a role on a given entity.
/// This allows for fine-grained access control where specific rows of data are visible or modifiable
/// based on the user's assigned roles and dynamic conditions.
/// The expression is compiled by <c>IRowFilterService</c> into an <c>Expression&lt;Func&lt;T, bool&gt;&gt;</c>.
/// </summary>
public class AppRowFilter
{
    /// <summary>
    /// The unique identifier for this row filter definition.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ID of the role to which this filter applies.
    /// </summary>
    public int RoleId { get; set; }
    /// <summary>
    /// The navigation property for the associated <c>AppRole</c>.
    /// </summary>
    public AppRole Role { get; set; } = null!;

    /// <summary>
    /// The entity key as registered via options.RegisterEntities(). This string identifies
    /// the data entity (e.g., "Project", "Document") to which the filter applies.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// The C#-like expression string that defines the row-level filter logic.
    /// E.g. "OwnerId == @CurrentUserId" or "Status == 'Active'".
    /// This expression is dynamically parsed and compiled.
    /// <para>
    /// **Security Note:** This property contains executable code. It must be carefully
    /// controlled and validated to prevent injection vulnerabilities or unauthorized data access.
    /// Only trusted sources should be allowed to define or modify these expressions.
    /// </para>
    /// </summary>
    public string FilterExpression { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of what this filter accomplishes.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
