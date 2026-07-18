namespace MarkdownStudio.Models;

/// <summary>عنصر في لوحة الأوامر / الفتح السريع / بحث الرموز.</summary>
public sealed class PaletteItem
{
    public required string Title { get; init; }
    public string? Subtitle { get; init; }

    /// <summary>الإجراء المُنفَّذ عند الاختيار.</summary>
    public required Action Invoke { get; init; }

    /// <summary>مفتاح إضافي للبحث (مسار كامل مثلاً) إلى جانب العنوان.</summary>
    public string? SearchKey { get; init; }
}
