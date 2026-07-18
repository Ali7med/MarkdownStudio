using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IThemeService"/>: قوالب ثيمات فوق Wpf.Ui مع تلوين أسطح اختياري.</summary>
public sealed class ThemeService : IThemeService
{
    // مفاتيح الفرش التي نتجاوزها لتلوين الأسطح؛ تُزال عند تبديل القالب.
    private readonly List<string> _overridden = new();

    public ThemeService()
    {
        Presets = new[]
        {
            new ThemePreset("fluent-dark", "theme.fluentDark",
                ApplicationTheme.Dark, "#4F8BFF", WindowBackdropType.None, "#1E1E24"),

            new ThemePreset("mica", "theme.mica",
                ApplicationTheme.Dark, "#4F8BFF", WindowBackdropType.Mica),

            new ThemePreset("warm-light", "theme.warmLight",
                ApplicationTheme.Light, "#4A6FA5", WindowBackdropType.None, "#FAF8F4", "#33302B"),

            new ThemePreset("bold-purple", "theme.boldPurple",
                ApplicationTheme.Dark, "#7C5CFF", WindowBackdropType.Mica, "#16161E", "#F0F0F6"),
        };
        CurrentPreset = Presets[0];
    }

    public bool IsDark { get; private set; }
    public IReadOnlyList<ThemePreset> Presets { get; }
    public ThemePreset CurrentPreset { get; private set; }
    public event EventHandler? ThemeChanged;

    public void ApplyPreset(string id)
    {
        var preset = Presets.FirstOrDefault(p => p.Id == id) ?? Presets[0];
        CurrentPreset = preset;

        ApplicationThemeManager.Apply(preset.Base, preset.Backdrop, updateAccent: false);
        ApplicationAccentColorManager.Apply(ParseColor(preset.AccentHex), preset.Base);

        ApplySurfaceOverrides(preset);

        IsDark = preset.Base == ApplicationTheme.Dark;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplySurfaceOverrides(ThemePreset preset)
    {
        var res = Application.Current.Resources;

        // أزل تجاوزات القالب السابق حتى تعود قيم الثيم الافتراضية.
        foreach (var key in _overridden) res.Remove(key);
        _overridden.Clear();

        void Override(string key, string? hex)
        {
            if (hex is null) return;
            res[key] = new SolidColorBrush(ParseColor(hex));
            _overridden.Add(key);
        }

        // خلفية التطبيق والمحرّر (مربوطة بـ DynamicResource فتتحدّث فوراً).
        Override("ApplicationBackgroundBrush", preset.BackgroundHex);
        Override("TextFillColorPrimaryBrush", preset.TextHex);
    }

    private static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
