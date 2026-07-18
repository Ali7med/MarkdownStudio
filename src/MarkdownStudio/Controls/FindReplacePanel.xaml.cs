using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using MarkdownStudio.Services;

namespace MarkdownStudio.Controls;

/// <summary>لوحة بحث/استبدال منزلقة تعمل فوق محرّر AvalonEdit.</summary>
public partial class FindReplacePanel : UserControl
{
    private TextEditor? _editor;

    public FindReplacePanel() => InitializeComponent();

    /// <summary>يربط اللوحة بالمحرّر الهدف.</summary>
    public void Attach(TextEditor editor) => _editor = editor;

    /// <summary>يُظهر اللوحة (مع/بدون صف الاستبدال) ويملأ حقل البحث بالتحديد الحالي.</summary>
    public void Show(bool replaceMode)
    {
        ReplaceBox.Visibility = replaceMode ? Visibility.Visible : Visibility.Collapsed;
        Visibility = Visibility.Visible;

        if (_editor is { SelectedText.Length: > 0 } ed && !ed.SelectedText.Contains('\n'))
            FindBox.Text = ed.SelectedText;

        FindBox.Focus();
        FindBox.SelectAll();
        UpdateCount();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        _editor?.Focus();
    }

    private SearchOptions Options => new(
        CaseToggle.IsChecked == true,
        WordToggle.IsChecked == true,
        RegexToggle.IsChecked == true);

    private void UpdateCount()
    {
        if (_editor is null) { CountLabel.Text = "0"; return; }
        var regex = EditorSearch.BuildRegex(FindBox.Text, Options);
        if (FindBox.Text.Length > 0 && regex is null)
        {
            CountLabel.Text = "خطأ";   // نمط Regex غير صالح
            return;
        }
        CountLabel.Text = EditorSearch.Count(_editor, regex).ToString();
    }

    // ===== Handlers =====

    private void OnQueryChanged(object sender, TextChangedEventArgs e) => UpdateCount();

    private void OnOptionsChanged(object sender, RoutedEventArgs e) => UpdateCount();

    private void FindNext(bool backwards)
    {
        if (_editor is null) return;
        var regex = EditorSearch.BuildRegex(FindBox.Text, Options);
        EditorSearch.FindNext(_editor, regex, backwards);
    }

    private void OnFindNext(object sender, RoutedEventArgs e) => FindNext(false);
    private void OnFindPrev(object sender, RoutedEventArgs e) => FindNext(true);
    private void OnClose(object sender, RoutedEventArgs e) => Hide();

    private void OnReplace(object sender, RoutedEventArgs e)
    {
        if (_editor is null) return;
        var regex = EditorSearch.BuildRegex(FindBox.Text, Options);
        EditorSearch.ReplaceNext(_editor, regex, ReplaceBox.Text);
        UpdateCount();
    }

    private void OnReplaceAll(object sender, RoutedEventArgs e)
    {
        if (_editor is null) return;
        var regex = EditorSearch.BuildRegex(FindBox.Text, Options);
        var n = EditorSearch.ReplaceAll(_editor, regex, ReplaceBox.Text);
        CountLabel.Text = $"{n} ✓";
    }

    private void OnFindBoxKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                FindNext(backwards: (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }

    private void OnReplaceBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { OnReplace(sender, e); e.Handled = true; }
        else if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
    }
}
