using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace MarkdownStudio.Controls;

/// <summary>لوحة "اذهب إلى السطر" (Ctrl+G).</summary>
public partial class GoToLinePanel : UserControl
{
    private TextEditor? _editor;

    public GoToLinePanel() => InitializeComponent();

    public void Attach(TextEditor editor) => _editor = editor;

    public void Show()
    {
        if (_editor is null) return;
        Visibility = Visibility.Visible;
        HintLabel.Text = $"من 1 إلى {_editor.Document.LineCount}";
        LineBox.Text = _editor.TextArea.Caret.Line.ToString();
        LineBox.Focus();
        LineBox.SelectAll();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        _editor?.Focus();
    }

    private void Jump()
    {
        if (_editor is null) return;
        if (!int.TryParse(LineBox.Text.Trim(), out var line)) return;

        line = Math.Clamp(line, 1, _editor.Document.LineCount);
        var docLine = _editor.Document.GetLineByNumber(line);
        _editor.CaretOffset = docLine.Offset;
        _editor.ScrollToLine(line);
        _editor.Select(docLine.Offset, docLine.Length);
        Hide();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Jump(); e.Handled = true; }
        else if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
    }
}
