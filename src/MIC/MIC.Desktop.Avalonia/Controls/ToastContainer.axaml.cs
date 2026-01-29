using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.Controls;

public partial class ToastContainer : UserControl
{
    public ToastContainer()
    {
        InitializeComponent();
        DataContext = NotificationService.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void OnDismissClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ToastNotification notification)
        {
            NotificationService.Instance.Dismiss(notification);
        }
    }
}
