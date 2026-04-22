namespace RaporAraclari.Launcher.Models;

public sealed class AppInstallationRecord
{
    public string? InstalledVersion { get; set; }
    public string? InstalledPath { get; set; }
    public string? LastKnownReleaseVersion { get; set; }
    public DateTimeOffset? LastCheckedUtc { get; set; }
    public DateTimeOffset? LastUpdatedUtc { get; set; }
    public string? LastError { get; set; }
    public string? LastAssetKind { get; set; }
}

