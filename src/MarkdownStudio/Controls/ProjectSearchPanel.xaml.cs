using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarkdownStudio.Services;

namespace MarkdownStudio.Controls;

/// <summary>لوحة بحث داخل كل ملفات المشروع (Ctrl+Shift+F).</summary>
public partial class ProjectSearchPanel : UserControl
{
    public sealed record ResultVm(string FilePath, int Line, string Preview)
    {
        public string FileName => Path.GetFileName(FilePath);
    }

    private ProjectSearchService? _service;
    private Func<string?>? _rootProvider;
    private Action<string, int>? _openAt;
    private readonly ObservableCollection<ResultVm> _results = new();

    public ProjectSearchPanel()
    {
        InitializeComponent();
        ResultList.ItemsSource = _results;
    }

    public void Attach(ProjectSearchService service, Func<string?> rootProvider, Action<string, int> openAt)
    {
        _service = service;
        _rootProvider = rootProvider;
        _openAt = openAt;
    }

    public void Show()
    {
        Visibility = Visibility.Visible;
        QueryBox.Focus();
        QueryBox.SelectAll();
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void RunSearch()
    {
        _results.Clear();
        var root = _rootProvider?.Invoke();
        if (root is null) { StatusText.Text = "افتح مجلداً أولاً"; return; }
        if (_service is null || QueryBox.Text.Trim().Length == 0) { StatusText.Text = "اكتب واضغط Enter"; return; }

        var options = new SearchOptions(
            MatchCase: CaseToggle.IsChecked == true,
            WholeWord: false,
            UseRegex: RegexToggle.IsChecked == true);

        var hits = _service.Search(root, QueryBox.Text, options);
        foreach (var h in hits)
            _results.Add(new ResultVm(h.FilePath, h.Line, h.Preview));

        StatusText.Text = hits.Count == 0 ? "لا نتائج"
            : $"{hits.Count} نتيجة في {hits.Select(h => h.FilePath).Distinct().Count()} ملف";
        if (_results.Count > 0) ResultList.SelectedIndex = 0;
    }

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { RunSearch(); e.Handled = true; }
        else if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
        else if (e.Key == Key.Down && _results.Count > 0)
        {
            ResultList.SelectedIndex = 0;
            ResultList.Focus();
            e.Handled = true;
        }
    }

    private void OnResultKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Activate(); e.Handled = true; }
        else if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
    }

    private void OnReplaceAll(object sender, RoutedEventArgs e)
    {
        var root = _rootProvider?.Invoke();
        if (root is null) { StatusText.Text = "افتح مجلداً أولاً"; return; }
        if (_service is null || QueryBox.Text.Trim().Length == 0) { StatusText.Text = "اكتب نص البحث"; return; }

        var options = new SearchOptions(
            MatchCase: CaseToggle.IsChecked == true,
            WholeWord: false,
            UseRegex: RegexToggle.IsChecked == true);

        var (replacements, files) = _service.ReplaceAll(root, QueryBox.Text, ReplaceBox.Text, options);
        StatusText.Text = replacements == 0
            ? "لا تطابقات للاستبدال"
            : $"استُبدل {replacements} تطابق في {files} ملف";
        RunSearch();   // حدّث النتائج
    }

    private void OnResultActivate(object sender, MouseButtonEventArgs e) => Activate();

    private void Activate()
    {
        if (ResultList.SelectedItem is ResultVm r)
        {
            Hide();
            _openAt?.Invoke(r.FilePath, r.Line);
        }
    }
}
