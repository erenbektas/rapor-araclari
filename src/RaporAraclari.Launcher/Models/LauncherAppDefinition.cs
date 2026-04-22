namespace RaporAraclari.Launcher.Models;

public sealed class LauncherAppDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RepoOwner { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#0F172A";
    public string AccentLightColor { get; set; } = "#E2E8F0";
    public string SetupAssetRegex { get; set; } = string.Empty;
    public string? PortableAssetRegex { get; set; }
    public string[] SilentInstallArguments { get; set; } = [];
    public string InstalledExeRelativePath { get; set; } = string.Empty;
    public string[] KnownInstallDirectories { get; set; } = [];
    public string PortableInstallRoot { get; set; } = string.Empty;
}

