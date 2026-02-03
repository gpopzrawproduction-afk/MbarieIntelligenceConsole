using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views.Dialogs;

public partial class ComposeEmailDialog : Window
{
    public ComposeEmailDialog()
    {
        InitializeComponent();
        
        // Set data context
        DataContext = new ComposeEmailViewModel();
        
        // Handle view model events
        if (DataContext is ComposeEmailViewModel viewModel)
        {
            viewModel.OnSent += () => Close(true);
            viewModel.OnCancel += () => Close(false);
        }
    }
    
    public ComposeEmailDialog(ComposeEmailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.OnSent += () => Close(true);
        viewModel.OnCancel += () => Close(false);
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}