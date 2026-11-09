namespace Dabbler.Security.Entities;

/// <summary>
/// Stores multi-factor authentication (MFA) configuration and state for a specific user.
/// </summary>
/// <remarks>
/// This entity is exceptionally sensitive as it contains the secrets required for a user's MFA.
/// All sensitive fields, especially <see cref="SecretEncrypted"/> and <see cref="RecoveryCodes"/>,
/// **must** be encrypted at rest using a strong, well-managed encryption key.
/// Unauthorized access to this data would severely compromise user accounts.
/// </remarks>
public class AppUserMfa
{
    /// <summary>
    /// The foreign key identifying the associated application user.
    /// This links the MFA configuration to a specific <see cref="AppUser"/>.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// A string identifying the type of MFA configured (e.g., "TOTP" for Time-based One-Time Password, "SMS", "FIDO2").
    /// </summary>
    public string MfaType { get; set; } = "TOTP";

    /// <summary>
    /// The encrypted secret key or configuration data required for the MFA method (e.g., TOTP shared secret).
    /// <para>Security Note: This field is highly sensitive and **must** be encrypted at rest.
    /// Never store raw MFA secrets. The encryption key must be securely managed.</para>
    /// </summary>
    public byte[] SecretEncrypted { get; set; } = [];

    /// <summary>
    /// The Initialization Vector (IV) used for encrypting <see cref="SecretEncrypted"/>.
    /// <para>Security Note: This IV must be unique for each encryption operation to ensure semantic security.</para>
    /// </summary>
    public byte[] SecretIv { get; set; } = [];

    /// <summary>
    /// A boolean indicating whether the MFA setup has been successfully verified by the user.
    /// MFA should only be considered active and enforced after successful verification.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// An optional timestamp indicating when MFA was successfully enabled for the user.
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// An optional timestamp indicating the last successful MFA validation attempt for the user.
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>
    /// The number of consecutive failed MFA validation attempts.
    /// This count is used to trigger lockout policies and prevent brute-force attacks on MFA codes.
    /// </summary>
    public int FailedAttemptCount { get; set; }

    /// <summary>
    /// An optional timestamp indicating until when MFA attempts are locked out due to excessive failed attempts.
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Encrypted string containing a set of one-time recovery codes for MFA bypass in case
    /// the primary MFA method is lost or inaccessible.
    /// <para>Security Note: This field is highly sensitive and **must** be encrypted at rest.
    /// Recovery codes provide a backdoor to an account; their handling must be extremely secure.</para>
    /// </summary>
    public string? RecoveryCodes { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="AppUser"/> entity.
    /// </summary>
    public AppUser? User { get; set; }
}
