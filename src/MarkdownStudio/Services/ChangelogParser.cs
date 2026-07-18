using System.Globalization;
using System.Text.RegularExpressions;
using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>
/// محلّل صارم لملف CHANGELOG.md. يعتمد الأنماط حرفياً:
///   ## [x.y.z] - YYYY-MM-DD
///   ### HIGHLIGHTS | NEW | IMPROVED | FIXED
///   - بند
/// </summary>
public static partial class ChangelogParser
{
    public static List<ChangelogEntry> Parse(string text)
    {
        var entries = new List<ChangelogEntry>();
        if (string.IsNullOrEmpty(text)) return entries;

        ChangelogEntry? current = null;
        ChangelogSection? section = null;

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();

            var header = HeaderRegex().Match(line);
            if (header.Success)
            {
                current = new ChangelogEntry
                {
                    Version = header.Groups["ver"].Value,
                    Date = DateOnly.ParseExact(header.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                };
                entries.Add(current);
                section = null;
                continue;
            }

            var sec = SectionRegex().Match(line);
            if (sec.Success && current is not null)
            {
                var kind = sec.Groups["kind"].Value switch
                {
                    "HIGHLIGHTS" => ChangelogSectionKind.Highlights,
                    "NEW" => ChangelogSectionKind.New,
                    "IMPROVED" => ChangelogSectionKind.Improved,
                    "FIXED" => ChangelogSectionKind.Fixed,
                    _ => (ChangelogSectionKind?)null
                };
                if (kind is null) { section = null; continue; }
                section = new ChangelogSection { Kind = kind.Value };
                current.Sections.Add(section);
                continue;
            }

            var item = ItemRegex().Match(line);
            if (item.Success && section is not null)
                section.Items.Add(item.Groups["text"].Value.Trim());
        }

        // أزل الأقسام الفارغة.
        foreach (var e in entries)
            e.Sections.RemoveAll(s => s.Items.Count == 0);

        return entries;
    }

    [GeneratedRegex(@"^##\s+\[(?<ver>[^\]]+)\]\s+-\s+(?<date>\d{4}-\d{2}-\d{2})\s*$")]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^###\s+(?<kind>HIGHLIGHTS|NEW|IMPROVED|FIXED)\s*$")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^-\s+(?<text>.+)$")]
    private static partial Regex ItemRegex();
}
