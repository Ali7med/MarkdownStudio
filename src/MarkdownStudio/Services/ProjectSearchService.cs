using System.IO;
using System.Text.RegularExpressions;

namespace MarkdownStudio.Services;

/// <summary>نتيجة بحث داخل ملفات المشروع.</summary>
public sealed record SearchHit(string FilePath, int Line, string Preview);

/// <summary>يبحث عن نص/نمط داخل كل ملفات Markdown في مساحة العمل.</summary>
public sealed class ProjectSearchService
{
    private readonly IWorkspaceService _workspace;

    public ProjectSearchService(IWorkspaceService workspace) => _workspace = workspace;

    /// <summary>يبحث في كل ملفات المجلد ويعيد التطابقات (بحدّ أقصى لتفادي التجمّد).</summary>
    public List<SearchHit> Search(string root, string query, SearchOptions options, int maxHits = 500)
    {
        var hits = new List<SearchHit>();
        var regex = EditorSearch.BuildRegex(query, options);
        if (regex is null) return hits;

        foreach (var file in _workspace.EnumerateMarkdownFiles(root))
        {
            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch { continue; }

            for (var i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i])) continue;
                hits.Add(new SearchHit(file, i + 1, lines[i].Trim()));
                if (hits.Count >= maxHits) return hits;
            }
        }
        return hits;
    }

    /// <summary>يستبدل كل التطابقات في كل ملفات المشروع. يعيد (عدد التطابقات، عدد الملفات).</summary>
    public (int Replacements, int Files) ReplaceAll(
        string root, string query, string replacement, SearchOptions options)
    {
        var regex = EditorSearch.BuildRegex(query, options);
        if (regex is null) return (0, 0);

        int totalReplacements = 0, filesChanged = 0;
        foreach (var file in _workspace.EnumerateMarkdownFiles(root))
        {
            string text;
            try { text = File.ReadAllText(file); }
            catch { continue; }

            var matches = regex.Matches(text).Count;
            if (matches == 0) continue;

            try
            {
                File.WriteAllText(file, regex.Replace(text, replacement));
                totalReplacements += matches;
                filesChanged++;
            }
            catch { /* ملف للقراءة فقط: تجاهل */ }
        }
        return (totalReplacements, filesChanged);
    }
}
