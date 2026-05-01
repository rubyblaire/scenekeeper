using System.Security.Cryptography;
using System.Text;

namespace SceneKeeper.Services;

public static class OwnerUnlockService
{
    private const int Iterations = 200_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static (string SaltBase64, string HashBase64) CreatePasswordHash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPassword(password, salt);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool VerifyPassword(string password, string saltBase64, string hashBase64)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(saltBase64) || string.IsNullOrWhiteSpace(hashBase64))
            return false;
        try
        {
            var salt = Convert.FromBase64String(saltBase64);
            var expectedHash = Convert.FromBase64String(hashBase64);
            var actualHash = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch { return false; }
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }
}
