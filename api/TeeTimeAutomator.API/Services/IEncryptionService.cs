namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive strings.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string using AES-256.
    /// </summary>
    /// <param name="plaintext">The text to encrypt.</param>
    /// <returns>The encrypted text as a Base64 string.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="ciphertext">The encrypted text as a Base64 string.</param>
    /// <returns>The decrypted plaintext.</returns>
    string Decrypt(string ciphertext);
}
