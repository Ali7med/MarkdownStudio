using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MarkdownStudio.Models;

/// <summary>
/// يمثّل مستنداً واحداً مفتوحاً في المحرر (ملف على القرص أو مسودة جديدة).
/// </summary>
public partial class MarkdownDocument : ObservableObject
{
    /// <summary>المسار الكامل على القرص، أو null إذا كان المستند جديداً غير محفوظ.</summary>
    [ObservableProperty]
    private string? _filePath;

    /// <summary>محتوى المستند النصي.</summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>هل توجد تعديلات غير محفوظة (Dirty state).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isModified;

    /// <summary>هل التبويب مثبّت (لا يُغلق مع "إغلاق الكل/الآخرين").</summary>
    [ObservableProperty]
    private bool _isPinned;

    /// <summary>ترميز الملف (افتراضياً UTF-8 بدون BOM).</summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(false);

    /// <summary>نوع نهاية السطر المكتشف.</summary>
    public string LineEnding { get; set; } = "\r\n";

    /// <summary>اسم يُعرض في التبويب/العنوان مع مؤشّر التعديل.</summary>
    public string Title
    {
        get
        {
            var name = FilePath is null ? "Untitled" : Path.GetFileName(FilePath);
            return IsModified ? $"● {name}" : name;
        }
    }

    partial void OnFilePathChanged(string? value) => OnPropertyChanged(nameof(Title));
}
