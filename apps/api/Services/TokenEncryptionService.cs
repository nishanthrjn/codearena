// Placeholder for TokenEncryptionService.cs
using Microsoft.AspNetCore.DataProtection;

namespace CodeArena.Api.Services;

public interface ITokenEncryptionService
{
    string Encrypt(string plaintext);
    /// <summary>
    /// Decrypts a previously encrypted token.
    /// Throws <see cref="TokenDecryptionException"/> if keys have rotated or the ciphertext is corrupted.
    /// Callers should catch this and prompt the user to re-authenticate.
    /// </summary>
    string Decrypt(string ciphertext);
}

/// <summary>L-4: Raised when the stored GitHub token can no longer be decrypted (e.g. key rotation).</summary>
public sealed class TokenDecryptionException(Exception inner)
    : Exception("GitHub token could not be decrypted. The user must re-authenticate.", inner);

public class TokenEncryptionService(IDataProtectionProvider dp) : ITokenEncryptionService
{
    private readonly IDataProtector _protector =
        dp.CreateProtector("CodeArena.GitHubToken.v1");

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // L-4: keys have rotated or ciphertext is corrupted; surface as domain exception
            throw new TokenDecryptionException(ex);
        }
    }
}