using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Services;

public sealed class GitHubReleaseService : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GitHubReleaseService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RaporAraclariLauncher", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<GitHubReleaseInfo> GetLatestReleaseAsync(LauncherAppDefinition definition, CancellationToken cancellationToken = default)
    {
        var requestUri = $"https://api.github.com/repos/{definition.RepoOwner}/{definition.RepoName}/releases/latest";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{definition.DisplayName} icin surum bilgisi alinamadi. GitHub yaniti: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(payload, SerializerOptions);
        if (release is null)
        {
            throw new InvalidOperationException($"{definition.DisplayName} icin GitHub release verisi cozumlenemedi.");
        }

        return release;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
