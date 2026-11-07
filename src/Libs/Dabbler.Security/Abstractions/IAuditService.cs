using Dabbler.Security.Models;

namespace Dabbler.Security.Abstractions;

/// <summary>
/// Defines an interface for services responsible for retrieving audit trail entries related to security events.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Retrieves a list of audit entries related to a specific user's activities.
    /// </summary>
    /// <param name="userId">The ID of the user whose history is to be retrieved.</param>
    /// <param name="from">Optional. The start date and time for the history retrieval. If null, retrieves from the beginning of time.</param>
    /// <param name="to">Optional. The end date and time for the history retrieval. If null, retrieves up to the current time.</param>
    /// <returns>A task representing the asynchronous operation, yielding a list of <see cref="AuditEntry"/> objects.</returns>
    Task<List<AuditEntry>> GetUserHistoryAsync(int userId, DateTime? from, DateTime? to);

    /// <summary>
    /// Retrieves a list of audit entries related to role assignments.
    /// </summary>
    /// <param name="userId">Optional. The ID of the user whose role assignment history is to be retrieved. If null, retrieves for all users.</param>
    /// <param name="roleId">Optional. The ID of the role whose assignment history is to be retrieved. If null, retrieves for all roles.</param>
    /// <param name="from">Optional. The start date and time for the history retrieval. If null, retrieves from the beginning of time.</param>
    /// <param name="to">Optional. The end date and time for the history retrieval. If null, retrieves up to the current time.</param>
    /// <returns>A task representing the asynchronous operation, yielding a list of <see cref="AuditEntry"/> objects.</returns>
    Task<List<AuditEntry>> GetRoleAssignmentHistoryAsync(int? userId, int? roleId, DateTime? from, DateTime? to);

    /// <summary>
    /// Retrieves a paginated list of all audit entries within a specified time range.
    /// </summary>
    /// <param name="from">The start date and time for the audit entries.</param>
    /// <param name="to">The end date and time for the audit entries.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The maximum number of entries per page.</param>
    /// <returns>A task representing the asynchronous operation, yielding a list of <see cref="AuditEntry"/> objects for the specified page.</returns>
    Task<List<AuditEntry>> GetAllChangesAsync(DateTime from, DateTime to, int page, int pageSize);
}
