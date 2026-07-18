using System.Collections.ObjectModel;

namespace MarkdownStudio.Models;

/// <summary>عنوان في مخطّط المستند (Outline)، مع أبنائه المتداخلين.</summary>
public sealed class OutlineItem
{
    public required int Level { get; init; }       // 1..6
    public required string Title { get; init; }
    public required int Line { get; init; }        // رقم السطر (1-based)
    public ObservableCollection<OutlineItem> Children { get; } = new();
}
