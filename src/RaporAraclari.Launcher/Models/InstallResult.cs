namespace RaporAraclari.Launcher.Models;

public sealed class InstallResult
{
    public required string InstalledPath { get; init; }
    public required string InstalledVersion { get; init; }
    public required ReleaseAssetKind AssetKind { get; init; }
}

