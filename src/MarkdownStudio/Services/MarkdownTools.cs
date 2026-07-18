using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownStudio.Services;

/// <summary>أدوات تحويل نصّية على مستوى المستند (ترقيم، فحص روابط، لصق ذكي).</summary>
public static partial class MarkdownTools
{
    /// <summary>يعيد ترقيم العناوين هرمياً (1، 1.1، 1.2، 2 …) متجاهلاً كتل الأكواد.</summary>
    public static string AutoNumberHeadings(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var counters = new int[6];
        var inFence = false;
        var sb = new StringBuilder(markdown.Length + 64);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                inFence = !inFence;
                sb.Append(line);
                if (i < lines.Length - 1) sb.Append('\n');
                continue;
            }

            var level = 0;
            if (!inFence)
            {
                while (level < trimmed.Length && trimmed[level] == '#') level++;
            }

            if (level is >= 1 and <= 6 && level < trimmed.Length && trimmed[level] == ' ')
            {
                counters[level - 1]++;
                for (var d = level; d < 6; d++) counters[d] = 0;

                var number = string.Join('.', counters.Take(level).Where(c => c > 0));
                // أزل أي ترقيم سابق من نص العنوان.
                var title = StripLeadingNumber(trimmed[(level + 1)..].Trim());
                sb.Append(new string('#', level)).Append(' ')
                  .Append(number).Append(' ').Append(title);
            }
            else
            {
                sb.Append(line);
            }

            if (i < lines.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string StripLeadingNumber(string title)
        => LeadingNumberRegex().Replace(title, string.Empty);

    /// <summary>
    /// تنسيق تلقائي: يزيل الفراغات الطرفية، يوحّد علامات القوائم إلى "-"،
    /// يضمن مسافة بعد #، ويقلّص الأسطر الفارغة المتتالية.
    /// </summary>
    public static string Format(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(markdown.Length + 32);
        var inFence = false;
        var blankRun = 0;

        foreach (var original in lines)
        {
            var line = original;
            var trimmedStart = line.TrimStart();

            if (trimmedStart.StartsWith("```") || trimmedStart.StartsWith("~~~"))
            {
                inFence = !inFence;
                sb.Append(line.TrimEnd()).Append('\n');
                blankRun = 0;
                continue;
            }

            if (inFence)
            {
                sb.Append(line).Append('\n');   // لا تلمس محتوى الكود
                continue;
            }

            line = line.TrimEnd();

            // تقليص الأسطر الفارغة المتتالية إلى واحد.
            if (line.Length == 0)
            {
                if (++blankRun > 1) continue;
                sb.Append('\n');
                continue;
            }
            blankRun = 0;

            var indent = line[..(line.Length - line.TrimStart().Length)];
            var body = line.TrimStart();

            // توحيد علامات القوائم غير المرتّبة: * أو + ⇒ -
            if (body.Length >= 2 && (body[0] is '*' or '+') && body[1] == ' ')
                body = "- " + body[2..];
            // ضمان مسافة بعد علامات العنوان.
            else if (body.StartsWith('#'))
            {
                var h = 0;
                while (h < body.Length && body[h] == '#') h++;
                if (h < body.Length && body[h] != ' ')
                    body = body[..h] + " " + body[h..];
            }

            sb.Append(indent).Append(body).Append('\n');
        }

        return sb.ToString().TrimEnd('\n') + "\n";   // نهاية بسطر واحد
    }

    /// <summary>يفحص الروابط والصور المحلية ويعيد قائمة المكسورة (النص، رقم السطر).</summary>
    public static List<(string Target, int Line)> FindBrokenLinks(string markdown, string? baseDir)
    {
        var broken = new List<(string, int)>();
        if (string.IsNullOrEmpty(baseDir)) return broken;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (Match m in LinkRegex().Matches(lines[i]))
            {
                var target = m.Groups["url"].Value.Trim();
                if (target.Length == 0) continue;
                if (target.StartsWith('#')) continue;                       // مرساة داخلية
                if (Uri.IsWellFormedUriString(target, UriKind.Absolute)) continue;  // رابط خارجي

                var pathPart = target.Split('#')[0].Split('?')[0];
                if (pathPart.Length == 0) continue;

                var full = Path.GetFullPath(Path.Combine(baseDir, pathPart));
                if (!File.Exists(full) && !Directory.Exists(full))
                    broken.Add((target, i + 1));
            }
        }
        return broken;
    }

    [GeneratedRegex(@"^\d+(\.\d+)*\.?\s+")]
    private static partial Regex LeadingNumberRegex();

    // يلتقط [text](url) و ![alt](url)
    [GeneratedRegex(@"!?\[(?<text>[^\]]*)\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();
}
