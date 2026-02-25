// Placeholder for TokenEncryptionService.cs
using Microsoft.AspNetCore.DataProtection;

namespace CodeArena.Api.Services;

public interface ITokenEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class TokenEncryptionService(IDataProtectionProvider dp) : ITokenEncryptionService
{
    private readonly IDataProtector _protector =
        dp.CreateProtector("CodeArena.GitHubToken.v1");

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);
    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
}