namespace Dabbler.Security.Services;

/// <summary>
/// Thrown by <see cref="IBackofficeUserCreationService"/> when the requested email
/// address already belongs to an existing <see cref="Entities.AppUser"/>. The API
/// controller maps this to an HTTP 409 Conflict response.
/// </summary>
public sealed class EmailAlreadyExistsException(string email)
    : Exception($"A user with email '{email}' already exists.")
{
    public string Email { get; } = email;
}
