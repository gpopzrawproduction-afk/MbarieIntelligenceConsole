using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MIC.Desktop.Avalonia.Controls;

/// <summary>
/// Empty state component for views with no data.
/// </summary>
public partial class EmptyState : UserControl
{
    public EmptyState()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
