namespace RaporAraclari.Launcher.Models;

public sealed class LauncherState
{
    public Dictionary<string, AppInstallationRecord> Apps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

