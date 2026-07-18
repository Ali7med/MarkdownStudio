using System.Collections.ObjectModel;
using System.Text;
using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>يستخرج عناوين ATX من نص Markdown ويبنيها شجرةً متداخلة.</summary>
public static class OutlineService
{
    public readonly record struct Heading(int Level, string Title, int Line);

    /// <summary>قائمة مسطّحة بالعناوين (تتجاهل العناوين داخل كتل الأكواد المسيّجة).</summary>
    public static List<Heading> ParseFlat(string markdown)
    {
        var result = new List<Heading>();
        if (string.IsNullOrEmpty(markdown)) return result;

        var lines = markdown.Split('\n');
        var inFence = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence) continue;

            var level = 0;
            while (level < trimmed.Length && trimmed[level] == '#') level++;
            if (level is < 1 or > 6) continue;
            if (level >= trimmed.Length || trimmed[level] != ' ') continue;

            var title = trimmed[(level + 1)..].Trim().TrimEnd('#').Trim();
            if (title.Length == 0) continue;

            result.Add(new Heading(level, title, i + 1));
        }
        return result;
    }

    /// <summary>يبني شجرة العناوين المتداخلة.</summary>
    public static ObservableCollection<OutlineItem> BuildTree(string markdown)
    {
        var roots = new ObservableCollection<OutlineItem>();
        var stack = new Stack<OutlineItem>();

        foreach (var h in ParseFlat(markdown))
        {
            var item = new OutlineItem { Level = h.Level, Title = h.Title, Line = h.Line };

            while (stack.Count > 0 && stack.Peek().Level >= h.Level)
                stack.Pop();

            if (stack.Count == 0) roots.Add(item);
            else stack.Peek().Children.Add(item);

            stack.Push(item);
        }
        return roots;
    }

    /// <summary>يولّد جدول محتويات Markdown (قائمة مرتّبة بروابط داخلية).</summary>
    public static string GenerateToc(string markdown)
    {
        var sb = new StringBuilder();
        foreach (var h in ParseFlat(markdown))
        {
            if (h.Level == 1) continue;   // نتجاوز عنوان المستند الرئيسي عادةً
            sb.Append(new string(' ', (h.Level - 2) * 2));
            sb.Append("- [").Append(h.Title).Append("](#").Append(Slug(h.Title)).Append(")\n");
        }
        return sb.ToString();
    }

    /// <summary>يحوّل عنواناً إلى مُعرّف رابط بأسلوب GitHub.</summary>
    private static string Slug(string title)
    {
        var sb = new StringBuilder(title.Length);
        foreach (var c in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-') sb.Append('-');
            // باقي الرموز تُحذف
        }
        return sb.ToString();
    }
}
