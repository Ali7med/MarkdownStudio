using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace MarkdownStudio.Services;

public enum AppTheme { Light, Dark, System }

/// <summary>قالب ثيم كامل: الأساس، اللون المميّز، نوع الخلفية، وتلوين اختياري للأسطح.</summary>
public sealed record ThemePreset(
    string Id,
    string NameKey,
    ApplicationTheme Base,
    string AccentHex,
    WindowBackdropType Backdrop,
    string? BackgroundHex = null,
    string? TextHex = null);

/// <summary>إدارة ثيمات التطبيق (قوالب جاهزة) مع خلفيات Mica/صلبة.</summary>
public interface IThemeService
{
    /// <summary>هل الثيم الفعّال داكن (يستخدمه مُصيّر المعاينة).</summary>
    bool IsDark { get; }

    /// <summary>القوالب المتاحة.</summary>
    IReadOnlyList<ThemePreset> Presets { get; }

    /// <summary>القالب الفعّال حالياً.</summary>
    ThemePreset CurrentPreset { get; }

    /// <summary>يُطلق عند تغيّر الثيم الفعّال.</summary>
    event EventHandler? ThemeChanged;

    /// <summary>يطبّق قالباً بالمعرّف (يتجاهل غير الموجود).</summary>
    void ApplyPreset(string id);
}
