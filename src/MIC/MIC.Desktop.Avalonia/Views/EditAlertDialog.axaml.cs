using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MIC.Core.Application.Alerts.Common;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class EditAlertDialog : Window
{
    public EditAlertDialog()
    {
        InitializeComponent();
    }

    public EditAlertDialog(AlertDto alert) : this()
    {
        InitializeComponent();
        
        var viewModel = new EditAlertViewModel(alert.Id);
        viewModel.OnUpdated += () => Close(true);
        viewModel.OnCancel += () => Close(false);
        DataContext = viewModel;
    }

    public EditAlertDialog(Guid alertId) : this()
    {
        InitializeComponent();
        
        var viewModel = new EditAlertViewModel(alertId);
        viewModel.OnUpdated += () => Close(true);
        viewModel.OnCancel += () => Close(false);
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}