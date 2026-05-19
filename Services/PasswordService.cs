using System.Security.Cryptography;
using System.Text;
using PanelTuner.Models;

namespace PanelTuner.Services;

public static class PasswordService
{
    public static void SetPassword(AppSettings settings, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPasswordWithSalt(password, salt);
        settings.Password.HashedPassword = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(AppSettings settings, string password)
    {
        if (string.IsNullOrEmpty(settings.Password.HashedPassword))
        {
            return false;
        }

        var parts = settings.Password.HashedPassword.Split(':');
        if (parts.Length != 2)
        {
            // Fallback for old unsalted passwords or invalid format
            return string.Equals(settings.Password.HashedPassword, HashPasswordLegacy(password));
        }

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var computedHash = HashPasswordWithSalt(password, salt);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    private static byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);

        return SHA256.HashData(combined);
    }

    private static string HashPasswordLegacy(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
