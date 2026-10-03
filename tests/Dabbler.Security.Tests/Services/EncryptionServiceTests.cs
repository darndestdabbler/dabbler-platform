using System.Security.Cryptography;
using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

public class EncryptionServiceTests
{
    private static string GenerateValidKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public void Constructor_RejectsInvalidKeyLength()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);
        var act = () => new EncryptionService(shortKey);
        act.Should().Throw<ArgumentException>().WithMessage("*32 bytes*");
    }

    [Fact]
    public void Encrypt_ProducesVersionedGcmOutput()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "Hello, AES-GCM!"u8.ToArray();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);

        nonce.Should().HaveCount(12, "AES-GCM uses 12-byte nonce");
        // Format: 1 version byte + plaintext-length ciphertext + 16-byte tag
        ciphertext.Should().HaveCount(1 + plaintext.Length + 16);
        ciphertext[0].Should().Be(0x01, "version byte identifies AES-GCM");
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrips()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "TOTP secret bytes for MFA"u8.ToArray();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);
        var decrypted = svc.Decrypt(ciphertext, nonce);

        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Encrypt_ProducesDifferentNonceEachTime()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "same input"u8.ToArray();

        svc.Encrypt(plaintext, out var nonce1);
        svc.Encrypt(plaintext, out var nonce2);

        nonce1.Should().NotEqual(nonce2);
    }

    [Fact]
    public void Decrypt_DetectsTamperedCiphertext()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "sensitive data"u8.ToArray();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);
        // Tamper with encrypted payload (after version byte)
        ciphertext[1] ^= 0xFF;

        var act = () => svc.Decrypt(ciphertext, nonce);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_DetectsTamperedTag()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "sensitive data"u8.ToArray();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);
        // Tamper with authentication tag (last 16 bytes)
        ciphertext[^1] ^= 0xFF;

        var act = () => svc.Decrypt(ciphertext, nonce);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_RejectsWrongKey()
    {
        var svc1 = new EncryptionService(GenerateValidKey());
        var svc2 = new EncryptionService(GenerateValidKey());
        var plaintext = "key-specific data"u8.ToArray();

        var ciphertext = svc1.Encrypt(plaintext, out var nonce);

        var act = () => svc2.Decrypt(ciphertext, nonce);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_LegacyCbc_StillWorks()
    {
        // Simulate data encrypted with old AES-CBC (16-byte IV, no version prefix)
        var key = GenerateValidKey();
        var keyBytes = Convert.FromBase64String(key);
        var plaintext = "legacy TOTP secret"u8.ToArray();

        // Encrypt with AES-CBC manually (how old EncryptionService worked)
        byte[] cbcCiphertext;
        byte[] cbcIv;
        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.GenerateIV();
            cbcIv = aes.IV; // 16 bytes

            using var encryptor = aes.CreateEncryptor();
            cbcCiphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        cbcIv.Should().HaveCount(16, "AES-CBC uses 16-byte IV");
        // Legacy CBC ciphertext won't start with version byte 0x01
        // (CBC block cipher output is essentially random bytes)

        // Decrypt with new service — should use legacy CBC path
        var svc = new EncryptionService(key);
        var decrypted = svc.Decrypt(cbcCiphertext, cbcIv);

        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Decrypt_GcmCiphertext_NotDowngradedToCbc_WhenIvPaddedTo16Bytes()
    {
        // Regression test: attacker pads GCM nonce to 16 bytes
        // to try forcing the CBC path. The version prefix prevents this.
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "sensitive"u8.ToArray();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);

        // Pad GCM nonce to 16 bytes — should still take GCM path
        // because detection is by version prefix, not IV length
        var paddedIv = new byte[16];
        nonce.CopyTo(paddedIv, 0);

        // Version byte 0x01 present → GCM path. Wrong nonce size → rejected.
        var act = () => svc.Decrypt(ciphertext, paddedIv);
        act.Should().Throw<Exception>(); // ArgumentException for invalid nonce size
    }

    [Fact]
    public void Decrypt_RejectsTooShortGcmBlob()
    {
        var svc = new EncryptionService(GenerateValidKey());
        // Version byte + less than 16-byte tag
        var shortBlob = new byte[] { 0x01, 0x00, 0x00 };
        var nonce = new byte[12];

        var act = () => svc.Decrypt(shortBlob, nonce);
        act.Should().Throw<CryptographicException>().WithMessage("*too short*");
    }

    [Fact]
    public void Encrypt_Decrypt_EmptyPlaintext()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = Array.Empty<byte>();

        var ciphertext = svc.Encrypt(plaintext, out var nonce);
        // 1 version byte + 16-byte tag
        ciphertext.Should().HaveCount(17, "empty plaintext produces version + tag-only output");

        var decrypted = svc.Decrypt(ciphertext, nonce);
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_ZerosKey_PreventsReuse()
    {
        var svc = new EncryptionService(GenerateValidKey());
        var plaintext = "test"u8.ToArray();

        // Service works before disposal
        var ct = svc.Encrypt(plaintext, out _);
        ct.Should().NotBeEmpty();

        svc.Dispose();

        var encryptAct = () => svc.Encrypt(plaintext, out _);
        encryptAct.Should().Throw<ObjectDisposedException>();

        var decryptAct = () => svc.Decrypt(ct, new byte[12]);
        decryptAct.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var svc = new EncryptionService(GenerateValidKey());
        svc.Dispose();
        svc.Dispose(); // Should not throw
    }
}
