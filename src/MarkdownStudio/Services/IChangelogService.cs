using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>يوفّر سجل التغييرات المُحلَّل ومنطق الظهور التلقائي.</summary>
public interface IChangelogService
{
    /// <summary>كل الإصدارات (الأحدث أولاً).</summary>
    IReadOnlyList<ChangelogEntry> Entries { get; }

    /// <summary>هل يجب عرض اللوحة تلقائياً؟ (الإصدار الحالي يختلف عن آخر إصدار عُرِض).</summary>
    bool ShouldAutoShow(string? lastShownVersion);
}
