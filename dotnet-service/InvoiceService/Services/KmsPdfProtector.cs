using System.Security.Cryptography;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace InvoiceService.Services;

// File format: MAGIC(6) | keyLen(4) | encryptedDataKey | nonce(12) | tag(16) | ciphertext
public class KmsPdfProtector : IPdfProtector
{
    private static readonly byte[] Magic = "LGPDF1"u8.ToArray();
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IAmazonKeyManagementService _kms;
    private readonly string _keyId;

    public KmsPdfProtector(IAmazonKeyManagementService kms, IConfiguration config)
    {
        _kms = kms;
        _keyId = config["KMS_KEY_ID"]
            ?? throw new InvalidOperationException("KMS_KEY_ID is not set.");
    }

    public bool IsEncrypted => true;

    public async Task<byte[]> ProtectAsync(byte[] pdfBytes)
    {
        var dk = await _kms.GenerateDataKeyAsync(new GenerateDataKeyRequest
        {
            KeyId = _keyId,
            KeySpec = DataKeySpec.AES_256
        });

        var plainKey = dk.Plaintext.ToArray();
        var encKey = dk.CiphertextBlob.ToArray();

        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var tag = new byte[TagSize];
            var cipher = new byte[pdfBytes.Length];

            using (var aes = new AesGcm(plainKey, TagSize))
            {
                aes.Encrypt(nonce, pdfBytes, cipher, tag, Magic);
            }

            using var ms = new MemoryStream();
            ms.Write(Magic);
            ms.Write(BitConverter.GetBytes(encKey.Length));
            ms.Write(encKey);
            ms.Write(nonce);
            ms.Write(tag);
            ms.Write(cipher);
            return ms.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainKey);
        }
    }

    public async Task<byte[]> UnprotectAsync(byte[] data)
    {
        if (data.Length < Magic.Length + 4 || !data.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new CryptographicException("Not a LedgerGuard encrypted PDF.");

        var pos = Magic.Length;
        var keyLen = BitConverter.ToInt32(data, pos);
        pos += 4;
        var encKey = data.AsSpan(pos, keyLen).ToArray();
        pos += keyLen;
        var nonce = data.AsSpan(pos, NonceSize).ToArray();
        pos += NonceSize;
        var tag = data.AsSpan(pos, TagSize).ToArray();
        pos += TagSize;
        var cipher = data.AsSpan(pos).ToArray();

        var res = await _kms.DecryptAsync(new DecryptRequest
        {
            KeyId = _keyId,
            CiphertextBlob = new MemoryStream(encKey)
        });
        var plainKey = res.Plaintext.ToArray();

        try
        {
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(plainKey, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain, Magic);
            return plain;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainKey);
        }
    }
}