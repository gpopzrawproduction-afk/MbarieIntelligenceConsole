using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Desktop.Avalonia.Views;

namespace MIC.Desktop.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // TEMPORARY: Skip login, go straight to dashboard
                    Services.UserSessionService.Instance.SetSession(
                        Guid.NewGuid().ToString(),
                        "demo",
                        "demo@mbarie.com",
                        "Demo User",
                        "demo-token");
                    
                    desktop.MainWindow = new MainWindow();
                }
            }
            catch (Exception)
            {
                // Fallback to a simple window if there's an error
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var errorWindow = new MainWindow();
                    desktop.MainWindow = errorWindow;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
