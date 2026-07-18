using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>صيغ التصدير المدعومة.</summary>
public enum ExportFormat { Html, Pdf, Docx, PlainText }

/// <summary>يصدّر مستند Markdown إلى صيغ أخرى (لا يتضمّن PDF — يُنفَّذ في الواجهة عبر WebView2).</summary>
public interface IExportService
{
    Task ExportHtmlAsync(MarkdownDocument document, string path, bool darkTheme);
    Task ExportDocxAsync(MarkdownDocument document, string path);
    Task ExportPlainTextAsync(MarkdownDocument document, string path);

    /// <summary>يبني HTML كامل جاهز للطباعة (ثيم فاتح) — يُستخدم لتصدير PDF في الواجهة.</summary>
    string BuildPrintableHtml(MarkdownDocument document);
}
