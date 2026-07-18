using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace MarkdownStudio.Localization;

/// <summary>معلومات لغة واحدة (من ملف ترجمة).</summary>
public sealed class LanguageInfo
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Direction { get; init; }   // "rtl" أو "ltr"
    public required IReadOnlyDictionary<string, string> Strings { get; init; }

    public FlowDirection FlowDirection =>
        Direction.Equals("rtl", StringComparison.OrdinalIgnoreCase)
            ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

/// <summary>
/// محرّك التوطين: يكتشف ملفات اللغة (مضمّنة + مجلد lang بجانب التنفيذي)،
/// ويوفّر بحث نصوص + اتجاه + تبديل لغة حيّ عبر الربط.
/// لإضافة لغة: ترجم ملف JSON وأسقطه في مجلد lang بجانب البرنامج — يظهر تلقائياً.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    public static Localizer Instance { get; } = new();

    private readonly Dictionary<string, LanguageInfo> _languages = new(StringComparer.OrdinalIgnoreCase);
    private LanguageInfo _current;

    private Localizer()
    {
        LoadAll();
        _current = _languages.GetValueOrDefault("ar")
                   ?? _languages.Values.FirstOrDefault()
                   ?? Fallback();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public IReadOnlyList<LanguageInfo> Available =>
        _languages.Values.OrderBy(l => l.Name, StringComparer.CurrentCulture).ToList();

    public LanguageInfo Current => _current;
    public string CurrentCode => _current.Code;
    public FlowDirection FlowDirection => _current.FlowDirection;

    /// <summary>بحث نصّي؛ يعيد المفتاح نفسه إن لم يوجد (يسهّل اكتشاف النقص).</summary>
    public string this[string key] => _current.Strings.GetValueOrDefault(key, key);

    public string Format(string key, params object[] args) => string.Format(this[key], args);

    public bool SetLanguage(string? code)
    {
        if (code is null || !_languages.TryGetValue(code, out var lang)) return false;
        if (ReferenceEquals(lang, _current)) return true;

        _current = lang;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));   // يحدّث كل روابط النصوص
        OnChanged(nameof(FlowDirection));
        OnChanged(nameof(CurrentCode));
        OnChanged(nameof(Current));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ===== التحميل =====

    private void LoadAll()
    {
        // 1) الملفات المضمّنة (MarkdownStudio.lang.<code>.json)
        var asm = Assembly.GetExecutingAssembly();
        foreach (var res in asm.GetManifestResourceNames())
        {
            if (!res.Contains(".lang.", StringComparison.OrdinalIgnoreCase) ||
                !res.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                using var stream = asm.GetManifestResourceStream(res)!;
                using var reader = new StreamReader(stream);
                AddFromJson(reader.ReadToEnd());
            }
            catch { /* ملف تالف: تجاهل */ }
        }

        // 2) مجلد lang بجانب التنفيذي (يسمح للمستخدم بإضافة/استبدال لغات)
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "lang");
            if (Directory.Exists(dir))
                foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                    try { AddFromJson(File.ReadAllText(file)); } catch { }
        }
        catch { }
    }

    private void AddFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var meta = root.GetProperty("meta");

        var code = meta.GetProperty("code").GetString();
        var name = meta.GetProperty("name").GetString();
        var dir = meta.TryGetProperty("direction", out var d) ? d.GetString() : "ltr";
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) return;

        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.TryGetProperty("strings", out var s) && s.ValueKind == JsonValueKind.Object)
            foreach (var kv in s.EnumerateObject())
                strings[kv.Name] = kv.Value.GetString() ?? kv.Name;

        _languages[code!] = new LanguageInfo
        {
            Code = code!, Name = name!, Direction = dir ?? "ltr", Strings = strings
        };
    }

    private static LanguageInfo Fallback() => new()
    {
        Code = "en", Name = "English", Direction = "ltr",
        Strings = new Dictionary<string, string>()
    };
}
