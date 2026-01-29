using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Global toast notification service for user feedback.
/// </summary>
public interface INotificationService
{
    ObservableCollection<ToastNotification> Notifications { get; }
    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
    void Dismiss(ToastNotification notification);
    void DismissAll();
}

public class NotificationService : INotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    public ObservableCollection<ToastNotification> Notifications { get; } = new();

    public void ShowSuccess(string message, string? title = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Success,
            Title = title ?? "Success",
            Message = message,
            Icon = "?"
        });
    }

    public void ShowError(string message, string? title = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Error,
            Title = title ?? "Error",
            Message = message,
            Icon = "?",
            Duration = TimeSpan.FromSeconds(8) // Errors stay longer
        });
    }

    public void ShowWarning(string message, string? title = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Warning,
            Title = title ?? "Warning",
            Message = message,
            Icon = "?"
        });
    }

    public void ShowInfo(string message, string? title = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Info,
            Title = title ?? "Info",
            Message = message,
            Icon = "?"
        });
    }

    public void Dismiss(ToastNotification notification)
    {
        if (Notifications.Contains(notification))
        {
            Notifications.Remove(notification);
        }
    }

    public void DismissAll()
    {
        Notifications.Clear();
    }

    private void Show(ToastNotification notification)
    {
        // Add to collection (on UI thread if needed)
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, notification);

            // Auto-dismiss after duration
            if (notification.Duration > TimeSpan.Zero)
            {
                Observable.Timer(notification.Duration)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => Dismiss(notification));
            }

            // Limit max visible notifications
            while (Notifications.Count > 5)
            {
                Notifications.RemoveAt(Notifications.Count - 1);
            }
        });
    }
}

public class ToastNotification : ReactiveObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Icon { get; init; } = "?";
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(4);
    public DateTime CreatedAt { get; } = DateTime.Now;

    public string BackgroundColor => Type switch
    {
        ToastType.Success => "#20339900",
        ToastType.Error => "#30FF0055",
        ToastType.Warning => "#30FF9500",
        ToastType.Info => "#2000E5FF",
        _ => "#20FFFFFF"
    };

    public string BorderColor => Type switch
    {
        ToastType.Success => "#339900",
        ToastType.Error => "#FF0055",
        ToastType.Warning => "#FF9500",
        ToastType.Info => "#00E5FF",
        _ => "#FFFFFF"
    };

    public string IconColor => Type switch
    {
        ToastType.Success => "#39FF14",
        ToastType.Error => "#FF0055",
        ToastType.Warning => "#FF9500",
        ToastType.Info => "#00E5FF",
        _ => "#FFFFFF"
    };
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
