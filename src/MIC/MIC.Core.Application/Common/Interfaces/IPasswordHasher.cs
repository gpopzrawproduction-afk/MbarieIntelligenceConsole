using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Abstraction for hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    (string hash, string salt) HashPassword(string password);

    bool VerifyPassword(string password, string hash, string salt);
}