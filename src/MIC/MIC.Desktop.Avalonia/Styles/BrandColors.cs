using Avalonia.Media;

namespace MIC.Desktop.Avalonia.Styles;

/// <summary>
/// Mbarie Intelligence Console brand colors.
/// Use these constants for consistent branding across all views.
/// </summary>
/// <remarks>
/// Color palette inspired by cyberpunk/holographic aesthetic with
/// deep space backgrounds and neon accents.
/// </remarks>
public static class BrandColors
{
    #region Primary Colors

    /// <summary>
    /// Primary brand color - Deep space blue.
    /// Use for primary backgrounds and main UI elements.
    /// </summary>
    public const string Primary = "#1a237e";
    public static readonly Color PrimaryColor = Color.Parse(Primary);
    public static readonly SolidColorBrush PrimaryBrush = new(PrimaryColor);

    /// <summary>
    /// Primary dark variant - Near black.
    /// Use for main application background.
    /// </summary>
    public const string PrimaryDark = "#0B0C10";
    public static readonly Color PrimaryDarkColor = Color.Parse(PrimaryDark);
    public static readonly SolidColorBrush PrimaryDarkBrush = new(PrimaryDarkColor);

    /// <summary>
    /// Primary light variant - Elevated surface.
    /// Use for cards and elevated surfaces.
    /// </summary>
    public const string PrimaryLight = "#0D1117";
    public static readonly Color PrimaryLightColor = Color.Parse(PrimaryLight);
    public static readonly SolidColorBrush PrimaryLightBrush = new(PrimaryLightColor);

    #endregion

    #region Accent Colors

    /// <summary>
    /// Cyan neon accent - Primary accent color.
    /// Use for interactive elements, highlights, and CTAs.
    /// </summary>
    public const string AccentCyan = "#00E5FF";
    public static readonly Color AccentCyanColor = Color.Parse(AccentCyan);
    public static readonly SolidColorBrush AccentCyanBrush = new(AccentCyanColor);

    /// <summary>
    /// Gold accent - Secondary accent color.
    /// Use for premium features, warnings, and important highlights.
    /// </summary>
    public const string AccentGold = "#FFC107";
    public static readonly Color AccentGoldColor = Color.Parse(AccentGold);
    public static readonly SolidColorBrush AccentGoldBrush = new(AccentGoldColor);

    /// <summary>
    /// Magenta accent - Tertiary accent.
    /// Use for predictions and AI-related features.
    /// </summary>
    public const string AccentMagenta = "#BF40FF";
    public static readonly Color AccentMagentaColor = Color.Parse(AccentMagenta);
    public static readonly SolidColorBrush AccentMagentaBrush = new(AccentMagentaColor);

    /// <summary>
    /// Green neon - Success state.
    /// Use for positive indicators, success messages, on-target metrics.
    /// </summary>
    public const string AccentGreen = "#39FF14";
    public static readonly Color AccentGreenColor = Color.Parse(AccentGreen);
    public static readonly SolidColorBrush AccentGreenBrush = new(AccentGreenColor);

    #endregion

    #region Semantic Colors

    /// <summary>
    /// Success color - Operation completed successfully.
    /// </summary>
    public const string Success = "#39FF14";
    public static readonly Color SuccessColor = Color.Parse(Success);
    public static readonly SolidColorBrush SuccessBrush = new(SuccessColor);

    /// <summary>
    /// Warning color - Attention required.
    /// </summary>
    public const string Warning = "#FF6B00";
    public static readonly Color WarningColor = Color.Parse(Warning);
    public static readonly SolidColorBrush WarningBrush = new(WarningColor);

    /// <summary>
    /// Error/Critical color - Immediate action required.
    /// </summary>
    public const string Error = "#FF0055";
    public static readonly Color ErrorColor = Color.Parse(Error);
    public static readonly SolidColorBrush ErrorBrush = new(ErrorColor);

    /// <summary>
    /// Info color - Informational messages.
    /// </summary>
    public const string Info = "#00E5FF";
    public static readonly Color InfoColor = Color.Parse(Info);
    public static readonly SolidColorBrush InfoBrush = new(InfoColor);

    #endregion

    #region Text Colors

    /// <summary>
    /// Primary text color - High emphasis.
    /// Use for headings and important text.
    /// </summary>
    public const string TextPrimary = "#FFFFFF";
    public static readonly Color TextPrimaryColor = Color.Parse(TextPrimary);
    public static readonly SolidColorBrush TextPrimaryBrush = new(TextPrimaryColor);

    /// <summary>
    /// Secondary text color - Medium emphasis.
    /// Use for body text and descriptions.
    /// </summary>
    public const string TextSecondary = "#B0BEC5";
    public static readonly Color TextSecondaryColor = Color.Parse(TextSecondary);
    public static readonly SolidColorBrush TextSecondaryBrush = new(TextSecondaryColor);

    /// <summary>
    /// Tertiary text color - Low emphasis.
    /// Use for hints, labels, and disabled text.
    /// </summary>
    public const string TextTertiary = "#607D8B";
    public static readonly Color TextTertiaryColor = Color.Parse(TextTertiary);
    public static readonly SolidColorBrush TextTertiaryBrush = new(TextTertiaryColor);

    /// <summary>
    /// Disabled text color.
    /// </summary>
    public const string TextDisabled = "#455A64";
    public static readonly Color TextDisabledColor = Color.Parse(TextDisabled);
    public static readonly SolidColorBrush TextDisabledBrush = new(TextDisabledColor);

    #endregion

    #region Surface Colors

    /// <summary>
    /// Surface color - Cards and elevated elements.
    /// </summary>
    public const string Surface = "#14FFFFFF";
    
    /// <summary>
    /// Surface border color.
    /// </summary>
    public const string SurfaceBorder = "#20FFFFFF";

    /// <summary>
    /// Divider color.
    /// </summary>
    public const string Divider = "#15FFFFFF";

    #endregion

    #region Glow Effects

    /// <summary>
    /// Cyan glow for box shadows (40% opacity).
    /// </summary>
    public const string GlowCyan = "#4000E5FF";

    /// <summary>
    /// Green glow for success states.
    /// </summary>
    public const string GlowGreen = "#4039FF14";

    /// <summary>
    /// Red glow for error/critical states.
    /// </summary>
    public const string GlowRed = "#40FF0055";

    /// <summary>
    /// Gold glow for warnings.
    /// </summary>
    public const string GlowGold = "#40FFC107";

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the appropriate color for an alert severity level.
    /// </summary>
    public static string GetSeverityColor(string severity) => severity?.ToLowerInvariant() switch
    {
        "critical" => Error,
        "high" => Error,
        "warning" => Warning,
        "medium" => Warning,
        "info" => Info,
        "low" => Info,
        _ => TextSecondary
    };

    /// <summary>
    /// Gets the appropriate glow color for an alert severity level.
    /// </summary>
    public static string GetSeverityGlow(string severity) => severity?.ToLowerInvariant() switch
    {
        "critical" or "high" => GlowRed,
        "warning" or "medium" => GlowGold,
        "info" or "low" => GlowCyan,
        _ => "#00000000"
    };

    #endregion
}
