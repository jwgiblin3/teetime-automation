using System.Security.Cryptography;
using System.Text;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of encryption service using AES-256.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly string _key;
    private readonly string _iv;
    private readonly ILogger<EncryptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the EncryptionService.
    /// </summary>
    /// <param name="configuration">Application configuration containing encryption key and IV.</param>
    /// <param name="logger">Logger instance.</param>
    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _key = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key not configured");
        _iv = configuration["Encryption:IV"] ?? throw new InvalidOperationException("Encryption:IV not configured");
        _logger = logger;

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption key must be 32 characters (256 bits)");
        }

        if (_iv.Length != 16)
        {
            throw new InvalidOperationException("Encryption IV must be 16 characters (128 bits)");
        }
    }

    /// <summary>
    /// Encrypts a plaintext string using AES-256 in CBC mode.
    /// </summary>
    public string Encrypt(string plaintext)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plaintext);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during encryption");
            throw;
        }
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted string.
    /// </summary>
    public string Decrypt(string ciphertext)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(Convert.FromBase64String(ciphertext)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during decryption");
            throw;
        }
    }
}
