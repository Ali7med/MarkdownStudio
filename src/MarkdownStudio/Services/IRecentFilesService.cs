using System.Collections.ObjectModel;

namespace MarkdownStudio.Services;

/// <summary>يدير قائمة الملفات المفتوحة مؤخراً مع الحفظ على القرص.</summary>
public interface IRecentFilesService
{
    /// <summary>القائمة الحالية (الأحدث أولاً)، قابلة للربط بالواجهة.</summary>
    ReadOnlyObservableCollection<string> Files { get; }

    /// <summary>يضيف مساراً إلى القمة (مع إزالة التكرار) ويحفظ.</summary>
    void Add(string path);

    /// <summary>يزيل مساراً (مثلاً عند تعذّر فتحه) ويحفظ.</summary>
    void Remove(string path);

    void Clear();
}
