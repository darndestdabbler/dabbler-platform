using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Dabbler.Security.Services;

/// <summary>
/// Implements <see cref="IMfaService"/> to provide Time-based One-Time Password (TOTP)
/// multi-factor authentication functionality.
/// </summary>
public class MfaService : IMfaService
{
    private readonly DabblerSecurityDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<MfaService> _logger;
    private readonly int _lockoutThreshold;
    private readonly TimeSpan _lockoutDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaService"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="encryption">The service for encrypting and decrypting secrets.</param>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="lockoutThreshold">The number of failed attempts before an account is locked.</param>
    /// <param name="lockoutDuration">The duration for which an account is locked after exceeding the threshold.</param>
    public MfaService(
        DabblerSecurityDbContext db,
        IEncryptionService encryption,
        ILogger<MfaService> logger,
        int lockoutThreshold = 5,
        TimeSpan? lockoutDuration = null)
    {
        _db = db;
        _encryption = encryption;
        _logger = logger;
        _lockoutThreshold = lockoutThreshold;
        _lockoutDuration = lockoutDuration ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Initiates the MFA enrollment process for a user.
    /// Generates a new TOTP secret and a corresponding QR code URI for authenticator apps.
    /// </summary>
    /// <param name="userId">The ID of the user enrolling in MFA.</param>
    /// <param name="appName">The name of the application to be displayed in the authenticator app.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a tuple with the Base32 encoded secret and the QR code URI.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the user is not found, or if MFA is already enrolled and verified for the user.
    /// </exception>
    public async Task<(string Secret, string QrCodeUri)> InitiateEnrollmentAsync(int userId, string appName)
    {
        // Generate 160-bit TOTP secret
        var secretKey = KeyGeneration.GenerateRandomKey(20); // 160 bits = 20 bytes
        var secretBase32 = Base32Encoding.ToString(secretKey);

        // Get user email for the URI
        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        // Create otpauth:// URI
        var uri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email)}?secret={secretBase32}&issuer={Uri.EscapeDataString(appName)}";

        // Encrypt and store secret (not yet verified)
        var secretBytes = Base32Encoding.ToBytes(secretBase32);
        var encrypted = _encryption.Encrypt(secretBytes, out var iv);

        var mfa = new AppUserMfa
        {
            UserId = userId,
            MfaType = "TOTP",
            SecretEncrypted = encrypted,
            SecretIv = iv,
            IsVerified = false,
            FailedAttemptCount = 0
        };

        // Remove any existing unverified enrollment; refuse to overwrite verified MFA
        var existing = await _db.AppUserMfas.FindAsync(userId);
        if (existing != null)
        {
            if (existing.IsVerified)
                throw new InvalidOperationException("Cannot re-enroll MFA without resetting existing enrollment first.");
            _db.AppUserMfas.Remove(existing);
        }

        _db.AppUserMfas.Add(mfa);
        await _db.SaveChangesAsync();

        _logger.LogInformation("MFA enrollment initiated for user {UserId}", userId);

        return (secretBase32, uri);
    }

    /// <summary>
    /// Completes the MFA enrollment process by verifying an initial TOTP code.
    /// If successful, it marks MFA as verified and generates recovery codes.
    /// </summary>
    /// <param name="userId">The ID of the user completing enrollment.</param>
    /// <param name="verificationCode">The TOTP code from the authenticator app.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is <c>true</c> if the code is valid and enrollment is completed; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> CompleteEnrollmentAsync(int userId, string verificationCode)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        if (mfa == null || mfa.IsVerified) return false;

        // Decrypt secret
        var secretBytes = _encryption.Decrypt(mfa.SecretEncrypted, mfa.SecretIv);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        // Validate code
        var totp = new Totp(secretBytes);
        if (!totp.VerifyTotp(verificationCode, out long timeStepMatched))
        {
            _logger.LogWarning("MFA enrollment verification failed for user {UserId}", userId);
            return false;
        }

        // Mark as verified
        mfa.IsVerified = true;
        mfa.EnabledAt = DateTime.UtcNow;
        mfa.LastValidatedAt = DateTime.UtcNow;

        // Generate recovery codes
        var recoveryCodes = GenerateRecoveryCodeHashes(10);
        mfa.RecoveryCodes = JsonSerializer.Serialize(recoveryCodes);

        await _db.SaveChangesAsync();

        _logger.LogInformation("MFA enrollment completed for user {UserId}", userId);

        return true;
    }

    /// <summary>
    /// Validates a TOTP code provided by a user during sign-in.
    /// This method includes protection against account lockout and replay attacks.
    /// </summary>
    /// <param name="userId">The ID of the user being validated.</param>
    /// <param name="code">The TOTP code to validate.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is <c>true</c> if the code is valid; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> ValidateTotpAsync(int userId, string code)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        
        // Check if MFA is enrolled and verified
        if (mfa == null || !mfa.IsVerified) return false;

        // Check lockout
        if (mfa.LockedUntil.HasValue && mfa.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("MFA validation blocked - account locked for user {UserId}", userId);
            return false;
        }

        // Decrypt secret
        var secretBytes = _encryption.Decrypt(mfa.SecretEncrypted, mfa.SecretIv);

        // Validate with ±1 window tolerance (90 seconds)
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);
        var window = new VerificationWindow(previous: 1, future: 1);
        
        if (!totp.VerifyTotp(code, out long timeStepMatched, window))
        {
            // Increment failed attempts
            mfa.FailedAttemptCount++;
            
            if (mfa.FailedAttemptCount >= _lockoutThreshold)
            {
                mfa.LockedUntil = DateTime.UtcNow.Add(_lockoutDuration);
                _logger.LogWarning("MFA account locked for user {UserId} after {Attempts} failed attempts", 
                    userId, mfa.FailedAttemptCount);
            }
            
            await _db.SaveChangesAsync();
            return false;
        }

        // Check for replay within same window
        if (mfa.LastValidatedAt.HasValue)
        {
            var lastTimeStep = (long)((mfa.LastValidatedAt.Value - DateTime.UnixEpoch).TotalSeconds / 30);
            if (timeStepMatched <= lastTimeStep)
            {
                _logger.LogWarning("MFA replay detected for user {UserId}", userId);
                return false;
            }
        }

        // Success - reset failed attempts and update last validated
        mfa.FailedAttemptCount = 0;
        mfa.LastValidatedAt = DateTime.UtcNow;
        mfa.LockedUntil = null;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Validates an MFA recovery code. If valid, the code is consumed and cannot be used again.
    /// </summary>
    /// <param name="userId">The ID of the user using the recovery code.</param>
    /// <param name="code">The recovery code to validate.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result is <c>true</c> if the code is valid; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> ValidateRecoveryCodeAsync(int userId, string code)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        if (mfa == null || !mfa.IsVerified || string.IsNullOrEmpty(mfa.RecoveryCodes))
            return false;

        // Enforce lockout — recovery codes must not bypass TOTP lockout
        if (mfa.LockedUntil.HasValue && mfa.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Recovery code validation blocked - account locked for user {UserId}", userId);
            return false;
        }

        var recoveryCodes = JsonSerializer.Deserialize<string[]>(mfa.RecoveryCodes);
        if (recoveryCodes == null) return false;

        // Hash the provided code and use constant-time comparison against stored hashes
        var codeHash = HashCode(code);
        var codeHashBytes = Encoding.UTF8.GetBytes(codeHash);
        var index = Array.FindIndex(recoveryCodes, h =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(h), codeHashBytes));
        
        if (index < 0) return false;

        // Remove used code
        var newCodes = recoveryCodes.Where((_, i) => i != index).ToArray();
        mfa.RecoveryCodes = JsonSerializer.Serialize(newCodes);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Recovery code used for user {UserId}", userId);

        return true;
    }

    /// <summary>
    /// Resets and removes MFA enrollment for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose MFA is to be reset.</param>
    /// <param name="resetByUserId">The ID of the user performing the reset operation (for auditing).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ResetMfaAsync(int userId, int resetByUserId)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        if (mfa != null)
        {
            _db.AppUserMfas.Remove(mfa);
            await _db.SaveChangesAsync();
        }

        _logger.LogWarning("MFA reset for user {UserId} by user {ResetBy}", userId, resetByUserId);
    }

    /// <summary>
    /// Retrieves the current MFA status for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AppUserMfa"/> entity if enrolled; otherwise, <c>null</c>.
    /// </returns>
    public async Task<AppUserMfa?> GetMfaStatusAsync(int userId)
    {
        return await _db.AppUserMfas.FindAsync(userId);
    }

    /// <summary>
    /// Unlocks a user's account if it was locked due to too many failed MFA attempts.
    /// </summary>
    /// <param name="userId">The ID of the user to unlock.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UnlockAccountAsync(int userId)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        if (mfa != null)
        {
            mfa.FailedAttemptCount = 0;
            mfa.LockedUntil = null;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Generates a new set of recovery codes for a user, replacing any existing ones.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to generate codes.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an array of new recovery codes.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the user is not enrolled in MFA.</exception>
    public async Task<string[]> GenerateRecoveryCodesAsync(int userId)
    {
        var mfa = await _db.AppUserMfas.FindAsync(userId);
        if (mfa == null || !mfa.IsVerified)
            throw new InvalidOperationException("MFA not enrolled");

        // Generate 10 new recovery codes
        var codes = new List<string>();
        var hashes = new List<string>();
        
        for (int i = 0; i < 10; i++)
        {
            var code = GenerateRandomCode(8);
            codes.Add(code);
            hashes.Add(HashCode(code));
        }

        mfa.RecoveryCodes = JsonSerializer.Serialize(hashes.ToArray());
        await _db.SaveChangesAsync();

        return codes.ToArray();
    }

    private static string[] GenerateRecoveryCodeHashes(int count)
    {
        var hashes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var code = GenerateRandomCode(8);
            hashes.Add(HashCode(code));
        }
        return hashes.ToArray();
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int maxUnbiased = 252; // 36 * 7 = 252, largest multiple of 36 <= 256
        var result = new char[length];
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < length; i++)
        {
            byte[] b = new byte[1];
            int value;
            do
            {
                rng.GetBytes(b);
                value = b[0];
            } while (value >= maxUnbiased);

            result[i] = chars[value % chars.Length];
        }

        return new string(result);
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
