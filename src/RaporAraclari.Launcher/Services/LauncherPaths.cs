namespace RaporAraclari.Launcher.Services;

public static class LauncherPaths
{
    public static string RootDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaporAraclariLauncher");

    public static string StateFilePath => Path.Combine(RootDirectory, "launcher-state.json");

    public static string DownloadsDirectory => Path.Combine(RootDirectory, "downloads");

    public static string PortableAppsDirectory => Path.Combine(RootDirectory, "apps");

    public static void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DownloadsDirectory);
        Directory.CreateDirectory(PortableAppsDirectory);
    }

    public static string GetPortableInstallDirectory(string appFolderName, string version)
    {
        return Path.Combine(PortableAppsDirectory, appFolderName, SanitizePathSegment(version));
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalidChars.Contains(character) ? '_' : character));
    }
}

