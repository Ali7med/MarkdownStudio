using System.IO;
using System.Text;
using MarkdownStudio.Models;
using Microsoft.Win32;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IFileService"/> باستخدام نظام الملفات وحوارات Win32.</summary>
public sealed class FileService : IFileService
{
    private const string Filter =
        "Markdown files|*.md;*.markdown;*.mdown;*.mkd;*.mkdn;*.mdtxt|All files|*.*";

    public async Task<MarkdownDocument> OpenAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var (encoding, text) = DecodeWithBom(bytes);
        var lineEnding = text.Contains("\r\n") ? "\r\n" : text.Contains('\n') ? "\n" : "\r\n";

        return new MarkdownDocument
        {
            FilePath = path,
            Content = text,
            Encoding = encoding,
            LineEnding = lineEnding,
            IsModified = false
        };
    }

    public Task SaveAsync(MarkdownDocument document)
    {
        if (string.IsNullOrEmpty(document.FilePath))
            throw new InvalidOperationException("لا يوجد مسار للحفظ. استخدم SaveAs.");
        return WriteAsync(document, document.FilePath);
    }

    public async Task SaveAsAsync(MarkdownDocument document, string path)
    {
        await WriteAsync(document, path);
        document.FilePath = path;
    }

    private static async Task WriteAsync(MarkdownDocument document, string path)
    {
        await File.WriteAllTextAsync(path, document.Content, document.Encoding);
        document.IsModified = false;
    }

    public string? ShowOpenDialog()
    {
        var dlg = new OpenFileDialog { Filter = Filter, CheckFileExists = true };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveDialog(string? suggestedName = null)
    {
        var dlg = new SaveFileDialog
        {
            Filter = Filter,
            DefaultExt = ".md",
            AddExtension = true,
            FileName = suggestedName ?? "Untitled.md"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveFileDialog(string filter, string defaultExt, string? suggestedName)
    {
        var dlg = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            AddExtension = true,
            FileName = suggestedName
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowFolderDialog()
    {
        var dlg = new OpenFolderDialog { Multiselect = false };
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }

    /// <summary>يكتشف الترميز عبر BOM ويعيد النص المفكوك.</summary>
    private static (Encoding, string) DecodeWithBom(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (new UTF8Encoding(true), new UTF8Encoding(true).GetString(bytes, 3, bytes.Length - 3));
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (Encoding.Unicode, Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2));
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (Encoding.BigEndianUnicode, Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2));

        // افتراضياً UTF-8 بدون BOM.
        var utf8 = new UTF8Encoding(false);
        return (utf8, utf8.GetString(bytes));
    }
}
