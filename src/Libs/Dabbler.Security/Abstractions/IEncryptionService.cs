namespace Dabbler.Security.Abstractions;

/// <summary>
/// Defines an interface for a service capable of performing symmetric encryption and decryption operations.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the provided plaintext data using a symmetric encryption algorithm.
    /// A new Initialization Vector (IV) is generated for each encryption operation.
    /// </summary>
    /// <param name="plaintext">The data to be encrypted.</param>
    /// <param name="iv">When this method returns, contains the Initialization Vector (IV) used for encryption.
    /// This IV must be stored and provided to the <see cref="Decrypt(byte[], byte[])"/> method for successful decryption.</param>
    /// <returns>The encrypted ciphertext as a byte array.</returns>
    byte[] Encrypt(byte[] plaintext, out byte[] iv);

    /// <summary>
    /// Decrypts the provided ciphertext using a symmetric encryption algorithm and the given Initialization Vector (IV).
    /// </summary>
    /// <param name="ciphertext">The data to be decrypted.</param>
    /// <param name="iv">The Initialization Vector (IV) that was used during the encryption of the ciphertext.</param>
    /// <returns>The decrypted plaintext as a byte array.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if decryption fails (e.g., incorrect key or IV).</exception>
    byte[] Decrypt(byte[] ciphertext, byte[] iv);
}
