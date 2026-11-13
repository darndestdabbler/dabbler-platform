using System.Security.Cryptography;
using Dabbler.Security.Abstractions;

namespace Dabbler.Security.Services;

/// <summary>
/// Provides encryption and decryption services using AES-256-GCM.
/// This service is designed to be a singleton or scoped dependency and is disposable to securely clear the encryption key from memory.
/// </summary>
public class EncryptionService : IEncryptionService, IDisposable
{
    private const byte VersionGcm = 0x01;
    private const int GcmNonceSize = 12; // AES-GCM standard nonce size
    private const int GcmTagSize = 16;   // AES-GCM authentication tag size
    // Minimum GCM blob: 1-byte version + 16-byte tag (empty plaintext)
    private const int MinGcmBlobSize = 1 + GcmTagSize;

    private readonly byte[] _key;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class with a specified encryption key.
    /// </summary>
    /// <param name="base64Key">A Base64-encoded string representing the 32-byte (256-bit) encryption key.</param>
    /// <exception cref="ArgumentException">Thrown if the decoded key is not exactly 32 bytes long.</exception>
    public EncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32) // 256 bits
            throw new ArgumentException("Encryption key must be 32 bytes (256 bits)");
    }

    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="iv">When this method returns, contains the 12-byte nonce (initialization vector) used for the encryption. This value is required for decryption.</param>
    /// <returns>The encrypted data, formatted as [1-byte version][ciphertext][16-byte tag].</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public byte[] Encrypt(byte[] plaintext, out byte[] iv)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // AES-GCM: authenticated encryption — eliminates padding oracle risk
        var nonce = new byte[GcmNonceSize];
        RandomNumberGenerator.Fill(nonce);
        iv = nonce;

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[GcmTagSize];

        using var aesGcm = new AesGcm(_key, GcmTagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Format: [version byte] [ciphertext] [tag]
        var result = new byte[1 + ciphertext.Length + GcmTagSize];
        result[0] = VersionGcm;
        ciphertext.CopyTo(result, 1);
        tag.CopyTo(result, 1 + ciphertext.Length);
        return result;
    }

    /// <summary>
    /// Decrypts ciphertext. This method supports both the current AES-GCM format and a legacy AES-CBC format for backward compatibility.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="iv">The nonce (initialization vector) that was used for encryption.</param>
    /// <returns>The original plaintext data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails, for example, due to an invalid authentication tag or incorrect key.</exception>
    public byte[] Decrypt(byte[] ciphertext, byte[] iv)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ciphertext.Length > 0 && ciphertext[0] == VersionGcm)
            return DecryptGcm(ciphertext, iv);

        // Legacy AES-CBC path for data encrypted before AES-GCM migration
        return DecryptLegacyCbc(ciphertext, iv);
    }

    private byte[] DecryptGcm(byte[] blob, byte[] nonce)
    {
        if (blob.Length < MinGcmBlobSize)
            throw new CryptographicException("Ciphertext too short for AES-GCM decryption.");

        // Strip version byte, split ciphertext and tag
        var actualCiphertext = blob.AsSpan(1, blob.Length - 1 - GcmTagSize);
        var tag = blob.AsSpan(blob.Length - GcmTagSize, GcmTagSize);
        var plaintext = new byte[actualCiphertext.Length];

        using var aesGcm = new AesGcm(_key, GcmTagSize);
        aesGcm.Decrypt(nonce, actualCiphertext, tag, plaintext);
        return plaintext;
    }

    private byte[] DecryptLegacyCbc(byte[] ciphertext, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="EncryptionService"/>, securely clearing the encryption key from memory.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_key);
            _disposed = true;
        }
    }
}
