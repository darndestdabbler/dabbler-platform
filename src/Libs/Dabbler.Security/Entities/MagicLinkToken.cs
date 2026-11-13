namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a single-use token used for passwordless authentication or email verification.
/// </summary>
/// <remarks>
/// This entity stores highly sensitive information related to user authentication.
/// The <see cref="TokenHash"/> and <see cref="CodeHash"/> fields must contain cryptographic hashes of
/// the actual tokens/codes, never the raw values. Proper lifecycle management, including strict
/// expiration (<see cref="ExpiresAt"/>) and single-use enforcement (<see cref="UsedAt"/>),
/// is critical to prevent security vulnerabilities like replay attacks.
/// </remarks>
public class MagicLinkToken
{
    /// <summary>
    /// The unique identifier for this magic link token record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The email address associated with this token, identifying the user who initiated the magic link request.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// A cryptographic hash of the secure, long-lived part of the magic link token.
    /// This value is used to verify the token without storing the raw token itself.
    /// <para>Security Note: Must be stored as a strong, salted hash (e.g., Argon2, bcrypt)
    /// to prevent brute-force attacks against the hash.</para>
    /// </summary>
    public byte[] TokenHash { get; set; } = [];

    /// <summary>
    /// A cryptographic hash of a short, user-entered code component, often used for secondary verification.
    /// <para>Security Note: Similar to <see cref="TokenHash"/>, this must be stored as a strong, salted hash.</para>
    /// </summary>
    public byte[] CodeHash { get; set; } = [];

    /// <summary>
    /// The timestamp indicating when this magic link token becomes invalid.
    /// Tokens should have a short lifespan to minimize exposure.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// An optional timestamp indicating when this token was successfully used.
    /// A non-null value signifies that the token has been consumed and cannot be used again,
    /// preventing replay attacks.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// The number of consecutive failed attempts to use this token or its associated code.
    /// Can be used to implement lockout policies against brute-force attacks.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// The timestamp when this magic link token was generated.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The IP address from which the magic link token was requested.
    /// Useful for security auditing, anomaly detection, and fraud prevention.
    /// Consider privacy implications when storing IP addresses.
    /// </summary>
    public string? IpAddress { get; set; }
}
