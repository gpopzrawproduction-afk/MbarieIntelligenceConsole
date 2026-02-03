using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MIC.Desktop.Avalonia.Views;

public partial class OnboardingTourDialog : Window
{
    public OnboardingTourDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
