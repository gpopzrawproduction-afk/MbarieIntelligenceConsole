using System;
using System.Globalization;
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
    /// Usage: Converter={x:Static BoolConverters.ToClassName}, ConverterParameter='active-class|inactive-class'
    /// </summary>
    public static new readonly IValueConverter ToString = new BoolToStringConverter();

    /// <summary>
    /// Converts boolean to FontWeight. True = Normal (read), False = Bold (unread).
    /// </summary>
    public static readonly IValueConverter ToFontWeight = new BoolToFontWeightConverter();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
