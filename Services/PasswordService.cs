using System.Security.Cryptography;
using System.Text;
using PanelTuner.Models;

namespace PanelTuner.Services;

public static class PasswordService
{
    public static void SetPassword(AppSettings settings, string password)
    {
        settings.Password.HashedPassword = HashPassword(password);
    }

    public static bool Verify(AppSettings settings, string password)
    {
        if (string.IsNullOrEmpty(settings.Password.HashedPassword))
        {
            return false;
        }

        return string.Equals(settings.Password.HashedPassword, HashPassword(password));
    }

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
