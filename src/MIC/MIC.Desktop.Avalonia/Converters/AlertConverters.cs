using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MIC.Core.Domain.Entities;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Converts AlertStatus to bool indicating if status is Active (for visibility).
/// </summary>
public class AlertStatusToActiveConverter : IValueConverter
{
    public static readonly AlertStatusToActiveConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlertStatus status)
        {
            return status == AlertStatus.Active;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(AlertStatus) && value is bool isActive)
        {
            return isActive ? AlertStatus.Active : AlertStatus.Resolved;
        }

        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts AlertStatus to color brush for UI display.
/// </summary>
public class AlertStatusToColorConverter : IValueConverter
{
    public static readonly AlertStatusToColorConverter Instance = new();

    private static readonly SolidColorBrush ActiveColor = new(Color.Parse("#FF0055"));
    private static readonly SolidColorBrush AcknowledgedColor = new(Color.Parse("#FF6B00"));
    private static readonly SolidColorBrush ResolvedColor = new(Color.Parse("#39FF14"));
    private static readonly SolidColorBrush EscalatedColor = new(Color.Parse("#BF40FF"));
    private static readonly SolidColorBrush DefaultColor = new(Color.Parse("#607D8B"));

    private static readonly SolidColorBrush ActiveBg = new(Color.Parse("#20FF0055"));
    private static readonly SolidColorBrush AcknowledgedBg = new(Color.Parse("#20FF6B00"));
    private static readonly SolidColorBrush ResolvedBg = new(Color.Parse("#2039FF14"));
    private static readonly SolidColorBrush EscalatedBg = new(Color.Parse("#20BF40FF"));
    private static readonly SolidColorBrush DefaultBg = new(Color.Parse("#20607D8B"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlertStatus status)
            return DefaultColor;

        var isBackground = parameter?.ToString()?.Equals("Background", StringComparison.OrdinalIgnoreCase) == true;

        return status switch
        {
            AlertStatus.Active => isBackground ? ActiveBg : ActiveColor,
            AlertStatus.Acknowledged => isBackground ? AcknowledgedBg : AcknowledgedColor,
            AlertStatus.Resolved => isBackground ? ResolvedBg : ResolvedColor,
            AlertStatus.Escalated => isBackground ? EscalatedBg : EscalatedColor,
            _ => isBackground ? DefaultBg : DefaultColor
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(AlertStatus) && value is SolidColorBrush brush)
        {
            if (brush.Color == ActiveColor.Color) return AlertStatus.Active;
            if (brush.Color == AcknowledgedColor.Color) return AlertStatus.Acknowledged;
            if (brush.Color == ResolvedColor.Color) return AlertStatus.Resolved;
            if (brush.Color == EscalatedColor.Color) return AlertStatus.Escalated;
        }

        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts AlertSeverity to color brush for UI display.
/// </summary>
public class AlertSeverityToColorConverter : IValueConverter
{
    public static readonly AlertSeverityToColorConverter Instance = new();

    private static readonly SolidColorBrush InfoColor = new(Color.Parse("#00E5FF"));
    private static readonly SolidColorBrush WarningColor = new(Color.Parse("#FF6B00"));
    private static readonly SolidColorBrush CriticalColor = new(Color.Parse("#FF0055"));
    private static readonly SolidColorBrush EmergencyColor = new(Color.Parse("#FF0055"));
    private static readonly SolidColorBrush DefaultColor = new(Color.Parse("#607D8B"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlertSeverity severity)
            return DefaultColor;

        return severity switch
        {
            AlertSeverity.Info => InfoColor,
            AlertSeverity.Warning => WarningColor,
            AlertSeverity.Critical => CriticalColor,
            AlertSeverity.Emergency => EmergencyColor,
            _ => DefaultColor
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(AlertSeverity) && value is SolidColorBrush brush)
        {
            if (brush.Color == InfoColor.Color) return AlertSeverity.Info;
            if (brush.Color == WarningColor.Color) return AlertSeverity.Warning;
            if (brush.Color == CriticalColor.Color) return AlertSeverity.Critical;
            if (brush.Color == EmergencyColor.Color) return AlertSeverity.Emergency;
        }

        return BindingOperations.DoNothing;
    }
}
