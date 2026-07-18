using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;

namespace MarkdownStudio.Services;

/// <summary>خيارات ونتائج البحث داخل المحرّر.</summary>
public sealed record SearchOptions(bool MatchCase, bool WholeWord, bool UseRegex);

/// <summary>محرّك بحث/استبدال يعمل فوق <see cref="TextEditor"/> من AvalonEdit.</summary>
public static class EditorSearch
{
    /// <summary>يبني تعبيراً نمطياً من نص البحث وخياراته، أو null إن كان النمط غير صالح/فارغ.</summary>
    public static Regex? BuildRegex(string query, SearchOptions opts)
    {
        if (string.IsNullOrEmpty(query)) return null;

        var pattern = opts.UseRegex ? query : Regex.Escape(query);
        if (opts.WholeWord) pattern = $@"\b(?:{pattern})\b";

        var options = RegexOptions.Multiline;
        if (!opts.MatchCase) options |= RegexOptions.IgnoreCase;

        try { return new Regex(pattern, options); }
        catch (ArgumentException) { return null; }   // نمط Regex غير صالح
    }

    /// <summary>عدد التطابقات في المستند بالكامل.</summary>
    public static int Count(TextEditor editor, Regex? regex)
        => regex is null ? 0 : regex.Matches(editor.Text).Count;

    /// <summary>ينتقل إلى التطابق التالي بدءاً من موضع المؤشّر (مع التفاف).</summary>
    public static bool FindNext(TextEditor editor, Regex? regex, bool backwards = false)
    {
        if (regex is null) return false;
        var text = editor.Text;
        if (text.Length == 0) return false;

        var start = backwards ? editor.SelectionStart : editor.SelectionStart + editor.SelectionLength;

        Match? match;
        if (backwards)
        {
            match = LastMatchBefore(regex, text, start) ?? LastMatchBefore(regex, text, text.Length);
        }
        else
        {
            match = regex.Match(text, Math.Min(start, text.Length));
            if (!match.Success) match = regex.Match(text, 0);   // التفاف من البداية
        }

        if (match is null || !match.Success) return false;
        Select(editor, match.Index, match.Length);
        return true;
    }

    /// <summary>يستبدل التحديد الحالي إن كان يطابق، ثم ينتقل للتطابق التالي.</summary>
    public static bool ReplaceNext(TextEditor editor, Regex? regex, string replacement)
    {
        if (regex is null) return false;

        var selected = editor.SelectedText;
        var m = selected.Length > 0 ? regex.Match(selected) : Match.Empty;
        if (m.Success && m.Index == 0 && m.Length == selected.Length)
        {
            var result = m.Result(replacement);
            editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, result);
        }
        return FindNext(editor, regex);
    }

    /// <summary>يستبدل كل التطابقات ويعيد عددها.</summary>
    public static int ReplaceAll(TextEditor editor, Regex? regex, string replacement)
    {
        if (regex is null) return 0;
        var count = regex.Matches(editor.Text).Count;
        if (count == 0) return 0;
        editor.Document.Text = regex.Replace(editor.Text, replacement);
        return count;
    }

    private static Match? LastMatchBefore(Regex regex, string text, int position)
    {
        Match? last = null;
        foreach (Match m in regex.Matches(text))
        {
            if (m.Index >= position) break;
            last = m;
        }
        return last;
    }

    private static void Select(TextEditor editor, int offset, int length)
    {
        editor.Select(offset, length);
        var line = editor.Document.GetLineByOffset(offset);
        editor.ScrollToLine(line.LineNumber);
    }
}
