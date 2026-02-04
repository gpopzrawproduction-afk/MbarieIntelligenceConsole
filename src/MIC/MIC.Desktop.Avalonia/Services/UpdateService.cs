using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Service for checking and downloading application updates
/// </summary>
public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Represents update information from the server
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    /// Checks for available updates
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(string currentVersion)
    {
        try
        {
            var updateUrl = "https://api.github.com/repos/your-org/mbarie-intelligence-console/releases/latest";

            // For now, simulate update check - replace with actual API call
            var latestRelease = await _httpClient.GetFromJsonAsync<GitHubRelease>(updateUrl);

            if (latestRelease == null)
                return null;

            var latestVersion = latestRelease.tag_name.TrimStart('v');
            if (IsNewerVersion(latestVersion, currentVersion))
            {
                return new UpdateInfo
                {
                    Version = latestVersion,
                    DownloadUrl = latestRelease.assets?
                        .FirstOrDefault(a => a.name.Contains(".msix"))?.browser_download_url ?? "",
                    ReleaseNotes = latestRelease.body ?? "",
                    IsRequired = IsMajorUpdate(latestVersion, currentVersion),
                    Size = latestRelease.assets?.FirstOrDefault(a => a.name.Contains(".msix"))?.size ?? 0
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return null;
        }
    }

    /// <summary>
    /// Downloads and installs the update
    /// </summary>
    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<double> progress)
    {
        try
        {
            _logger.LogInformation("Downloading update {Version}", updateInfo.Version);

            using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create("update.msix");

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var progressValue = (double)downloadedBytes / totalBytes * 100;
                    progress.Report(progressValue);
                }
            }

            // Install the update
            return await InstallUpdateAsync("update.msix");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and install update");
            return false;
        }
    }

    private async Task<bool> InstallUpdateAsync(string msixPath)
    {
        try
        {
            // Use Windows Package Manager or direct MSIX installation
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Add-AppxPackage -Path '{msixPath}' -ForceApplicationShutdown\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install update");
            return false;
        }
    }

    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        try
        {
            var latest = Version.Parse(latestVersion);
            var current = Version.Parse(currentVersion);
            return latest > current;
        }
        catch
        {
            return false;
        }
    }

    private bool IsMajorUpdate(string latestVersion, string currentVersion)
    {
        try
        {
            var latest = Version.Parse(latestVersion);
            var current = Version.Parse(currentVersion);
            return latest.Major > current.Major;
        }
        catch
        {
            return false;
        }
    }

    // GitHub API response models
    private class GitHubRelease
    {
        public string tag_name { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
        public List<GitHubAsset> assets { get; set; } = new();
    }

    private class GitHubAsset
    {
        public string name { get; set; } = string.Empty;
        public string browser_download_url { get; set; } = string.Empty;
        public long size { get; set; }
    }
}