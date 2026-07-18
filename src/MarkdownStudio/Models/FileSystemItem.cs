using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MarkdownStudio.Models;

/// <summary>عقدة في شجرة مستكشف الملفات (ملف أو مجلد).</summary>
public partial class FileSystemItem : ObservableObject
{
    private static readonly HashSet<string> MarkdownExt = new(StringComparer.OrdinalIgnoreCase)
        { ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdtxt" };

    public FileSystemItem(string fullPath, bool isDirectory)
    {
        _fullPath = fullPath;
        _name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar));
        if (string.IsNullOrEmpty(_name)) _name = fullPath;   // جذر القرص
        IsDirectory = isDirectory;
    }

    public bool IsDirectory { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _fullPath;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isRenaming;

    /// <summary>أبناء المجلد (فارغة للملفات).</summary>
    public ObservableCollection<FileSystemItem> Children { get; } = new();

    public bool IsMarkdown => !IsDirectory && MarkdownExt.Contains(Path.GetExtension(FullPath));
}
