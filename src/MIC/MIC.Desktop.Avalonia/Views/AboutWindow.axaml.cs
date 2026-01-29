using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MIC.Desktop.Avalonia.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        // Set version from assembly
        var versionText = this.FindControl<TextBlock>("VersionText");
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (versionText != null && version != null)
        {
            versionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
