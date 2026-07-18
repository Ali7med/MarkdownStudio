using System.IO;
using System.Text.Json;

namespace MarkdownStudio.Services;

/// <summary>إعدادات المستخدم المستمرّة.</summary>
public sealed class AppSettings
{
    /// <summary>آخر إصدار عُرِضت له لوحة «ما الجديد».</summary>
    public string? LastShownWhatsNewVersion { get; set; }

    /// <summary>رمز اللغة المختارة (مثل ar / en). null = لغة النظام أو الافتراضية.</summary>
    public string? LanguageCode { get; set; }

    /// <summary>معرّف قالب الثيم المختار. null = الافتراضي.</summary>
    public string? ThemeId { get; set; }
}

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
}

/// <summary>تطبيق <see cref="ISettingsService"/> يحفظ JSON في %AppData%\MarkdownStudio.</summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarkdownStudio");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Settings = Load();
    }

    public AppSettings Settings { get; }

    public void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(Settings, Options)); }
        catch { /* تعذّر الحفظ: تجاهل */ }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch { /* ملف تالف */ }
        return new AppSettings();
    }
}
