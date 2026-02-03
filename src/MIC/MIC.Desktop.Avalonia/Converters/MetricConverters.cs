using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Converts progress percentage to width for progress bars.
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public static readonly ProgressToWidthConverter Instance = new();
    
    // Base width for 100% (matches parent container width minus padding)
    private const double MaxWidth = 180;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return Math.Max(0, Math.Min(MaxWidth, progress / 100.0 * MaxWidth));
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(double) && value is double width)
        {
            var clampedWidth = Math.Max(0, Math.Min(MaxWidth, width));
            return MaxWidth <= 0 ? 0d : Math.Round(clampedWidth / MaxWidth * 100.0, 2);
        }

        return BindingOperations.DoNothing;
    }
}
