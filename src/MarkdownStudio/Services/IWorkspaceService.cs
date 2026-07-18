using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>يبني شجرة مجلد العمل وينفّذ عمليات الملفات عليه.</summary>
public interface IWorkspaceService
{
    /// <summary>يبني عقدة الجذر للمجلد ويحمّل أبناءه (غير متزامن).</summary>
    Task<FileSystemItem> OpenFolderAsync(string path);

    /// <summary>يعيد فحص أبناء عقدة مجلد ويحدّث مجموعتها.</summary>
    void Refresh(FileSystemItem folder);

    /// <summary>يعدّد كل ملفات Markdown داخل المجلد تدرّجياً (لـ Quick Open/البحث).</summary>
    IEnumerable<string> EnumerateMarkdownFiles(string root);

    string CreateFile(string parentDir, string name);
    string CreateFolder(string parentDir, string name);
    string Rename(string path, string newName);
    void Delete(string path);
    string Duplicate(string path);
}
