namespace MarkdownStudio;

/// <summary>
/// المصدر الوحيد لرقم إصدار التطبيق وتاريخه. ممنوع كتابة رقم الإصدار حرفياً في أي مكان آخر —
/// كل الواجهات والكود يقرأ من هنا فقط.
/// </summary>
public static class AppVersion
{
    /// <summary>الإصدار الحالي (SemVer: MAJOR.MINOR.PATCH).</summary>
    public const string Current = "1.1.1";

    /// <summary>تاريخ الإصدار الحالي (ISO 8601: YYYY-MM-DD).</summary>
    public const string ReleasedDate = "2026-07-23";
}
