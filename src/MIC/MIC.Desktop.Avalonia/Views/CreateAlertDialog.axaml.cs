using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class CreateAlertDialog : Window
{
    public CreateAlertDialog()
    {
        InitializeComponent();
        
        var viewModel = new CreateAlertViewModel();
        viewModel.OnCreated += () => Close(true);
        viewModel.OnCancel += () => Close(false);
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
