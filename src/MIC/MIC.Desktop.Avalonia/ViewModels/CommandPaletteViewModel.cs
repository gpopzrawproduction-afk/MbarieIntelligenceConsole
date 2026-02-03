using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Command Palette (Ctrl+K) for quick access to all application commands.
/// </summary>
public class CommandPaletteViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private bool _isOpen;
    private CommandItem? _selectedCommand;
    private int _selectedIndex;

    public CommandPaletteViewModel()
    {
        // Initialize all commands
        InitializeCommands();

        // Filter commands as user types
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => FilterCommands());

        // Commands
        ExecuteSelectedCommand = ReactiveCommand.Create(ExecuteSelected);
        CloseCommand = ReactiveCommand.Create(Close);
        MoveUpCommand = ReactiveCommand.Create(MoveUp);
        MoveDownCommand = ReactiveCommand.Create(MoveDown);
    }

    #region Properties

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            this.RaiseAndSetIfChanged(ref _isOpen, value);
            if (value)
            {
                SearchText = string.Empty;
                SelectedIndex = 0;
                FilterCommands();
            }
        }
    }

    public CommandItem? SelectedCommand
    {
        get => _selectedCommand;
        set => this.RaiseAndSetIfChanged(ref _selectedCommand, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= 0 && value < FilteredCommands.Count)
            {
                this.RaiseAndSetIfChanged(ref _selectedIndex, value);
                SelectedCommand = FilteredCommands[value];
            }
        }
    }

    public ObservableCollection<CommandItem> AllCommands { get; } = new();
    public ObservableCollection<CommandItem> FilteredCommands { get; } = new();

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> ExecuteSelectedCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }

    // Action to execute when a command is selected
    public Action<string>? OnNavigate { get; set; }
    public Action<string>? OnAction { get; set; }

    #endregion

    #region Methods

    private void InitializeCommands()
    {
        AllCommands.Clear();

        // Navigation Commands
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Go to Dashboard",
            Description = "View the main dashboard",
            Shortcut = "Ctrl+1",
            Icon = "mail",
            Action = () => OnNavigate?.Invoke("Dashboard")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Go to Alerts",
            Description = "View and manage alerts",
            Shortcut = "Ctrl+2",
            Icon = "bell",
            Action = () => OnNavigate?.Invoke("Alerts")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Go to Metrics",
            Description = "View business metrics",
            Shortcut = "Ctrl+3",
            Icon = "user",
            Action = () => OnNavigate?.Invoke("Metrics")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Go to Predictions",
            Description = "AI-powered predictions",
            Shortcut = "Ctrl+4",
            Icon = "archive",
            Action = () => OnNavigate?.Invoke("Predictions")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Open AI Chat",
            Description = "Chat with AI assistant",
            Shortcut = "Ctrl+5",
            Icon = "flag",
            Action = () => OnNavigate?.Invoke("AI Chat")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Navigation",
            Name = "Open Settings",
            Description = "Configure application settings",
            Shortcut = "Ctrl+,",
            Icon = "trash",
            Action = () => OnNavigate?.Invoke("Settings")
        });

        // Action Commands
        AllCommands.Add(new CommandItem
        {
            Category = "Actions",
            Name = "Create New Alert",
            Description = "Create a new alert manually",
            Shortcut = "Ctrl+N",
            Icon = "?",
            Action = () => OnAction?.Invoke("CreateAlert")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Actions",
            Name = "Refresh Data",
            Description = "Refresh all data from sources",
            Shortcut = "F5",
            Icon = "markread",
            Action = () => OnAction?.Invoke("Refresh")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Actions",
            Name = "Export Report",
            Description = "Export current view as PDF",
            Shortcut = "Ctrl+E",
            Icon = "mail",
            Action = () => OnAction?.Invoke("Export")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "Actions",
            Name = "Search Everywhere",
            Description = "Search across all data",
            Shortcut = "Ctrl+Shift+F",
            Icon = "bell",
            Action = () => OnAction?.Invoke("Search")
        });

        // AI Commands
        AllCommands.Add(new CommandItem
        {
            Category = "AI",
            Name = "Ask AI a Question",
            Description = "Open AI chat and ask anything",
            Icon = "user",
            Action = () => OnNavigate?.Invoke("AI Chat")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "AI",
            Name = "Generate Predictions",
            Description = "Run AI prediction analysis",
            Icon = "archive",
            Action = () => OnAction?.Invoke("GeneratePredictions")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "AI",
            Name = "Analyze Alerts",
            Description = "AI analysis of current alerts",
            Icon = "flag",
            Action = () => OnAction?.Invoke("AnalyzeAlerts")
        });

        // System Commands
        AllCommands.Add(new CommandItem
        {
            Category = "System",
            Name = "Toggle Dark Mode",
            Description = "Switch between light and dark theme",
            Shortcut = "Ctrl+T",
            Icon = "trash",
            Action = () => OnAction?.Invoke("ToggleTheme")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "System",
            Name = "Show Keyboard Shortcuts",
            Description = "View all keyboard shortcuts",
            Shortcut = "Ctrl+?",
            Icon = "markread",
            Action = () => OnAction?.Invoke("ShowShortcuts")
        });
        AllCommands.Add(new CommandItem
        {
            Category = "System",
            Name = "About",
            Description = "About this application",
            Icon = "??",
            Action = () => OnAction?.Invoke("About")
        });
    }

    private void FilterCommands()
    {
        FilteredCommands.Clear();

        var query = SearchText?.ToLowerInvariant() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? AllCommands
            : AllCommands.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var cmd in filtered.Take(10))
        {
            FilteredCommands.Add(cmd);
        }

        if (FilteredCommands.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    private void ExecuteSelected()
    {
        if (SelectedCommand != null)
        {
            var cmd = SelectedCommand;
            Close();
            cmd.Action?.Invoke();
        }
    }

    private void Close()
    {
        IsOpen = false;
        SearchText = string.Empty;
    }

    private void MoveUp()
    {
        if (SelectedIndex > 0)
        {
            SelectedIndex--;
        }
    }

    private void MoveDown()
    {
        if (SelectedIndex < FilteredCommands.Count - 1)
        {
            SelectedIndex++;
        }
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    #endregion
}

public class CommandItem
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Shortcut { get; init; } = string.Empty;
    public string Icon { get; init; } = "?";
    public Action? Action { get; init; }
}
