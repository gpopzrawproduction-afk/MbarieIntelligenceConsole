using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Global keyboard shortcuts manager for the application.
/// Provides consistent keyboard navigation throughout.
/// </summary>
public class KeyboardShortcutService
{
    private static KeyboardShortcutService? _instance;
    public static KeyboardShortcutService Instance => _instance ??= new KeyboardShortcutService();

    private readonly Dictionary<(Key, KeyModifiers), Action> _shortcuts = new();

    // Events for different shortcut categories
    public event Action? OnOpenCommandPalette;
    public event Action<string>? OnNavigate;
    public event Action? OnRefresh;
    public event Action? OnCreateNew;
    public event Action? OnExport;
    public event Action? OnToggleTheme;
    public event Action? OnSearch;
    public event Action? OnEscape;

    public KeyboardShortcutService()
    {
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        // Command Palette
        Register(Key.K, KeyModifiers.Control, () => OnOpenCommandPalette?.Invoke());
        Register(Key.P, KeyModifiers.Control | KeyModifiers.Shift, () => OnOpenCommandPalette?.Invoke());

        // Navigation
        Register(Key.D1, KeyModifiers.Control, () => OnNavigate?.Invoke("Dashboard"));
        Register(Key.D2, KeyModifiers.Control, () => OnNavigate?.Invoke("Alerts"));
        Register(Key.D3, KeyModifiers.Control, () => OnNavigate?.Invoke("Metrics"));
        Register(Key.D4, KeyModifiers.Control, () => OnNavigate?.Invoke("Predictions"));
        Register(Key.D5, KeyModifiers.Control, () => OnNavigate?.Invoke("AI Chat"));
        Register(Key.OemComma, KeyModifiers.Control, () => OnNavigate?.Invoke("Settings"));

        // Actions
        Register(Key.F5, KeyModifiers.None, () => OnRefresh?.Invoke());
        Register(Key.R, KeyModifiers.Control, () => OnRefresh?.Invoke());
        Register(Key.N, KeyModifiers.Control, () => OnCreateNew?.Invoke());
        Register(Key.E, KeyModifiers.Control, () => OnExport?.Invoke());
        Register(Key.T, KeyModifiers.Control, () => OnToggleTheme?.Invoke());
        Register(Key.F, KeyModifiers.Control, () => OnSearch?.Invoke());
        Register(Key.Escape, KeyModifiers.None, () => OnEscape?.Invoke());
    }

    public void Register(Key key, KeyModifiers modifiers, Action action)
    {
        _shortcuts[(key, modifiers)] = action;
    }

    public void Unregister(Key key, KeyModifiers modifiers)
    {
        _shortcuts.Remove((key, modifiers));
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        // Don't handle if typing in a text box (except for Escape)
        if (e.Source is TextBox && e.Key != Key.Escape)
        {
            return false;
        }

        var key = (e.Key, e.KeyModifiers);

        if (_shortcuts.TryGetValue(key, out var action))
        {
            action?.Invoke();
            e.Handled = true;
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Invokes navigation to a specific view.
    /// </summary>
    public void InvokeNavigate(string viewName)
    {
        OnNavigate?.Invoke(viewName);
    }

    /// <summary>
    /// Gets all shortcuts formatted for display.
    /// </summary>
    public List<ShortcutInfo> GetShortcutList()
    {
        return new List<ShortcutInfo>
        {
            new("Ctrl+K", "Open Command Palette", "Navigation"),
            new("Ctrl+1", "Go to Dashboard", "Navigation"),
            new("Ctrl+2", "Go to Alerts", "Navigation"),
            new("Ctrl+3", "Go to Metrics", "Navigation"),
            new("Ctrl+4", "Go to Predictions", "Navigation"),
            new("Ctrl+5", "Open AI Chat", "Navigation"),
            new("Ctrl+,", "Open Settings", "Navigation"),
            new("F5 / Ctrl+R", "Refresh Data", "Actions"),
            new("Ctrl+N", "Create New", "Actions"),
            new("Ctrl+E", "Export", "Actions"),
            new("Ctrl+F", "Search", "Actions"),
            new("Ctrl+T", "Toggle Theme", "Appearance"),
            new("Esc", "Close Dialog / Cancel", "General")
        };
    }
}

public record ShortcutInfo(string Shortcut, string Description, string Category);
