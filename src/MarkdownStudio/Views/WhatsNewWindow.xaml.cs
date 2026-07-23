using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MarkdownStudio.Localization;
using MarkdownStudio.Models;
using MarkdownStudio.Services;

namespace MarkdownStudio.Views;

/// <summary>لوحة «ما الجديد»: تعرض سجل التغييرات كاملاً (الأحدث أولاً).</summary>
public partial class WhatsNewWindow
{
    public WhatsNewWindow(IChangelogService changelog)
    {
        InitializeComponent();
        VersionBadge.Text = $"{AppVersion.Current} · {AppVersion.ReleasedDate}";
        Build(changelog.Entries);
    }

    private void Build(IReadOnlyList<ChangelogEntry> entries)
    {
        var loc = Localizer.Instance;

        foreach (var entry in entries)
        {
            var block = new StackPanel { Margin = new Thickness(0, 0, 0, 22) };

            // ترويسة الإصدار
            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            header.Children.Add(new TextBlock { Text = entry.Version, FontSize = 18, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock
            {
                Text = entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Opacity = 0.55, FontSize = 12,
                Margin = new Thickness(10, 6, 0, 0), VerticalAlignment = VerticalAlignment.Bottom
            });
            block.Children.Add(header);

            foreach (var kind in new[] { ChangelogSectionKind.Highlights, ChangelogSectionKind.New,
                                         ChangelogSectionKind.Improved, ChangelogSectionKind.Fixed })
            {
                var section = entry.Sections.FirstOrDefault(s => s.Kind == kind);
                if (section is null) continue;
                block.Children.Add(kind == ChangelogSectionKind.Highlights
                    ? BuildHighlights(section, loc)
                    : BuildSection(section, kind, loc));
            }

            EntriesHost.Children.Add(block);
        }
    }

    private FrameworkElement BuildHighlights(ChangelogSection section, Localizer loc)
    {
        var stack = new StackPanel();
        stack.Children.Add(SectionTitle("", loc["whatsNew.section.highlights"], AccentBrush()));
        foreach (var item in section.Items)
            stack.Children.Add(BuildItem(item, "✦", AccentBrush()));

        return new Border
        {
            Background = AccentTint(),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = stack
        };
    }

    private FrameworkElement BuildSection(ChangelogSection section, ChangelogSectionKind kind, Localizer loc)
    {
        var (glyph, brush, key) = kind switch
        {
            ChangelogSectionKind.New => ("", ThemeBrush("SystemFillColorSuccessBrush", "#3FB950"), "whatsNew.section.new"),
            ChangelogSectionKind.Improved => ("", ThemeBrush("SystemFillColorAttentionBrush", "#4493F8"), "whatsNew.section.improved"),
            _ => ("", ThemeBrush("SystemFillColorCautionBrush", "#E8873B"), "whatsNew.section.fixed"),
        };

        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        stack.Children.Add(SectionTitle(glyph, loc[key], brush));
        foreach (var item in section.Items)
            stack.Children.Add(BuildItem(item, "•", brush));
        return stack;
    }

    private static StackPanel SectionTitle(string glyph, string text, Brush brush)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        row.Children.Add(new TextBlock
        {
            Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            Foreground = brush, FontSize = 14, Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, FontSize = 14, Foreground = brush });
        return row;
    }

    /// <summary>بند بنقطة تعداد ونص يدعم <c>**عريض**</c>.</summary>
    private static FrameworkElement BuildItem(string text, string bullet, Brush bulletBrush)
    {
        var grid = new Grid { Margin = new Thickness(6, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dot = new TextBlock { Text = bullet, Foreground = bulletBrush, FontSize = 13, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dot, 0);

        var body = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20, Opacity = 0.92 };
        var bold = false;
        foreach (var part in Regex.Split(text, @"\*\*"))
        {
            if (part.Length > 0)
                body.Inlines.Add(new Run(part) { FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal });
            bold = !bold;
        }
        Grid.SetColumn(body, 1);

        grid.Children.Add(dot);
        grid.Children.Add(body);
        return grid;
    }

    private Brush AccentBrush() => ThemeBrush("AccentTextFillColorPrimaryBrush", "#4493F8");

    /// <summary>يحلّ فرشاة الثيم بمفتاحها (متكيّفة مع الفاتح/الداكن)، مع احتياط لوني إن غاب المفتاح.</summary>
    private Brush ThemeBrush(string key, string fallbackHex) =>
        TryFindResource(key) as Brush ?? Freeze(fallbackHex);

    private Brush AccentTint() =>
        TryFindResource("AccentFillColorSecondaryBrush") as Brush
        ?? new SolidColorBrush(Color.FromArgb(28, 0x44, 0x93, 0xF8)) { Opacity = 1 };

    private static Brush Freeze(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
