using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.Controls;

public partial class CommandPalette : UserControl
{
    public CommandPalette()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel vm)
        {
            vm.IsOpen = false;
        }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.IsOpen = false;
                e.Handled = true;
                break;
            case Key.Enter:
                vm.ExecuteSelectedCommand.Execute().Subscribe(_ => { });
                e.Handled = true;
                break;
            case Key.Up:
                vm.MoveUpCommand.Execute().Subscribe(_ => { });
                e.Handled = true;
                break;
            case Key.Down:
                vm.MoveDownCommand.Execute().Subscribe(_ => { });
                e.Handled = true;
                break;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Focus search box when palette opens
        if (DataContext is CommandPaletteViewModel vm)
        {
            vm.WhenAnyValue(x => x.IsOpen)
                .Where(isOpen => isOpen)
                .Subscribe(_ =>
                {
                    var searchBox = this.FindControl<TextBox>("SearchBox");
                    searchBox?.Focus();
                });
        }
    }
}
