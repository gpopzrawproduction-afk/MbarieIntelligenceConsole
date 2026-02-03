using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MIC.Desktop.Avalonia.Converters
{
    /// <summary>
    /// Converts a boolean value to navigation item class names
    /// True = "nav-item-active", False = "nav-item"
    /// </summary>
    public class BoolToNavItemClassConverter : IValueConverter
    {
        public static readonly BoolToNavItemClassConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "nav-item-active" : "nav-item";
            }
            return "nav-item";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string className)
            {
                return className == "nav-item-active";
            }
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Converts a boolean value to a foreground color for navigation items
    /// True = "#00E5FF" (cyan), False = "#607D8B" (gray)
    /// </summary>
    public class BoolToNavForegroundConverter : IValueConverter
    {
        public static readonly BoolToNavForegroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "#00E5FF" : "#607D8B";
            }
            return "#607D8B";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Converts a boolean value to path icon foreground color
    /// True = active color, False = inactive color
    /// </summary>
    public class BoolToPathIconColorConverter : IValueConverter
    {
        public static readonly BoolToPathIconColorConverter Instance = new();

        private static readonly string ActiveColor = "#00E5FF";
        private static readonly string InactiveColor = "#607D8B";

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? ActiveColor : InactiveColor;
            }
            
            // Handle special cases for specific navigation items
            if (parameter is string iconType)
            {
                // For AI and Email icons, they have special colors even when not active
                if (iconType == "ai")
                    return "#BF40FF"; // Purple
                if (iconType == "email")
                    return "#00E5FF"; // Cyan
            }
            
            return InactiveColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }
}