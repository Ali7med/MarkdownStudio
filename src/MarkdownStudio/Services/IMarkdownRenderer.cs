namespace MarkdownStudio.Services;

/// <summary>يحوّل نص Markdown إلى صفحة HTML كاملة جاهزة للعرض في WebView2.</summary>
public interface IMarkdownRenderer
{
    /// <summary>يحوّل جسم Markdown إلى شظية HTML (بدون &lt;html&gt; wrapper).</summary>
    string RenderFragment(string markdown);

    /// <summary>يبني صفحة HTML كاملة (مع CSS الخاص بالثيم) لعرضها في المعاينة.</summary>
    string RenderDocument(string markdown, bool darkTheme, string? baseDirectory = null);

    /// <summary>يبني صفحة HTML قابلة للتحرير المباشر (WYSIWYG) مع جسر أوامر إلى المضيف.</summary>
    string RenderEditable(string markdown, bool darkTheme, string? baseDirectory = null);

    /// <summary>رقم السطر (1-based) للكتلة العليا رقم blockIndex — لمزامنة المؤشر.</summary>
    int GetBlockLine(string markdown, int blockIndex);
}
