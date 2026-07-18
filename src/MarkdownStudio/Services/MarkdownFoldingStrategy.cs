using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace MarkdownStudio.Services;

/// <summary>
/// استراتيجية طيّ لملفات Markdown: تطوي الأقسام تحت العناوين وكتل الأكواد المسيّجة.
/// </summary>
public sealed class MarkdownFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = CreateFoldings(document);
        manager.UpdateFoldings(foldings, -1);
    }

    private static IEnumerable<NewFolding> CreateFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();

        // مكدّس العناوين المفتوحة: (المستوى، إزاحة نهاية سطر العنوان).
        var headingStack = new Stack<(int Level, int StartOffset)>();
        var lineCount = document.LineCount;

        int? fenceStart = null;   // إزاحة بداية كتلة الكود المسيّجة

        for (var n = 1; n <= lineCount; n++)
        {
            var line = document.GetLineByNumber(n);
            var text = document.GetText(line.Offset, line.Length);
            var trimmed = text.TrimStart();

            // كتل الأكواد المسيّجة ``` أو ~~~
            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                if (fenceStart is null)
                {
                    fenceStart = line.Offset + line.Length;
                }
                else
                {
                    if (line.Offset > fenceStart.Value)
                        foldings.Add(new NewFolding(fenceStart.Value, line.Offset + line.Length)
                        { Name = "```…```" });
                    fenceStart = null;
                }
                continue;
            }
            if (fenceStart is not null) continue;   // داخل كتلة كود: تجاهل العناوين

            // العناوين ATX (# .. ######)
            var level = HeadingLevel(trimmed);
            if (level == 0) continue;

            // أغلق الأقسام ذات المستوى الأعمق أو المساوي قبل هذا العنوان.
            while (headingStack.Count > 0 && headingStack.Peek().Level >= level)
                CloseSection(foldings, headingStack.Pop(), line.PreviousLine);

            headingStack.Push((level, line.Offset + line.Length));
        }

        // أغلق ما تبقّى حتى نهاية المستند.
        var last = document.GetLineByNumber(lineCount);
        while (headingStack.Count > 0)
            CloseSection(foldings, headingStack.Pop(), last);

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldings;
    }

    private static void CloseSection(List<NewFolding> foldings, (int Level, int StartOffset) heading, DocumentLine? endLine)
    {
        if (endLine is null) return;
        var end = endLine.Offset + endLine.Length;
        if (end > heading.StartOffset)
            foldings.Add(new NewFolding(heading.StartOffset, end));
    }

    /// <summary>يعيد مستوى العنوان (1..6) أو 0 إن لم يكن سطر عنوان.</summary>
    private static int HeadingLevel(string trimmed)
    {
        var i = 0;
        while (i < trimmed.Length && trimmed[i] == '#') i++;
        return i is >= 1 and <= 6 && i < trimmed.Length && trimmed[i] == ' ' ? i : 0;
    }
}
