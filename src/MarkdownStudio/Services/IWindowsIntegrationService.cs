namespace MarkdownStudio.Services;

/// <summary>تكامل Windows: ربط الملفات، قائمة السياق، بروتوكول URI، Jump List.</summary>
public interface IWindowsIntegrationService
{
    /// <summary>true إن كانت النسخة محمولة (لا تُعدّل الـ Registry).</summary>
    bool IsPortable { get; }

    /// <summary>true إن كان ProgId الخاص بالتطبيق مسجّلاً حالياً.</summary>
    bool IsRegistered { get; }

    /// <summary>يسجّل ربط الملفات + قائمة السياق + بروتوكول URI (HKCU، بلا صلاحيات مدير).</summary>
    void RegisterIntegration();

    /// <summary>يلغي كل التسجيلات ويعيد الحالة الافتراضية.</summary>
    void UnregisterIntegration();

    /// <summary>يحدّث Jump List في شريط المهام بالملفات الأخيرة.</summary>
    void UpdateJumpList(IEnumerable<string> recentFiles);

    /// <summary>يضيف ملفاً إلى "المستندات الأخيرة" في Windows (يظهر ببحث ويندوز والوصول السريع).</summary>
    void AddToRecentDocs(string path);
}
