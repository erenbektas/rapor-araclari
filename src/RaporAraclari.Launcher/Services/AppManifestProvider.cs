using System.Text.Json;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Services;

public sealed class AppManifestProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<LauncherAppDefinition>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Launcher uygulama manifesti bulunamadi.", manifestPath);
        }

        await using var stream = File.OpenRead(manifestPath);
        var definitions = await JsonSerializer.DeserializeAsync<List<LauncherAppDefinition>>(stream, SerializerOptions, cancellationToken);
        return definitions ?? [];
    }
}

