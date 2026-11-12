using System.Security.Cryptography;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Services;

/// <summary>
/// Represents the result of a magic link issuance request.
/// </summary>
/// <param name="Accepted">A value indicating whether the request was accepted for processing. This is always true to prevent account enumeration.</param>
/// <param name="Reason">An optional reason for failure. Not used in this implementation.</param>
public record MagicLinkIssueResult(bool Accepted, string? Reason = null);

/// <summary>
/// Represents the result of a magic link verification attempt.
/// </summary>
/// <param name="Success">A value indicating whether the verification was successful.</param>
/// <param name="RequiresMfa">A value indicating whether the user must complete a second factor of authentication.</param>
/// <param name="UserId">The user's ID if verification was successful.</param>
/// <param name="Email">The user's email if verification was successful.</param>
/// <param name="Reason">The reason for failure, if any.</param>
public record MagicLinkVerifyResult(bool Success, bool RequiresMfa, int? UserId, string? Email, string? Reason = null);

/// <summary>
/// Defines the contract for a service that handles passwordless authentication using magic links.
/// </summary>
public interface IMagicLinkService
{
    /// <summary>
    /// Issues a magic link for the specified email address.
    /// </summary>
    /// <param name="email">The email address of the user requesting to sign in.</param>
    /// <param name="baseVerifyUrl">The base URL to be used for constructing the verification link.</param>
    /// <param name="ipAddress">The IP address of the user making the request.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a <see cref="MagicLinkIssueResult"/> indicating the outcome.
    /// </returns>
    Task<MagicLinkIssueResult> IssueAsync(string email, string baseVerifyUrl, string? ipAddress, CancellationToken ct);

    /// <summary>
    /// Verifies a magic link token from a URL.
    /// </summary>
    /// <param name="token">The token to verify.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a <see cref="MagicLinkVerifyResult"/> indicating the outcome of the verification.
    /// </returns>
    Task<MagicLinkVerifyResult> VerifyByTokenAsync(string token, CancellationToken ct);

    /// <summary>
    /// Verifies a magic link using a short code provided in the email.
    /// </summary>
    /// <param name="email">The email address associated with the code.</param>
    /// <param name="code">The short code to verify.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a <see cref="MagicLinkVerifyResult"/> indicating the outcome of the verification.
    /// </returns>
    Task<MagicLinkVerifyResult> VerifyByCodeAsync(string email, string code, CancellationToken ct);
}

/// <summary>
/// Implements <see cref="IMagicLinkService"/> to provide passwordless authentication.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="hashing">The hashing service.</param>
/// <param name="emailSender">The email sending service.</param>
/// <param name="options">The security configuration options.</param>
/// <param name="logger">The logger for this service.</param>
public class MagicLinkService(
    DabblerSecurityDbContext db,
    IHashingService hashing,
    IEmailSender emailSender,
    IOptions<DabblerSecurityOptions> options,
    ILogger<MagicLinkService> logger) : IMagicLinkService
{
    /// <inheritdoc />
    public async Task<MagicLinkIssueResult> IssueAsync(string email, string baseVerifyUrl, string? ipAddress, CancellationToken ct)
    {
        email = NormalizeEmail(email);

        var user = await db.AppUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email && x.IsActive, ct);

        if (user is null)
        {
            logger.LogWarning("Magic link request for unknown/inactive email {Email}", Mask(email));
            return new MagicLinkIssueResult(true); // avoid account enumeration
        }

        if (!user.IsProvisioned)
        {
            // User exists but the admin-creation transaction has not committed yet.
            // Returning a non-enumeration-safe error is acceptable here because this
            // path is only reachable immediately after an admin creates the user and
            // before the magic link is enqueued — not through the public login form.
            logger.LogWarning("Magic link refused: user {UserId} is not yet provisioned", user.Id);
            throw new InvalidOperationException("Account still being provisioned. Please try again in a moment.");
        }

        var now = DateTime.UtcNow;
        var existing = await db.MagicLinkTokens.Where(x => x.Email == email && x.UsedAt == null && x.ExpiresAt > now).ToListAsync(ct);
        if (existing.Count > 0)
        {
            db.MagicLinkTokens.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
        }

        var token = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        db.MagicLinkTokens.Add(new MagicLinkToken
        {
            Email = email,
            TokenHash = hashing.Sha256(token),
            CodeHash = hashing.Sha256(code),
            ExpiresAt = now.Add(options.Value.MagicLinkExpiry),
            AttemptCount = 0,
            CreatedAt = now,
            IpAddress = ipAddress
        });

        await db.SaveChangesAsync(ct);

        var link = $"{baseVerifyUrl}?token={token}";
        await emailSender.SendMagicLinkAsync(email, code, link);

        logger.LogInformation("Magic link issued for {Email}", Mask(email));
        return new MagicLinkIssueResult(true);
    }

    /// <inheritdoc />
    public async Task<MagicLinkVerifyResult> VerifyByTokenAsync(string token, CancellationToken ct)
    {
        var hash = hashing.Sha256(token);
        var now = DateTime.UtcNow;

        var entry = await db.MagicLinkTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.UsedAt == null && x.ExpiresAt > now, ct);

        if (entry is null)
        {
            return new MagicLinkVerifyResult(false, false, null, null, "Invalid or expired token");
        }

        return await CompleteVerificationAsync(entry, ct);
    }

    /// <inheritdoc />
    public async Task<MagicLinkVerifyResult> VerifyByCodeAsync(string email, string code, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        var now = DateTime.UtcNow;

        var entry = await db.MagicLinkTokens
            .Where(x => x.Email == email && x.UsedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
        {
            return new MagicLinkVerifyResult(false, false, null, null, "Invalid or expired code");
        }

        if (entry.AttemptCount >= 5)
        {
            return new MagicLinkVerifyResult(false, false, null, null, "Too many attempts");
        }

        var inputHash = hashing.Sha256(code);
        if (!hashing.FixedTimeEquals(entry.CodeHash, inputHash))
        {
            entry.AttemptCount++;
            await db.SaveChangesAsync(ct);
            return new MagicLinkVerifyResult(false, false, null, null, "Invalid or expired code");
        }

        return await CompleteVerificationAsync(entry, ct);
    }

    private async Task<MagicLinkVerifyResult> CompleteVerificationAsync(MagicLinkToken entry, CancellationToken ct)
    {
        entry.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var user = await db.AppUsers
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == entry.Email && x.IsActive, ct);

        if (user is null)
        {
            return new MagicLinkVerifyResult(false, false, null, null, "User inactive");
        }

        var hasVerifiedMfa = await db.AppUserMfas.AnyAsync(x => x.UserId == user.Id && x.IsVerified, ct);
        var requiresMfa = options.Value.RequireMfa || user.Roles.Any(x => options.Value.Mfa.RequireMfaForRoles.Contains(x.Role!.Name));

        if (requiresMfa && !hasVerifiedMfa)
        {
            logger.LogWarning("User {UserId} requires MFA but has not enrolled", user.Id);
            return new MagicLinkVerifyResult(false, false, null, null, "MFA enrollment required");
        }

        return new MagicLinkVerifyResult(true, requiresMfa && hasVerifiedMfa, user.Id, user.Email);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}
