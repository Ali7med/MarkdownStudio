namespace MarkdownStudio.Models;

/// <summary>أنواع أقسام سجل التغييرات (بالترتيب المعروض).</summary>
public enum ChangelogSectionKind { Highlights, New, Improved, Fixed }

/// <summary>قسم داخل إصدار (نوع + بنوده النصية).</summary>
public sealed class ChangelogSection
{
    public required ChangelogSectionKind Kind { get; init; }
    public List<string> Items { get; } = new();
}

/// <summary>مدخلة إصدار واحد: رقم، تاريخ، وأقسام مصنّفة.</summary>
public sealed class ChangelogEntry
{
    public required string Version { get; init; }
    public required DateOnly Date { get; init; }
    public List<ChangelogSection> Sections { get; } = new();
}
