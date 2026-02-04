using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for handling application updates
/// </summary>
public class UpdateViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;
    private readonly ILogger<UpdateViewModel> _logger;

    private bool _isCheckingForUpdates;
    private bool _isDownloading;
    private bool _updateAvailable;
    private string _currentVersion;
    private string _latestVersion = string.Empty;
    private string _releaseNotes = string.Empty;
    private double _downloadProgress;
    private string _statusMessage = string.Empty;
    private bool _isUpdateRequired;

    public UpdateViewModel(UpdateService updateService, ILogger<UpdateViewModel> logger)
    {
        _updateService = updateService;
        _logger = logger;

        // Get current version from assembly
        _currentVersion = GetCurrentVersion();

        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdatesAsync);
        DownloadUpdateCommand = ReactiveCommand.Create(() => DownloadUpdateAsync());
        SkipUpdateCommand = ReactiveCommand.Create(() => { UpdateAvailable = false; });
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        set => this.RaiseAndSetIfChanged(ref _currentVersion, value);
    }

    public string LatestVersion
    {
        get => _latestVersion;
        set => this.RaiseAndSetIfChanged(ref _latestVersion, value);
    }

    public string ReleaseNotes
    {
        get => _releaseNotes;
        set => this.RaiseAndSetIfChanged(ref _releaseNotes, value);
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        set => this.RaiseAndSetIfChanged(ref _updateAvailable, value);
    }

    public bool IsUpdateRequired
    {
        get => _isUpdateRequired;
        set => this.RaiseAndSetIfChanged(ref _isUpdateRequired, value);
    }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        set => this.RaiseAndSetIfChanged(ref _isCheckingForUpdates, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<Unit, Task<Unit>> DownloadUpdateCommand { get; }
    public ReactiveCommand<Unit, Unit> SkipUpdateCommand { get; }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            IsCheckingForUpdates = true;
            StatusMessage = "Checking for updates...";

            var updateInfo = await _updateService.CheckForUpdatesAsync(CurrentVersion);

            if (updateInfo != null)
            {
                LatestVersion = updateInfo.Version;
                ReleaseNotes = updateInfo.ReleaseNotes;
                IsUpdateRequired = updateInfo.IsRequired;
                UpdateAvailable = true;
                StatusMessage = $"Update available: {updateInfo.Version}";
                _logger.LogInformation("Update available: {Version}", updateInfo.Version);
            }
            else
            {
                UpdateAvailable = false;
                StatusMessage = "You have the latest version";
                _logger.LogInformation("No updates available");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to check for updates";
            _logger.LogError(ex, "Failed to check for updates");
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private async Task<Unit> DownloadUpdateAsync()
    {
        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            StatusMessage = "Downloading update...";

            var progress = new Progress<double>(p =>
            {
                DownloadProgress = p;
                StatusMessage = $"Downloading update... {p:F1}%";
            });

            // Note: In a real implementation, you'd get the update info from the CheckForUpdatesAsync result
            var updateInfo = new UpdateService.UpdateInfo
            {
                Version = LatestVersion,
                DownloadUrl = $"https://github.com/your-org/mbarie-intelligence-console/releases/download/v{LatestVersion}/MIC.Desktop.Avalonia.msix"
            };

            var success = await _updateService.DownloadAndInstallUpdateAsync(updateInfo, progress);

            if (success)
            {
                StatusMessage = "Update installed successfully. Please restart the application.";
                UpdateAvailable = false;
                _logger.LogInformation("Update installed successfully");
            }
            else
            {
                StatusMessage = "Failed to install update";
                _logger.LogError("Failed to install update");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to download update";
            _logger.LogError(ex, "Failed to download update");
        }
        finally
        {
            IsDownloading = false;
        }

        return Unit.Default;
    }

    private string GetCurrentVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
}