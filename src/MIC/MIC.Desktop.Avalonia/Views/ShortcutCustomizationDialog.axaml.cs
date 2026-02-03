using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MIC.Desktop.Avalonia.Views;

public partial class ShortcutCustomizationDialog : Window
{
    public ShortcutCustomizationDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
