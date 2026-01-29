using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MIC.Desktop.Avalonia.Controls;

/// <summary>
/// Animated loading spinner with neon glow effect.
/// </summary>
public partial class LoadingSpinner : UserControl
{
    public LoadingSpinner()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
