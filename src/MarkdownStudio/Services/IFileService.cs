using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>عمليات القرص للمستندات: فتح، حفظ، حوارات النظام.</summary>
public interface IFileService
{
    /// <summary>يقرأ ملفاً من القرص ويعيد مستنداً مع اكتشاف الترميز ونهاية السطر.</summary>
    Task<MarkdownDocument> OpenAsync(string path);

    /// <summary>يحفظ المستند إلى مساره الحالي (يجب أن يكون FilePath غير فارغ).</summary>
    Task SaveAsync(MarkdownDocument document);

    /// <summary>يحفظ المستند إلى مسار جديد ويحدّث FilePath.</summary>
    Task SaveAsAsync(MarkdownDocument document, string path);

    /// <summary>يعرض حوار "فتح" ويعيد المسار المختار أو null.</summary>
    string? ShowOpenDialog();

    /// <summary>يعرض حوار "حفظ باسم" ويعيد المسار المختار أو null.</summary>
    string? ShowSaveDialog(string? suggestedName = null);

    /// <summary>حوار حفظ عام بمُرشِّح وامتداد مخصّصين (للتصدير).</summary>
    string? ShowSaveFileDialog(string filter, string defaultExt, string? suggestedName);

    /// <summary>يعرض حوار اختيار مجلد ويعيد المسار أو null.</summary>
    string? ShowFolderDialog();
}
