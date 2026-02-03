using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class AlertListView : UserControl
{
    private AlertListViewModel? _viewModel;
    
    public AlertListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CreateAlertRequested -= OnCreateAlertRequested;
            _viewModel.EditAlertRequested -= OnEditAlertRequested;
            _viewModel.ViewDetailsRequested -= OnViewDetailsRequested;
        }

        _viewModel = DataContext as AlertListViewModel;
        
        if (_viewModel != null)
        {
            _viewModel.CreateAlertRequested += OnCreateAlertRequested;
            _viewModel.EditAlertRequested += OnEditAlertRequested;
            _viewModel.ViewDetailsRequested += OnViewDetailsRequested;
        }
    }

    private async void OnCreateAlertRequested(object? sender, EventArgs e)
    {
        var dialog = new CreateAlertDialog();
        var result = await dialog.ShowDialog<bool?>(GetWindow());
        
        if (result == true && _viewModel != null)
        {
            await _viewModel.RefreshCommand.Execute().FirstAsync(); // Refresh the list
        }
    }

    private async void OnEditAlertRequested(object? sender, MIC.Core.Application.Alerts.Common.AlertDto alert)
    {
        var dialog = new EditAlertDialog(alert);
        var result = await dialog.ShowDialog<bool?>(GetWindow());
        
        if (result == true && _viewModel != null)
        {
            await _viewModel.RefreshCommand.Execute().FirstAsync(); // Refresh the list
        }
    }

    private async void OnViewDetailsRequested(object? sender, MIC.Core.Application.Alerts.Common.AlertDto alert)
    {
        if (_viewModel == null) return;

        _viewModel.SelectedAlert = alert;

        var viewModel = Program.ServiceProvider?.GetService<AlertDetailsViewModel>();
        if (viewModel == null)
        {
            return;
        }

        viewModel.LoadFromDto(alert);

        var window = new Window
        {
            Title = "Alert Details",
            Width = 900,
            Height = 700,
            Content = new AlertDetailsView { DataContext = viewModel }
        };

        await window.ShowDialog(GetWindow());
    }

    private Window GetWindow()
    {
        return TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("Cannot find parent window");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}