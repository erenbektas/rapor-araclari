using System.ComponentModel;
using System.Security.Authentication;

namespace RaporAraclari.Launcher.Services;

public static class TlsFailureDetector
{
    public static bool IsSecureChannelFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AuthenticationException)
            {
                return true;
            }

            if (current is Win32Exception win32Exception)
            {
                if (win32Exception.NativeErrorCode == unchecked((int)0x8009030E) ||
                    win32Exception.Message.Contains("No credentials are available in the security package", StringComparison.OrdinalIgnoreCase) ||
                    win32Exception.Message.Contains("Güvenlik paketinde kullanılabilir kimlik belgesi yok", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (current.Message.Contains("0x8009030E", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("The SSL connection could not be established", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string BuildUserFacingMessage(string toolName)
    {
        return $"{toolName} ile Schannel/TLS hatasi olustu. Bu makinede Windows TLS katmani GitHub baglantisini kuramiyor. "
             + "Launcher GitHub CLI fallback'i kullanmayi denedi; bu fallback de yoksa veya calismiyorsa GitHub CLI (gh) kurulu ve oturum acik olmali.";
    }
}

