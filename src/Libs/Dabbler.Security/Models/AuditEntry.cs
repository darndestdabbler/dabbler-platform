namespace Dabbler.Security.Models;

/// <summary>
/// Represents a single immutable entry in the application's audit log.
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// The UTC date and time when the audit event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The type or name of the entity affected by the operation (e.g., "User", "Role", "Product").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the specific entity instance affected.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the user or system responsible for the change, if applicable.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// A description of the action performed (e.g., "Create", "Update", "Delete", "Login", "FailedLogin").
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Additional, free-form details about the event, such as changes made to an entity's properties.
    /// </summary>
    public string? Details { get; set; }
}
