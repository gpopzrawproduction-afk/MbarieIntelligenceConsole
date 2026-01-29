using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace MIC.Desktop.Avalonia.Views;

public partial class SplashWindow : Window
{
    private readonly Border? _loadingBar;
    private readonly TextBlock? _statusText;
    private readonly TextBlock? _versionText;
    private readonly Border? _logoContainer;

    public SplashWindow()
    {
        InitializeComponent();
        
        _loadingBar = this.FindControl<Border>("LoadingBar");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _versionText = this.FindControl<TextBlock>("VersionText");
        _logoContainer = this.FindControl<Border>("LogoContainer");

        // Set version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (_versionText != null && version != null)
        {
            _versionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Runs the splash screen animation and initialization.
    /// </summary>
    /// <param name="initializationTask">The async task to run during splash.</param>
    /// <returns>True when complete.</returns>
    public async Task<bool> RunAsync(Func<IProgress<(string message, double progress)>, Task> initializationTask)
    {
        var progress = new Progress<(string message, double progress)>(report =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateStatus(report.message, report.progress);
            });
        });

        // Start logo pulse animation
        StartLogoAnimation();

        try
        {
            // Run initialization with progress reporting
            await initializationTask(progress);

            // Complete the loading bar
            UpdateStatus("Ready", 100);
            await Task.Delay(300); // Brief pause at 100%

            return true;
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}", 0);
            await Task.Delay(2000);
            return false;
        }
    }

    /// <summary>
    /// Updates the loading status display.
    /// </summary>
    public void UpdateStatus(string message, double progressPercent)
    {
        if (_statusText != null)
        {
            _statusText.Text = message;
        }

        if (_loadingBar != null)
        {
            // Animate width change
            _loadingBar.Width = progressPercent / 100.0 * 200;
        }
    }

    private void StartLogoAnimation()
    {
        if (_logoContainer == null) return;

        // Create subtle pulse animation using opacity
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(2),
            IterationCount = IterationCount.Infinite,
            PlaybackDirection = PlaybackDirection.Alternate,
            Easing = new SineEaseInOut(),
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(OpacityProperty, 0.8) },
                    Cue = new Cue(0)
                },
                new KeyFrame
                {
                    Setters = { new Setter(OpacityProperty, 1.0) },
                    Cue = new Cue(1)
                }
            }
        };

        animation.RunAsync(_logoContainer);
    }

    /// <summary>
    /// Fades out the splash window.
    /// </summary>
    public async Task FadeOutAsync()
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters = { new Setter(OpacityProperty, 1.0) },
                    Cue = new Cue(0)
                },
                new KeyFrame
                {
                    Setters = { new Setter(OpacityProperty, 0.0) },
                    Cue = new Cue(1)
                }
            }
        };

        await animation.RunAsync(this);
    }
}
