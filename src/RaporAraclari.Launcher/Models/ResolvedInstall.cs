namespace RaporAraclari.Launcher.Models;

public sealed class ResolvedInstall
{
    public bool IsInstalled => !string.IsNullOrWhiteSpace(ExecutablePath) && File.Exists(ExecutablePath);
    public string? ExecutablePath { get; init; }
    public string? Version { get; init; }
    public string? AssetKind { get; init; }
}

