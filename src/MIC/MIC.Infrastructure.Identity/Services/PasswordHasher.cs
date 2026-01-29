using System;
using System.Security.Cryptography;
using System.Text;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Infrastructure.Identity.Services;

/// <summary>
/// Simple password hasher using PBKDF2 with HMACSHA256.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 10000;

    public (string hash, string salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // Generate random salt
        var saltBytes = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        // Hash password with salt
        var hashBytes = HashPasswordWithSalt(password, saltBytes);

        // Convert to base64 strings
        var hash = Convert.ToBase64String(hashBytes);
        var salt = Convert.ToBase64String(saltBytes);

        return (hash, salt);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
            return false;

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Convert.FromBase64String(hash);

            var computedHash = HashPasswordWithSalt(password, saltBytes);

            // Constant-time comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
        }
        catch (FormatException)
        {
            // Invalid base64 strings
            return false;
        }
    }

    private static byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(HashSize);
    }
}