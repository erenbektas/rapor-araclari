using System.Text.Json;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Services;

public sealed class LauncherStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<LauncherState> LoadAsync(CancellationToken cancellationToken = default)
    {
        LauncherPaths.EnsureBaseDirectories();

        if (!File.Exists(LauncherPaths.StateFilePath))
        {
            return new LauncherState();
        }

        await using var stream = File.OpenRead(LauncherPaths.StateFilePath);
        var state = await JsonSerializer.DeserializeAsync<LauncherState>(stream, SerializerOptions, cancellationToken);
        return state ?? new LauncherState();
    }

    public async Task SaveAsync(LauncherState state, CancellationToken cancellationToken = default)
    {
        LauncherPaths.EnsureBaseDirectories();

        await using var stream = File.Create(LauncherPaths.StateFilePath);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
    }
}

