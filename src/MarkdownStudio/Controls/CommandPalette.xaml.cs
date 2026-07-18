using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarkdownStudio.Models;

namespace MarkdownStudio.Controls;

/// <summary>لوحة موحّدة للأوامر / الفتح السريع / بحث الرموز مع ترشيح ضبابي.</summary>
public partial class CommandPalette : UserControl
{
    private IReadOnlyList<PaletteItem> _all = Array.Empty<PaletteItem>();
    private readonly ObservableCollection<PaletteItem> _filtered = new();

    public CommandPalette()
    {
        InitializeComponent();
        ResultList.ItemsSource = _filtered;
    }

    /// <summary>يعرض اللوحة بمجموعة عناصر ونصّ إرشادي.</summary>
    public void Show(IReadOnlyList<PaletteItem> items, string placeholder)
    {
        _all = items;
        SearchBox.PlaceholderText = placeholder;
        SearchBox.Text = string.Empty;
        Filter(string.Empty);
        Visibility = Visibility.Visible;
        SearchBox.Focus();
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void Filter(string query)
    {
        _filtered.Clear();

        IEnumerable<PaletteItem> matches = string.IsNullOrWhiteSpace(query)
            ? _all
            : _all.Select(item => (item, score: Score(query, item)))
                  .Where(x => x.score > 0)
                  .OrderByDescending(x => x.score)
                  .Select(x => x.item);

        foreach (var item in matches.Take(200))
            _filtered.Add(item);

        if (_filtered.Count > 0) ResultList.SelectedIndex = 0;
    }

    /// <summary>تقييم بسيط: تطابق كامل &gt; بداية &gt; احتواء &gt; تتابع أحرف.</summary>
    private static int Score(string query, PaletteItem item)
    {
        var q = query.Trim().ToLowerInvariant();
        var best = 0;
        foreach (var field in new[] { item.Title, item.SearchKey ?? item.Subtitle })
        {
            if (string.IsNullOrEmpty(field)) continue;
            var f = field.ToLowerInvariant();
            if (f == q) best = Math.Max(best, 100);
            else if (f.StartsWith(q)) best = Math.Max(best, 80);
            else if (f.Contains(q)) best = Math.Max(best, 60);
            else if (IsSubsequence(q, f)) best = Math.Max(best, 30);
        }
        return best;
    }

    private static bool IsSubsequence(string q, string text)
    {
        var i = 0;
        foreach (var c in text)
            if (i < q.Length && q[i] == c) i++;
        return i == q.Length;
    }

    private void OnQueryChanged(object sender, TextChangedEventArgs e) => Filter(SearchBox.Text);

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down: Move(+1); e.Handled = true; break;
            case Key.Up: Move(-1); e.Handled = true; break;
            case Key.Enter: InvokeSelected(); e.Handled = true; break;
            case Key.Escape: Hide(); e.Handled = true; break;
        }
    }

    private void Move(int delta)
    {
        if (_filtered.Count == 0) return;
        var i = ResultList.SelectedIndex + delta;
        ResultList.SelectedIndex = Math.Clamp(i, 0, _filtered.Count - 1);
        ResultList.ScrollIntoView(ResultList.SelectedItem);
    }

    private void OnItemClick(object sender, MouseButtonEventArgs e) => InvokeSelected();

    private void InvokeSelected()
    {
        if (ResultList.SelectedItem is PaletteItem item)
        {
            Hide();
            item.Invoke();
        }
    }
}
