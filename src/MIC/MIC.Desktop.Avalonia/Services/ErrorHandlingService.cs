using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Global error handling service with user-friendly error display and recovery options.
/// </summary>
public class ErrorHandlingService
{
    private static ErrorHandlingService? _instance;
    public static ErrorHandlingService Instance => _instance ??= new ErrorHandlingService();

    private readonly NotificationService _notifications = NotificationService.Instance;

    public event Action<ErrorInfo>? OnError;
    public event Action<ErrorInfo>? OnCriticalError;

    /// <summary>
    /// Handles an exception with appropriate user feedback.
    /// </summary>
    public void HandleException(Exception ex, string? context = null, bool isCritical = false)
    {
        var errorInfo = new ErrorInfo
        {
            Exception = ex,
            Context = context ?? "An unexpected error occurred",
            IsCritical = isCritical,
            Timestamp = DateTime.Now
        };

        // Log the error
        LogError(errorInfo);

        // Show notification on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (isCritical)
            {
                _notifications.ShowError(
                    GetUserFriendlyMessage(ex),
                    "Critical Error");
                OnCriticalError?.Invoke(errorInfo);
            }
            else
            {
                _notifications.ShowError(
                    GetUserFriendlyMessage(ex),
                    context ?? "Error");
                OnError?.Invoke(errorInfo);
            }
        });
    }

    /// <summary>
    /// Wraps an async operation with error handling.
    /// </summary>
    public async Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> operation,
        string context,
        T? defaultValue = default)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            // Don't show error for cancelled operations
            return defaultValue;
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return defaultValue;
        }
    }

    /// <summary>
    /// Wraps an async operation with error handling (no return value).
    /// </summary>
    public async Task SafeExecuteAsync(Func<Task> operation, string context)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Don't show error for cancelled operations
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
        }
    }

    /// <summary>
    /// Wraps a synchronous operation with error handling.
    /// </summary>
    public T? SafeExecute<T>(Func<T> operation, string context, T? defaultValue = default)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            HandleException(ex, context);
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a user-friendly error message from an exception.
    /// </summary>
    private static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            // Network errors
            System.Net.Http.HttpRequestException => 
                "Unable to connect to the server. Please check your internet connection.",
            
            // Database errors
            Microsoft.Data.Sqlite.SqliteException sqliteEx => 
                $"Database error: {GetDatabaseErrorMessage(sqliteEx)}",
            
            // AI/OpenAI errors
            Exception e when e.Message.Contains("API key") => 
                "Invalid API key. Please check your settings.",
            Exception e when e.Message.Contains("rate limit") => 
                "Too many requests. Please wait a moment and try again.",
            Exception e when e.Message.Contains("timeout") => 
                "The operation timed out. Please try again.",
            
            // Validation errors
            FluentValidation.ValidationException validationEx => 
                string.Join(", ", validationEx.Errors),
            ArgumentException argEx => 
                argEx.Message,
            
            // File errors
            System.IO.IOException ioEx => 
                $"File operation failed: {ioEx.Message}",
            UnauthorizedAccessException => 
                "Access denied. Please check your permissions.",
            
            // Generic
            _ => ex.Message.Length > 200 
                ? ex.Message[..200] + "..." 
                : ex.Message
        };
    }

    private static string GetDatabaseErrorMessage(Microsoft.Data.Sqlite.SqliteException ex)
    {
        return ex.SqliteErrorCode switch
        {
            1 => "Database query error",
            5 => "Database is locked. Please try again.",
            14 => "Unable to open database file",
            19 => "Data constraint violation",
            _ => "Database operation failed"
        };
    }

    private void LogError(ErrorInfo error)
    {
        Log.Error(error.Exception, "{Context} (Critical: {IsCritical})", error.Context, error.IsCritical);
    }
}

public class ErrorInfo
{
    public Exception Exception { get; init; } = null!;
    public string Context { get; init; } = string.Empty;
    public bool IsCritical { get; init; }
    public DateTime Timestamp { get; init; }
}
