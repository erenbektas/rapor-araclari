namespace RaporAraclari.Launcher.Models;

public sealed class UpdateCandidate
{
    public required LauncherAppDefinition Definition { get; init; }
    public required GitHubReleaseInfo Release { get; init; }
    public required string CurrentVersion { get; init; }
    public required ReleaseAssetKind AssetKind { get; init; }
}
