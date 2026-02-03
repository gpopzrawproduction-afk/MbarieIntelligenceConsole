using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MIC.Desktop.Avalonia.Views;

public partial class SearchHelpDialog : Window
{
    public SearchHelpDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
