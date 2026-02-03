using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class KeyboardShortcutsDialog : Window
{
    public KeyboardShortcutsDialog()
    {
        InitializeComponent();
        
        var viewModel = new KeyboardShortcutsDialogViewModel();
        viewModel.RequestClose += () => Close();
        DataContext = viewModel;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}