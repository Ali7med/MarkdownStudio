using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IExportService"/> (HTML/DOCX/PlainText). PDF يُنفَّذ في الواجهة.</summary>
public sealed class ExportService : IExportService
{
    private readonly IMarkdownRenderer _renderer;

    public ExportService(IMarkdownRenderer renderer) => _renderer = renderer;

    public Task ExportHtmlAsync(MarkdownDocument document, string path, bool darkTheme)
    {
        var html = _renderer.RenderDocument(document.Content, darkTheme, BaseDir(document));
        return File.WriteAllTextAsync(path, html, new UTF8Encoding(false));
    }

    public Task ExportPlainTextAsync(MarkdownDocument document, string path)
    {
        var text = Markdown.ToPlainText(document.Content ?? string.Empty);
        return File.WriteAllTextAsync(path, text, new UTF8Encoding(false));
    }

    public string BuildPrintableHtml(MarkdownDocument document)
        => _renderer.RenderDocument(document.Content, darkTheme: false, BaseDir(document));

    public Task ExportDocxAsync(MarkdownDocument document, string path)
    {
        // ثيم فاتح وبدون سكربتات خارجية؛ Word يحوّل HTML إلى محتوى أصلي عبر altChunk.
        var html = _renderer.RenderDocument(document.Content, darkTheme: false, BaseDir(document));

        return Task.Run(() =>
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());

            const string chunkId = "htmlAltChunk1";
            var chunkPart = main.AddAlternativeFormatImportPart(
                AlternativeFormatImportPartType.Html, chunkId);
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(html)))
                chunkPart.FeedData(ms);

            main.Document.Body!.Append(new AltChunk { Id = chunkId });
            main.Document.Save();
        });
    }

    private static string? BaseDir(MarkdownDocument document)
        => document.FilePath is { } p ? Path.GetDirectoryName(p) : null;
}
