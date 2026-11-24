using System.Security.Cryptography;
using System.Text;

namespace InvoiceService.Services;

// DEV TESTING ONLY. Production-la KmsPdfProtector use aagum.
// File format: MAGIC(6) | nonce(12) | tag(16) | ciphertext
public class LocalAesPdfProtector : IPdfProtector
{
    private static readonly byte[] Magic = "LGPDF0"u8.ToArray();
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public LocalAesPdfProtector(IConfiguration config)
    {
        var secret = config["LOCAL_DEV_ENC_KEY"]
            ?? throw new InvalidOperationException("LOCAL_DEV_ENC_KEY is not set.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public bool IsEncrypted => true;

    public Task<byte[]> ProtectAsync(byte[] pdfBytes)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[pdfBytes.Length];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, pdfBytes, cipher, tag, Magic);
        }

        using var ms = new MemoryStream();
        ms.Write(Magic);
        ms.Write(nonce);
        ms.Write(tag);
        ms.Write(cipher);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> UnprotectAsync(byte[] data)
    {
        if (data.Length < Magic.Length + NonceSize + TagSize
            || !data.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new CryptographicException("Not a local dev encrypted PDF.");

        var pos = Magic.Length;
        var nonce = data.AsSpan(pos, NonceSize).ToArray();
        pos += NonceSize;
        var tag = data.AsSpan(pos, TagSize).ToArray();
        pos += TagSize;
        var cipher = data.AsSpan(pos).ToArray();

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, Magic);
        return Task.FromResult(plain);
    }
}