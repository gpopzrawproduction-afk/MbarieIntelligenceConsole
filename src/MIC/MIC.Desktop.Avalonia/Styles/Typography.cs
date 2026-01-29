namespace MIC.Desktop.Avalonia.Styles;

/// <summary>
/// Mbarie Intelligence Console typography definitions.
/// Use these constants for consistent text styling across all views.
/// </summary>
/// <remarks>
/// Typography follows a modular scale for visual hierarchy.
/// All sizes are in device-independent pixels.
/// </remarks>
public static class Typography
{
    #region Font Families

    /// <summary>
    /// Primary font family for UI text.
    /// Uses system default sans-serif for optimal rendering.
    /// </summary>
    public const string FontFamilyPrimary = "Segoe UI, San Francisco, Helvetica Neue, Arial, sans-serif";

    /// <summary>
    /// Monospace font family for code and data.
    /// </summary>
    public const string FontFamilyMono = "Cascadia Code, Consolas, Monaco, monospace";

    #endregion

    #region Font Sizes (Modular Scale)

    /// <summary>
    /// Display - Hero text, splash screen (48px).
    /// </summary>
    public const double FontSizeDisplay = 48;

    /// <summary>
    /// H1 - Page titles (32px).
    /// </summary>
    public const double FontSizeH1 = 32;

    /// <summary>
    /// H2 - Section headers (24px).
    /// </summary>
    public const double FontSizeH2 = 24;

    /// <summary>
    /// H3 - Card titles (20px).
    /// </summary>
    public const double FontSizeH3 = 20;

    /// <summary>
    /// H4 - Subsection headers (18px).
    /// </summary>
    public const double FontSizeH4 = 18;

    /// <summary>
    /// Body Large - Emphasized body text (16px).
    /// </summary>
    public const double FontSizeBodyLarge = 16;

    /// <summary>
    /// Body - Standard body text (14px).
    /// </summary>
    public const double FontSizeBody = 14;

    /// <summary>
    /// Body Small - Secondary text (13px).
    /// </summary>
    public const double FontSizeBodySmall = 13;

    /// <summary>
    /// Caption - Labels, hints (12px).
    /// </summary>
    public const double FontSizeCaption = 12;

    /// <summary>
    /// Overline - Section labels, badges (11px).
    /// </summary>
    public const double FontSizeOverline = 11;

    /// <summary>
    /// Tiny - Badges, micro text (10px).
    /// </summary>
    public const double FontSizeTiny = 10;

    #endregion

    #region Font Weights

    /// <summary>
    /// Thin weight (100) - Display text, large numbers.
    /// </summary>
    public const int FontWeightThin = 100;

    /// <summary>
    /// Light weight (300) - Subheadings, secondary titles.
    /// </summary>
    public const int FontWeightLight = 300;

    /// <summary>
    /// Regular weight (400) - Body text.
    /// </summary>
    public const int FontWeightRegular = 400;

    /// <summary>
    /// Medium weight (500) - Emphasized text, buttons.
    /// </summary>
    public const int FontWeightMedium = 500;

    /// <summary>
    /// SemiBold weight (600) - Section headers, labels.
    /// </summary>
    public const int FontWeightSemiBold = 600;

    /// <summary>
    /// Bold weight (700) - Important text, alerts.
    /// </summary>
    public const int FontWeightBold = 700;

    #endregion

    #region Letter Spacing

    /// <summary>
    /// Tight spacing for large text (-0.5px).
    /// </summary>
    public const double LetterSpacingTight = -0.5;

    /// <summary>
    /// Normal spacing for body text (0).
    /// </summary>
    public const double LetterSpacingNormal = 0;

    /// <summary>
    /// Wide spacing for labels (1px).
    /// </summary>
    public const double LetterSpacingWide = 1;

    /// <summary>
    /// Extra wide spacing for section titles (2px).
    /// </summary>
    public const double LetterSpacingExtraWide = 2;

    /// <summary>
    /// Ultra wide spacing for brand text (4-6px).
    /// </summary>
    public const double LetterSpacingBrand = 6;

    #endregion

    #region Line Heights

    /// <summary>
    /// Tight line height for compact layouts (1.2).
    /// </summary>
    public const double LineHeightTight = 1.2;

    /// <summary>
    /// Normal line height for body text (1.5).
    /// </summary>
    public const double LineHeightNormal = 1.5;

    /// <summary>
    /// Relaxed line height for readability (1.75).
    /// </summary>
    public const double LineHeightRelaxed = 1.75;

    #endregion
}

/// <summary>
/// Consistent spacing values for the application.
/// Based on an 8px grid system.
/// </summary>
public static class Spacing
{
    #region Base Unit

    /// <summary>
    /// Base spacing unit (4px).
    /// </summary>
    public const double Unit = 4;

    #endregion

    #region Common Spacing Values

    /// <summary>
    /// Extra small spacing (4px).
    /// </summary>
    public const double XS = 4;

    /// <summary>
    /// Small spacing (8px).
    /// </summary>
    public const double SM = 8;

    /// <summary>
    /// Medium spacing (12px).
    /// </summary>
    public const double MD = 12;

    /// <summary>
    /// Large spacing (16px).
    /// </summary>
    public const double LG = 16;

    /// <summary>
    /// Extra large spacing (20px).
    /// </summary>
    public const double XL = 20;

    /// <summary>
    /// 2X large spacing (24px).
    /// </summary>
    public const double XXL = 24;

    /// <summary>
    /// 3X large spacing (32px).
    /// </summary>
    public const double XXXL = 32;

    #endregion

    #region Component-Specific Spacing

    /// <summary>
    /// Page margin (32px).
    /// </summary>
    public const double PageMargin = 32;

    /// <summary>
    /// Card padding (20px).
    /// </summary>
    public const double CardPadding = 20;

    /// <summary>
    /// Card gap between cards (16px).
    /// </summary>
    public const double CardGap = 16;

    /// <summary>
    /// Section spacing between major sections (24px).
    /// </summary>
    public const double SectionSpacing = 24;

    /// <summary>
    /// Button padding horizontal (16px).
    /// </summary>
    public const double ButtonPaddingX = 16;

    /// <summary>
    /// Button padding vertical (10px).
    /// </summary>
    public const double ButtonPaddingY = 10;

    /// <summary>
    /// Input padding (12px).
    /// </summary>
    public const double InputPadding = 12;

    /// <summary>
    /// Icon margin in buttons (8px).
    /// </summary>
    public const double IconMargin = 8;

    /// <summary>
    /// Sidebar width (260px).
    /// </summary>
    public const double SidebarWidth = 260;

    /// <summary>
    /// Header height (60px).
    /// </summary>
    public const double HeaderHeight = 60;

    /// <summary>
    /// Footer/Status bar height (36px).
    /// </summary>
    public const double FooterHeight = 36;

    #endregion

    #region Border Radius

    /// <summary>
    /// Small radius for inputs, badges (4px).
    /// </summary>
    public const double RadiusSM = 4;

    /// <summary>
    /// Medium radius for buttons, cards (8px).
    /// </summary>
    public const double RadiusMD = 8;

    /// <summary>
    /// Large radius for modals, panels (12px).
    /// </summary>
    public const double RadiusLG = 12;

    /// <summary>
    /// Extra large radius for pills (24px).
    /// </summary>
    public const double RadiusXL = 24;

    /// <summary>
    /// Circle radius (50%).
    /// </summary>
    public const double RadiusCircle = 9999;

    #endregion
}

/// <summary>
/// Animation timing and easing constants.
/// </summary>
public static class Animations
{
    #region Durations (milliseconds)

    /// <summary>
    /// Instant - No perceptible delay (50ms).
    /// </summary>
    public const int DurationInstant = 50;

    /// <summary>
    /// Fast - Quick micro-interactions (150ms).
    /// </summary>
    public const int DurationFast = 150;

    /// <summary>
    /// Normal - Standard transitions (250ms).
    /// </summary>
    public const int DurationNormal = 250;

    /// <summary>
    /// Slow - Deliberate animations (400ms).
    /// </summary>
    public const int DurationSlow = 400;

    /// <summary>
    /// Splash - Splash screen display (2000ms).
    /// </summary>
    public const int DurationSplash = 2000;

    #endregion

    #region Easing Functions

    /// <summary>
    /// Ease out - Deceleration curve for entering elements.
    /// </summary>
    public const string EaseOut = "0.0, 0.0, 0.2, 1.0";

    /// <summary>
    /// Ease in - Acceleration curve for exiting elements.
    /// </summary>
    public const string EaseIn = "0.4, 0.0, 1.0, 1.0";

    /// <summary>
    /// Ease in-out - Standard curve for most animations.
    /// </summary>
    public const string EaseInOut = "0.4, 0.0, 0.2, 1.0";

    /// <summary>
    /// Bounce - Playful overshoot effect.
    /// </summary>
    public const string Bounce = "0.68, -0.55, 0.265, 1.55";

    #endregion
}
