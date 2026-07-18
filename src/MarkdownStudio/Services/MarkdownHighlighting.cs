using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace MarkdownStudio.Services;

/// <summary>يحمّل تعريف تلوين Markdown من المورد المضمّن (Assets/Markdown.xshd).</summary>
public static class MarkdownHighlighting
{
    private const string ResourceName = "MarkdownStudio.Assets.Markdown.xshd";
    private static IHighlightingDefinition? _cached;

    public static IHighlightingDefinition Load()
    {
        if (_cached is not null) return _cached;

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"مورد التلوين غير موجود: {ResourceName}");
        using var reader = XmlReader.Create(stream);

        _cached = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting(
            "Markdown", new[] { ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdtxt" }, _cached);
        return _cached;
    }
}
