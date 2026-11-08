using System.Security.Cryptography;
using System.Text;

namespace Dabbler.Security.Services;

/// <summary>
/// Defines a service for hashing data and performing cryptographic comparisons.
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Computes the SHA-256 hash of a string.
    /// </summary>
    /// <param name="value">The input string to hash. The string is UTF-8 encoded before hashing.</param>
    /// <returns>A 32-byte array representing the SHA-256 hash.</returns>
    byte[] Sha256(string value);
    
    /// <summary>
    /// Compares two byte arrays for equality in a way that is resistant to timing attacks.
    /// </summary>
    /// <param name="left">The first byte array to compare.</param>
    /// <param name="right">The second byte array to compare.</param>
    /// <returns><c>true</c> if the two arrays have the same length and content; otherwise, <c>false</c>.</returns>
    bool FixedTimeEquals(byte[] left, byte[] right);
}

/// <summary>
/// Implements <see cref="IHashingService"/> using standard .NET cryptographic primitives.
/// </summary>
public class HashingService : IHashingService
{
    /// <inheritdoc/>
    public byte[] Sha256(string value)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(value));
    }

    /// <inheritdoc/>
    public bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
