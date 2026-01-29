using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MIC.Desktop.Avalonia.Controls;

/// <summary>
/// A holographic glass-morphic stat card control for the futuristic dashboard.
/// </summary>
public partial class StatCard : UserControl
{
    // Neon color constants
    private static readonly Color NeonCyan = Color.Parse("#00E5FF");
    private static readonly Color NeonMagenta = Color.Parse("#FF0055");
    private static readonly Color NeonLime = Color.Parse("#39FF14");
    private static readonly Color NeonPurple = Color.Parse("#BF40FF");

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Title), "METRIC");

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Value), "0");

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Subtitle), string.Empty);

    public static readonly StyledProperty<string> TrendProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Trend), string.Empty);

    public static readonly StyledProperty<bool> IsTrendPositiveProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(IsTrendPositive), true);

    public static readonly StyledProperty<string> AccentColorProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(AccentColor), "Cyan");

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<StatCard, Geometry?>(nameof(IconData));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Trend
    {
        get => GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    public bool IsTrendPositive
    {
        get => GetValue(IsTrendPositiveProperty);
        set => SetValue(IsTrendPositiveProperty, value);
    }

    public string AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public StatCard()
    {
        InitializeComponent();
        UpdateBindings();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty ||
            change.Property == ValueProperty ||
            change.Property == SubtitleProperty ||
            change.Property == TrendProperty ||
            change.Property == IsTrendPositiveProperty ||
            change.Property == AccentColorProperty ||
            change.Property == IconDataProperty)
        {
            UpdateBindings();
        }
    }

    private void UpdateBindings()
    {
        var accentColor = GetAccentColor();
        var glowColor = Color.FromArgb(48, accentColor.R, accentColor.G, accentColor.B);
        var bgColor = Color.FromArgb(21, accentColor.R, accentColor.G, accentColor.B);

        if (this.FindControl<Border>("CardBorder") is Border cardBorder)
        {
            cardBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(64, accentColor.R, accentColor.G, accentColor.B));
            cardBorder.Background = new SolidColorBrush(bgColor);
            cardBorder.BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 8,
                Blur = 32,
                Color = glowColor
            });
        }

        if (this.FindControl<Border>("IconContainer") is Border iconContainer)
        {
            iconContainer.Background = new SolidColorBrush(Color.FromArgb(32, accentColor.R, accentColor.G, accentColor.B));
        }

        if (this.FindControl<PathIcon>("CardIcon") is PathIcon cardIcon)
        {
            cardIcon.Foreground = new SolidColorBrush(accentColor);
            cardIcon.Data = IconData;
            cardIcon.IsVisible = IconData != null;
        }

        if (this.FindControl<TextBlock>("TitleText") is TextBlock titleText)
        {
            titleText.Text = Title.ToUpperInvariant();
        }

        if (this.FindControl<TextBlock>("ValueText") is TextBlock valueText)
        {
            valueText.Text = Value;
            valueText.Foreground = new SolidColorBrush(accentColor);
        }

        if (this.FindControl<TextBlock>("SubtitleText") is TextBlock subtitleText)
        {
            subtitleText.Text = Subtitle;
            subtitleText.IsVisible = !string.IsNullOrEmpty(Subtitle);
        }

        UpdateTrendBadge();
    }

    private void UpdateTrendBadge()
    {
        if (this.FindControl<Border>("TrendBadge") is not Border trendBadge ||
            this.FindControl<TextBlock>("TrendText") is not TextBlock trendText ||
            this.FindControl<PathIcon>("TrendIcon") is not PathIcon trendIcon)
            return;

        var hasTrend = !string.IsNullOrEmpty(Trend);
        trendBadge.IsVisible = hasTrend;

        if (!hasTrend) return;

        var trendColor = IsTrendPositive ? NeonLime : NeonMagenta;
        var trendBgColor = Color.FromArgb(32, trendColor.R, trendColor.G, trendColor.B);

        trendBadge.Background = new SolidColorBrush(trendBgColor);
        trendBadge.BorderBrush = new SolidColorBrush(trendColor);
        trendText.Text = Trend;
        trendText.Foreground = new SolidColorBrush(trendColor);
        trendIcon.Foreground = new SolidColorBrush(trendColor);

        // Arrow direction based on trend
        trendIcon.Data = IsTrendPositive
            ? Geometry.Parse("M7,15L12,10L17,15H7Z") // Up arrow
            : Geometry.Parse("M7,10L12,15L17,10H7Z"); // Down arrow
    }

    private Color GetAccentColor() => AccentColor?.ToLowerInvariant() switch
    {
        "cyan" => NeonCyan,
        "magenta" or "red" => NeonMagenta,
        "lime" or "green" => NeonLime,
        "purple" => NeonPurple,
        _ => NeonCyan
    };
}
