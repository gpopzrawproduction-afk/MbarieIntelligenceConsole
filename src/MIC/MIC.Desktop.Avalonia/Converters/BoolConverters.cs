using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MIC.Desktop.Avalonia.Converters;

/// <summary>
/// Converts boolean values to various output types for UI binding.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// Converts true to green brush (connected), false to red brush (disconnected).
    /// </summary>
    public static readonly IValueConverter ToGreen = new BoolToGreenConverter();

    /// <summary>
    /// Converts boolean to string class name for styling.
    /// </summary>
    public static new readonly IValueConverter ToString = new BoolToStringConverter();

    /// <summary>
    /// Converts boolean to FontWeight. True = Normal (read), False = Bold (unread).
    /// </summary>
    public static readonly IValueConverter ToFontWeight = new BoolToFontWeightConverter();

    /// <summary>
    /// Converts boolean to refresh icon. True = ?? (pause), False = ?? (play).
    /// </summary>
    public static readonly IValueConverter ToRefreshIcon = new BoolToRefreshIconConverter();

    /// <summary>
    /// Converts boolean to refresh status color. True = Green, False = Gray.
    /// </summary>
    public static readonly IValueConverter ToRefreshColor = new BoolToRefreshColorConverter();
}

/// <summary>
/// Converts an integer (count) to bool indicating whether value is zero.
/// Useful for showing empty-state UI when collection count == 0.
/// </summary>
public class ZeroToBoolConverter : IValueConverter
{
    public static readonly IValueConverter Instance = new ZeroToBoolConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return i == 0;
        }
        if (value is long l)
        {
            return l == 0L;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class BoolToGreenConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.Parse("#43a047"));
    private static readonly SolidColorBrush RedBrush = new(Color.Parse("#e53935"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? GreenBrush : RedBrush;
        }
        return RedBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(bool) && value is SolidColorBrush brush)
        {
            if (brush.Color == GreenBrush.Color) return true;
            if (brush.Color == RedBrush.Color) return false;
        }

        return BindingOperations.DoNothing;
    }
}

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string classNames)
        {
            var parts = classNames.Split('|');
            if (parts.Length == 2)
            {
                return b ? parts[0] : parts[1];
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(bool) && value is string className && parameter is string classNames)
        {
            var parts = classNames.Split('|');
            if (parts.Length == 2)
            {
                if (string.Equals(className, parts[0], StringComparison.Ordinal)) return true;
                if (string.Equals(className, parts[1], StringComparison.Ordinal)) return false;
            }
        }

        return BindingOperations.DoNothing;
    }
}

public class BoolToFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRead)
        {
            // If read, use Normal weight. If unread, use Bold.
            return isRead ? FontWeight.Normal : FontWeight.Bold;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(bool) && value is FontWeight weight)
        {
            return weight == FontWeight.Normal;
        }

        return BindingOperations.DoNothing;
    }
}

public class BoolToRefreshIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled)
        {
            // If auto-refresh is enabled, show pause icon. If disabled, show play icon.
            return enabled ? "??" : "??";
        }
        return "??";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class BoolToRefreshColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled)
        {
            // If auto-refresh is enabled, show green. If disabled, show gray.
            return enabled ? Color.Parse("#10B981") : Color.Parse("#6B7280");
        }
        return Color.Parse("#6B7280");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

