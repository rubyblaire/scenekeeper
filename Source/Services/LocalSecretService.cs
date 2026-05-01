using System.Security.Cryptography;
using System.Text;

namespace SceneKeeper.Services;

public static class LocalSecretService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SceneKeeper.AidenAssist.v1");

    public static string ProtectString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string UnprotectString(string protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
            return string.Empty;
        try
        {
            var protectedBytes = Convert.FromBase64String(protectedBase64);
            var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch { return string.Empty; }
    }
}
