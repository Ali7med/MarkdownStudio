using System.IO;
using System.Reflection;
using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>
/// يقرأ CHANGELOG.md وقت التشغيل (مورد مضمّن أولاً، ثم القرص كاحتياط للتطوير) ويحلّله.
/// </summary>
public sealed class ChangelogService : IChangelogService
{
    private const string ResourceName = "MarkdownStudio.CHANGELOG.md";

    public ChangelogService()
    {
        var text = ReadEmbedded() ?? ReadFromDisk() ?? string.Empty;
        Entries = ChangelogParser.Parse(text);
    }

    public IReadOnlyList<ChangelogEntry> Entries { get; }

    public bool ShouldAutoShow(string? lastShownVersion)
        => !string.Equals(lastShownVersion, AppVersion.Current, StringComparison.Ordinal);

    private static string? ReadEmbedded()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream is null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch { return null; }
    }

    /// <summary>يبحث عن CHANGELOG.md بجانب التنفيذي وفي مجلدات الجذر (مفيد أثناء التطوير).</summary>
    private static string? ReadFromDisk()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "CHANGELOG.md");
            if (File.Exists(candidate))
                try { return File.ReadAllText(candidate); } catch { return null; }
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        return null;
    }
}
