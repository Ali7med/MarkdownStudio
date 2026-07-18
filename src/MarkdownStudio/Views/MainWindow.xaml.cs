using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;
using MarkdownStudio.Localization;
using MarkdownStudio.Models;
using MarkdownStudio.Services;
using MarkdownStudio.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;

namespace MarkdownStudio.Views;

public partial class MainWindow
{
    private readonly MainWindowViewModel _vm;
    private readonly FoldingManager _foldingManager;
    private readonly MarkdownFoldingStrategy _foldingStrategy = new();
    private readonly DispatcherTimer _foldingTimer;
    private MarkdownDocument? _boundDocument;
    private bool _suppressEditorSync;
    private bool _webViewReady;

    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Editor.SyntaxHighlighting = MarkdownHighlighting.Load();
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.EnableHyperlinks = false;
        FindPanel.Attach(Editor);
        GoToPanel.Attach(Editor);

        _foldingManager = FoldingManager.Install(Editor.TextArea);
        _foldingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _foldingTimer.Tick += (_, _) => { _foldingTimer.Stop(); UpdateFoldings(); };

        Editor.PreviewMouseWheel += OnEditorMouseWheel;   // تكبير/تصغير Ctrl+عجلة
        System.Windows.DataObject.AddPastingHandler(Editor, OnEditorPaste);   // لصق ذكي + صور

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.ConfirmCloseAsync = ConfirmCloseDocumentAsync;
        _vm.ConfirmDeleteAsync = ConfirmDeleteItemAsync;
        _vm.RequestJumpToLine += JumpToLine;
        _vm.RequestInsertText += InsertAtCaret;
        _vm.RequestVisualMode += ApplyVisualMode;

        // Fade + خلفية النافذة عند تغيير الثيم.
        var themeSvc = App.Services.GetRequiredService<IThemeService>();
        themeSvc.ThemeChanged += (_, _) => { AnimateThemeFade(); ApplyBackdrop(themeSvc.CurrentPreset); };
        ApplyBackdrop(themeSvc.CurrentPreset);   // الخلفية الابتدائية (الحدث سبق الاشتراك عند الإقلاع)

        ProjectSearch.Attach(
            App.Services.GetRequiredService<ProjectSearchService>(),
            () => _vm.WorkspacePath,
            async (path, line) => { await _vm.OpenPathAsync(path); JumpToLine(line); });
        Editor.TextChanged += OnEditorTextChanged;
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        PreviewKeyDown += OnWindowPreviewKeyDown;
        Loaded += OnLoaded;
        Closing += OnWindowClosing;

        BuildLanguageMenu();
        BuildThemeMenu();
        Localizer.Instance.LanguageChanged += (_, _) =>
        {
            BuildLanguageMenu();
            BuildThemeMenu();
            OnCaretPositionChanged(null, EventArgs.Empty);   // حدّث «سطر/عمود» باللغة الجديدة
        };
    }

    // ===== Theme picker =====

    private void BuildThemeMenu()
    {
        var theme = App.Services.GetRequiredService<IThemeService>();
        ThemeMenu.Items.Clear();
        foreach (var preset in theme.Presets)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = Localizer.Instance[preset.NameKey],
                IsCheckable = true,
                IsChecked = preset.Id == theme.CurrentPreset.Id,
                Tag = preset.Id
            };
            item.Click += OnThemeSelected;
            ThemeMenu.Items.Add(item);
        }
    }

    private void OnThemeSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem { Tag: string id }) return;
        var theme = App.Services.GetRequiredService<IThemeService>();
        theme.ApplyPreset(id);
        BuildThemeMenu();   // حدّث علامات الاختيار

        var settings = App.Services.GetRequiredService<ISettingsService>();
        settings.Settings.ThemeId = id;
        settings.Save();
    }

    /// <summary>يطبّق نوع خلفية النافذة (Mica/صلبة) حسب القالب.</summary>
    private void ApplyBackdrop(ThemePreset preset) => WindowBackdropType = preset.Backdrop;

    // ===== What's New =====

    private void OnOpenWhatsNew(object sender, RoutedEventArgs e)
    {
        var win = App.Services.GetRequiredService<WhatsNewWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    // ===== Language switching =====

    private void BuildLanguageMenu()
    {
        LanguageMenu.Items.Clear();
        foreach (var lang in Localizer.Instance.Available)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = lang.Name,
                IsCheckable = true,
                IsChecked = lang.Code == Localizer.Instance.CurrentCode,
                Tag = lang.Code
            };
            item.Click += OnLanguageSelected;
            LanguageMenu.Items.Add(item);
        }
    }

    private void OnLanguageSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem { Tag: string code }) return;
        if (!Localizer.Instance.SetLanguage(code)) return;

        var settings = App.Services.GetRequiredService<ISettingsService>();
        settings.Settings.LanguageCode = code;
        settings.Save();
    }

    private void AnimateThemeFade()
    {
        var fade = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.55,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
        };
        RootGrid.BeginAnimation(OpacityProperty, fade);
    }

    // ===== Outline / Navigation =====

    private void JumpToLine(int line)
    {
        if (line < 1 || line > Editor.Document.LineCount) return;
        var docLine = Editor.Document.GetLineByNumber(line);
        Editor.CaretOffset = docLine.Offset;
        Editor.ScrollToLine(line);
        Editor.TextArea.Caret.BringCaretToView();
        Editor.Focus();
    }

    private void InsertAtCaret(string text)
    {
        Editor.Document.Insert(Editor.CaretOffset, text);
        Editor.Focus();
    }

    private void OnOutlineSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is OutlineItem item)
            _vm.JumpToOutlineCommand.Execute(item);
    }

    // ===== Explorer interactions =====

    private void OnExplorerDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ExplorerTree.SelectedItem is FileSystemItem item && !item.IsDirectory)
            _vm.ActivateItemCommand.Execute(item);
    }

    private void OnExplorerKeyDown(object sender, KeyEventArgs e)
    {
        if (ExplorerTree.SelectedItem is not FileSystemItem item) return;
        switch (e.Key)
        {
            case Key.Enter: _vm.ActivateItemCommand.Execute(item); e.Handled = true; break;
            case Key.F2: item.IsRenaming = true; e.Handled = true; break;
            case Key.Delete: _vm.DeleteItemCommand.Execute(item); e.Handled = true; break;
        }
    }

    private void OnRenameMenu(object sender, RoutedEventArgs e)
    {
        if (ExplorerTree.SelectedItem is FileSystemItem item) item.IsRenaming = true;
    }

    private void OnCopyPathMenu(object sender, RoutedEventArgs e)
    {
        if (ExplorerTree.SelectedItem is FileSystemItem item)
            try { Clipboard.SetText(item.FullPath); } catch { /* clipboard مشغول */ }
    }

    private void OnRevealMenu(object sender, RoutedEventArgs e)
    {
        if (ExplorerTree.SelectedItem is not FileSystemItem item) return;
        var arg = item.IsDirectory ? $"\"{item.FullPath}\"" : $"/select,\"{item.FullPath}\"";
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", arg) { UseShellExecute = true }); }
        catch { /* تجاهل */ }
    }

    // ===== Inline rename textbox =====

    private void OnRenameBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox { Tag: FileSystemItem { IsRenaming: true } } tb)
        {
            tb.Focus();
            var stem = Path.GetFileNameWithoutExtension(tb.Text);
            tb.Select(0, string.IsNullOrEmpty(stem) ? tb.Text.Length : stem.Length);
        }
    }

    private void OnRenameKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox { Tag: FileSystemItem item } tb) return;
        if (e.Key == Key.Enter) { _vm.CommitRename(item, tb.Text); e.Handled = true; }
        else if (e.Key == Key.Escape) { item.IsRenaming = false; e.Handled = true; }
    }

    private void OnRenameLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox { Tag: FileSystemItem { IsRenaming: true } item } tb)
            _vm.CommitRename(item, tb.Text);
    }

    private async Task<bool> ConfirmDeleteItemAsync(FileSystemItem item)
    {
        var loc = Localizer.Instance;
        var result = await ShowDialogAsync(
            loc["dialog.deleteTitle"], loc.Format("dialog.deleteBody", item.Name),
            primary: loc["dialog.delete"], secondary: null, close: loc["dialog.cancel"]);
        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }

    // ===== حوارات الحفظ عند الإغلاق =====

    /// <summary>يبني حواراً موحّداً يظهر متوسّطاً على نافذة البرنامج (مهم لأنظمة الشاشتين) مع تخطيط RTL.</summary>
    private async Task<Wpf.Ui.Controls.MessageBoxResult> ShowDialogAsync(
        string title, string content, string primary, string? secondary, string close)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 420,
            MaxWidth = 560,
            Title = title,
            Content = content,
            PrimaryButtonText = primary,
            CloseButtonText = close,
            PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Primary
        };
        if (secondary is not null) box.SecondaryButtonText = secondary;
        return await box.ShowDialogAsync();
    }

    /// <summary>يؤكّد إغلاق مستند معدّل واحد. يعيد true إن جاز المتابعة.</summary>
    private async Task<bool> ConfirmCloseDocumentAsync(MarkdownDocument doc)
    {
        var loc = Localizer.Instance;
        var result = await ShowDialogAsync(
            loc["dialog.saveTitle"],
            loc.Format("dialog.saveDocModified", doc.Title),
            primary: loc["dialog.save"], secondary: loc["dialog.dontSave"], close: loc["dialog.cancel"]);
        return result switch
        {
            Wpf.Ui.Controls.MessageBoxResult.Primary => await _vm.SaveDocumentAsync(doc),
            Wpf.Ui.Controls.MessageBoxResult.Secondary => true,   // تجاهل التعديلات
            _ => false                                            // إلغاء
        };
    }

    private bool _forceClose;

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose) return;
        var count = _vm.ModifiedCount;
        if (count == 0) return;

        e.Cancel = true;   // أوقف الإغلاق حتى يقرّر المستخدم

        var loc = Localizer.Instance;
        var result = await ShowDialogAsync(
            loc["dialog.saveTitle"],
            count == 1 ? loc["dialog.saveOne"] : loc.Format("dialog.saveMany", count),
            primary: loc["dialog.saveAll"], secondary: loc["dialog.dontSave"], close: loc["dialog.cancel"]);

        var proceed = result switch
        {
            Wpf.Ui.Controls.MessageBoxResult.Primary => await _vm.SaveAllModifiedAsync(),
            Wpf.Ui.Controls.MessageBoxResult.Secondary => true,
            _ => false
        };

        if (proceed)
        {
            _forceClose = true;
            Close();
        }
    }

    private void UpdateFoldings()
    {
        if (Editor.Document is not null)
            _foldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (!ctrl) return;

        switch (e.Key)
        {
            case Key.P when shift: ShowCommandPalette(); e.Handled = true; break;
            case Key.P: ShowQuickOpen(); e.Handled = true; break;
            case Key.O when shift: ShowSymbols(); e.Handled = true; break;
            case Key.F when shift: ProjectSearch.Show(); e.Handled = true; break;
            case Key.F: FindPanel.Show(replaceMode: false); e.Handled = true; break;
            case Key.H: FindPanel.Show(replaceMode: true); e.Handled = true; break;
            case Key.G: GoToPanel.Show(); e.Handled = true; break;
            case Key.S: _vm.SaveCommand.Execute(null); e.Handled = true; break;
            case Key.O: _vm.OpenCommand.Execute(null); e.Handled = true; break;
            case Key.N: _vm.NewDocumentCommand.Execute(null); e.Handled = true; break;
            case Key.OemPlus or Key.Add: ZoomEditor(+1); e.Handled = true; break;
            case Key.OemMinus or Key.Subtract: ZoomEditor(-1); e.Handled = true; break;
            case Key.D0 or Key.NumPad0: Editor.FontSize = 14; e.Handled = true; break;
        }
    }

    // ===== Editor zoom =====

    private void ZoomEditor(int direction)
        => Editor.FontSize = Math.Clamp(Editor.FontSize + direction, 8, 40);

    private void OnEditorMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        ZoomEditor(e.Delta > 0 ? +1 : -1);
        e.Handled = true;
    }

    // ===== Smart paste + image paste =====

    private void OnEditorPaste(object sender, DataObjectPastingEventArgs e)
    {
        // لصق صورة → حفظها في مجلد images وإدراج رابط.
        if (Clipboard.ContainsImage() && TrySaveClipboardImage() is { } rel)
        {
            e.CancelCommand();
            InsertAtCaret($"![]({rel})");
            return;
        }

        // لصق نص: إن كان رابطاً وهناك تحديد → اجعله رابط Markdown.
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            var text = (e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty).Trim();
            if (Editor.SelectionLength > 0 && IsUrl(text))
            {
                e.CancelCommand();
                var sel = Editor.SelectedText;
                Editor.Document.Replace(Editor.SelectionStart, Editor.SelectionLength, $"[{sel}]({text})");
            }
        }
    }

    private static bool IsUrl(string text)
        => !text.Contains('\n')
           && Uri.TryCreate(text, UriKind.Absolute, out var u)
           && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    /// <summary>يحفظ صورة الحافظة في مجلد images بجوار المستند ويعيد المسار النسبي.</summary>
    private string? TrySaveClipboardImage()
    {
        var docPath = _vm.ActiveDocument?.FilePath;
        var baseDir = docPath is not null ? Path.GetDirectoryName(docPath) : _vm.WorkspacePath;
        if (baseDir is null)
        {
            _vm.ReportStatus(Localizer.Instance["status.saveFirstImages"]);
            return null;
        }

        try
        {
            var img = Clipboard.GetImage();
            if (img is null) return null;

            var imagesDir = Path.Combine(baseDir, "images");
            Directory.CreateDirectory(imagesDir);
            var name = $"pasted-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            var full = Path.Combine(imagesDir, name);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(img));
            using (var fs = File.Create(full)) encoder.Save(fs);

            return $"images/{name}";
        }
        catch (Exception ex)
        {
            _vm.ReportStatus(Localizer.Instance.Format("status.imageSaveFailed", ex.Message));
            return null;
        }
    }

    // ===== Command Palette / Quick Open / Symbols =====

    private void ShowCommandPalette()
    {
        PaletteItem Cmd(string title, Action action) => new() { Title = title, Invoke = action };

        var items = new List<PaletteItem>
        {
            Cmd("مستند: جديد", () => _vm.NewDocumentCommand.Execute(null)),
            Cmd("ملف: فتح…", () => _vm.OpenCommand.Execute(null)),
            Cmd("مجلد: فتح مساحة عمل…", () => _vm.OpenFolderCommand.Execute(null)),
            Cmd("مجلد: إغلاق مساحة العمل", () => _vm.CloseFolderCommand.Execute(null)),
            Cmd("ملف: حفظ", () => _vm.SaveCommand.Execute(null)),
            Cmd("ملف: حفظ باسم…", () => _vm.SaveAsCommand.Execute(null)),
            Cmd("تصدير: HTML", () => _vm.ExportHtmlCommand.Execute(null)),
            Cmd("تصدير: PDF", () => OnExportPdf(this, new RoutedEventArgs())),
            Cmd("تصدير: Word (DOCX)", () => _vm.ExportDocxCommand.Execute(null)),
            Cmd("تصدير: نص عادي", () => _vm.ExportTextCommand.Execute(null)),
            Cmd("عرض: الوضع المرئي (استوديو WYSIWYG)", () => _vm.ToggleVisualModeCommand.Execute(null)),
            Cmd("عرض: وضع التركيز", () => _vm.ToggleFocusModeCommand.Execute(null)),
            Cmd("عرض: وضع القراءة", () => _vm.ToggleReadingModeCommand.Execute(null)),
            Cmd("عرض: إظهار/إخفاء المستكشف", () => _vm.ToggleSidebarCommand.Execute(null)),
            Cmd("عرض: إظهار/إخفاء المعاينة", () => _vm.TogglePreviewPaneCommand.Execute(null)),
            Cmd("Markdown: إدراج جدول محتويات", () => _vm.InsertTocCommand.Execute(null)),
            Cmd("Markdown: تنسيق المستند", () => _vm.FormatDocumentCommand.Execute(null)),
            Cmd("Markdown: ترقيم العناوين تلقائياً", () => _vm.AutoNumberHeadingsCommand.Execute(null)),
            Cmd("Markdown: فحص الروابط المكسورة", () => _vm.CheckLinksCommand.Execute(null)),
            Cmd("تحرير: بحث", () => FindPanel.Show(false)),
            Cmd("تحرير: استبدال", () => FindPanel.Show(true)),
            Cmd("بحث: في كل ملفات المشروع…", () => ProjectSearch.Show()),
            Cmd("انتقال: إلى سطر…", () => GoToPanel.Show()),
            Cmd("انتقال: إلى رمز…", ShowSymbols),
            Cmd("ملف: فتح سريع…", ShowQuickOpen),
            Cmd("Windows: تسجيل تكامل النظام", () => _vm.RegisterWindowsIntegrationCommand.Execute(null)),
            Cmd("Windows: إلغاء تكامل النظام", () => _vm.UnregisterWindowsIntegrationCommand.Execute(null)),
            Cmd(Localizer.Instance["toolbar.whatsNew"], () => OnOpenWhatsNew(this, new RoutedEventArgs())),
        };

        // ثيمات: أمر لكل قالب.
        var themeSvc = App.Services.GetRequiredService<IThemeService>();
        foreach (var preset in themeSvc.Presets)
        {
            var id = preset.Id;
            items.Add(Cmd($"{Localizer.Instance["toolbar.theme"]}: {Localizer.Instance[preset.NameKey]}",
                () => { themeSvc.ApplyPreset(id); BuildThemeMenu();
                        var s = App.Services.GetRequiredService<ISettingsService>();
                        s.Settings.ThemeId = id; s.Save(); }));
        }
        Palette.Show(items, "اكتب اسم أمر…");
    }

    private void ShowQuickOpen()
    {
        if (_vm.WorkspacePath is null)
        {
            _vm.ReportStatus(Localizer.Instance["status.quickOpenNeedsFolder"]);
            return;
        }

        var ws = (IWorkspaceService)App.Services.GetService(typeof(IWorkspaceService))!;
        var root = _vm.WorkspacePath;
        var items = ws.EnumerateMarkdownFiles(root)
            .Select(path => new PaletteItem
            {
                Title = Path.GetFileName(path),
                Subtitle = Path.GetRelativePath(root, path),
                SearchKey = path,
                Invoke = async () => await _vm.OpenPathAsync(path)
            })
            .ToList();

        Palette.Show(items, "اكتب اسم ملف…");
    }

    private void ShowSymbols()
    {
        var content = _vm.ActiveDocument?.Content;
        if (string.IsNullOrEmpty(content))
        {
            _vm.ReportStatus(Localizer.Instance["status.noActiveDoc"]);
            return;
        }

        var items = OutlineService.ParseFlat(content)
            .Select(h => new PaletteItem
            {
                Title = new string('#', h.Level) + " " + h.Title,
                Subtitle = $"سطر {h.Line}",
                SearchKey = h.Title,
                Invoke = () => JumpToLine(h.Line)
            })
            .ToList();

        Palette.Show(items, "انتقل إلى عنوان…");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await Preview.EnsureCoreWebView2Async();
        _webViewReady = true;
        Preview.CoreWebView2.WebMessageReceived += OnPreviewWebMessage;
        BindActiveDocument();          // يحمّل المستند الأولي
        PushPreview(_vm.PreviewHtml);
        UpdateFoldings();
    }

    // ===== ViewModel -> View =====

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.ActiveDocument):
                BindActiveDocument();
                break;
            case nameof(MainWindowViewModel.PreviewHtml):
                PushPreview(_vm.PreviewHtml);
                break;
            case nameof(MainWindowViewModel.IsSidebarVisible):
            case nameof(MainWindowViewModel.IsPreviewVisible):
            case nameof(MainWindowViewModel.IsEditorVisible):
                ApplyPanelLayout();
                break;
        }
    }

    /// <summary>يضبط أعمدة اللوحات وفق رؤية كل لوحة (تحكّم dock-like).</summary>
    private void ApplyPanelLayout()
    {
        var sidebar = _vm.IsSidebarVisible;
        var editor = _vm.IsEditorVisible;
        var preview = _vm.IsPreviewVisible;

        Sidebar.Visibility = sidebar ? Visibility.Visible : Visibility.Collapsed;
        SidebarColumn.Width = sidebar ? new GridLength(260) : new GridLength(0);
        SidebarSplitterColumn.Width = sidebar ? GridLength.Auto : new GridLength(0);

        EditorPane.Visibility = editor ? Visibility.Visible : Visibility.Collapsed;
        EditorColumn.Width = editor ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        PreviewPane.Visibility = preview ? Visibility.Visible : Visibility.Collapsed;
        PreviewColumn.Width = preview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        // السبليتر بين المحرّر والمعاينة يظهر فقط عند وجود الاثنين.
        PreviewSplitterColumn.Width = (editor && preview) ? GridLength.Auto : new GridLength(0);
    }

    /// <summary>يربط محتوى المحرّر بالمستند النشط الحالي.</summary>
    private void BindActiveDocument()
    {
        if (ReferenceEquals(_boundDocument, _vm.ActiveDocument)) return;

        if (_boundDocument is not null)
            _boundDocument.PropertyChanged -= OnBoundDocumentPropertyChanged;

        _boundDocument = _vm.ActiveDocument;
        _suppressEditorSync = true;
        Editor.Text = _boundDocument?.Content ?? string.Empty;
        Editor.IsEnabled = _boundDocument is not null;
        _suppressEditorSync = false;

        if (_boundDocument is not null)
            _boundDocument.PropertyChanged += OnBoundDocumentPropertyChanged;

        UpdateFoldings();
    }

    /// <summary>يعكس تعديلات المحتوى الآتية من خارج المحرّر (كإعادة التحميل التلقائية).</summary>
    private void OnBoundDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MarkdownDocument.Content) || _boundDocument is null) return;
        if (Editor.Text == _boundDocument.Content) return;   // التغيير صادر من المحرّر نفسه

        var caret = Editor.CaretOffset;
        _suppressEditorSync = true;
        Editor.Text = _boundDocument.Content;
        Editor.CaretOffset = Math.Min(caret, Editor.Document.TextLength);
        _suppressEditorSync = false;
        UpdateFoldings();
    }

    private void PushPreview(string html)
    {
        if (!_webViewReady) return;
        if (_vm.IsVisualMode) return;   // في الوضع المرئي المعاينة هي المصدر — لا تُعاد كتابتها
        Preview.NavigateToString(string.IsNullOrEmpty(html) ? "<html><body></body></html>" : html);
    }

    // ===== الوضع المرئي (WYSIWYG) =====

    private void ApplyVisualMode(bool visual)
    {
        if (!_webViewReady) return;

        if (visual)
        {
            Editor.IsReadOnly = true;                       // لوحة الكود للعرض فقط
            RichToolbar.Visibility = Visibility.Visible;
            Preview.NavigateToString(_vm.BuildEditableHtml());
            _vm.ReportStatus(Localizer.Instance["status.visualMode"]);
        }
        else
        {
            Editor.IsReadOnly = false;
            RichToolbar.Visibility = Visibility.Collapsed;
            Preview.NavigateToString(_vm.PreviewHtml);      // عُد لمعاينة عادية للقراءة
            _vm.ReportStatus(Localizer.Instance["status.codeMode"]);
        }
    }

    private void OnPreviewWebMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!_vm.IsVisualMode) return;
        string json;
        try { json = e.TryGetWebMessageAsString(); } catch { return; }
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var t)) return;
            switch (t.GetString())
            {
                case "html" when doc.RootElement.TryGetProperty("html", out var h):
                    _vm.ApplyVisualEdit(h.GetString() ?? string.Empty);
                    break;
                case "caret" when doc.RootElement.TryGetProperty("blockIndex", out var b):
                    HighlightCodeLine(_vm.BlockLineFromVisual(b.GetInt32()));
                    break;
            }
        }
        catch { /* رسالة غير متوقّعة: تجاهل */ }
    }

    /// <summary>يمرّر ويُظلّل سطر الكود المقابل لموضع المؤشّر المرئي (بلا سرقة التركيز).</summary>
    private void HighlightCodeLine(int line)
    {
        if (line < 1 || line > Editor.Document.LineCount) return;
        var docLine = Editor.Document.GetLineByNumber(line);
        Editor.ScrollToLine(line);
        Editor.Select(docLine.Offset, docLine.Length);   // تحديد خامل يظهر الموضع دون تركيز
    }

    /// <summary>يرسل أمر تنسيق إلى المعاينة القابلة للتحرير.</summary>
    private void SendVisualCommand(string cmd, string? value = null)
    {
        if (!_webViewReady || !_vm.IsVisualMode) return;
        var payload = value is null
            ? $"{{\"cmd\":\"{cmd}\"}}"
            : $"{{\"cmd\":\"{cmd}\",\"value\":{System.Text.Json.JsonSerializer.Serialize(value)}}}";
        Preview.CoreWebView2.PostWebMessageAsString(payload);
    }

    private void SendVisualFontSize(int size)
    {
        if (!_webViewReady || !_vm.IsVisualMode) return;
        Preview.CoreWebView2.PostWebMessageAsString($"{{\"fontSize\":{size}}}");
    }

    private int _visualFontSize = 15;

    // معالجات أزرار شريط الأدوات الغني
    private void RtBold(object s, RoutedEventArgs e) => SendVisualCommand("bold");
    private void RtItalic(object s, RoutedEventArgs e) => SendVisualCommand("italic");
    private void RtStrike(object s, RoutedEventArgs e) => SendVisualCommand("strikeThrough");
    private void RtH1(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "H1");
    private void RtH2(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "H2");
    private void RtH3(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "H3");
    private void RtParagraph(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "P");
    private void RtQuote(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "BLOCKQUOTE");
    private void RtCode(object s, RoutedEventArgs e) => SendVisualCommand("formatBlock", "PRE");
    private void RtUl(object s, RoutedEventArgs e) => SendVisualCommand("insertUnorderedList");
    private void RtOl(object s, RoutedEventArgs e) => SendVisualCommand("insertOrderedList");
    private void RtLink(object s, RoutedEventArgs e) => SendVisualCommand("link");
    private void RtImage(object s, RoutedEventArgs e) => SendVisualCommand("image");
    private void RtHr(object s, RoutedEventArgs e) => SendVisualCommand("insertHorizontalRule");
    private void RtTable(object s, RoutedEventArgs e) => SendVisualCommand("table");
    private void RtUndo(object s, RoutedEventArgs e) => SendVisualCommand("undo");
    private void RtRedo(object s, RoutedEventArgs e) => SendVisualCommand("redo");
    private void RtInlineCode(object s, RoutedEventArgs e) => SendVisualCommand("inlineCode");
    private void RtTaskList(object s, RoutedEventArgs e) => SendVisualCommand("tasklist");
    private void RtUnlink(object s, RoutedEventArgs e) => SendVisualCommand("unlink");
    private void RtIndent(object s, RoutedEventArgs e) => SendVisualCommand("indent");
    private void RtOutdent(object s, RoutedEventArgs e) => SendVisualCommand("outdent");
    private void RtClear(object s, RoutedEventArgs e) => SendVisualCommand("removeFormat");
    private void RtFontUp(object s, RoutedEventArgs e) => SendVisualFontSize(_visualFontSize = Math.Min(_visualFontSize + 1, 40));
    private void RtFontDown(object s, RoutedEventArgs e) => SendVisualFontSize(_visualFontSize = Math.Max(_visualFontSize - 1, 8));

    // ===== View -> ViewModel =====

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_suppressEditorSync || _boundDocument is null) return;
        _boundDocument.Content = Editor.Text;
        _boundDocument.IsModified = true;
        _foldingTimer.Stop();
        _foldingTimer.Start();
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        var caret = Editor.TextArea.Caret;
        CaretStatus.Text = Localizer.Instance.Format("status.caret", caret.Line, caret.Column);
    }

    // ===== PDF Export (WebView2 خارج الشاشة) =====

    private async void OnExportPdf(object sender, RoutedEventArgs e)
    {
        var prep = _vm.PreparePdfExport();
        if (prep is not { } req) return;   // لا مستند أو أُلغي الحوار

        _vm.ReportStatus(Localizer.Instance["status.exportingPdf"]);
        var web = new WebView2();
        ExportLayer.Children.Add(web);
        try
        {
            await web.EnsureCoreWebView2Async();

            var loaded = new TaskCompletionSource();
            void OnNav(object? _, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs __)
                => loaded.TrySetResult();
            web.NavigationCompleted += OnNav;
            web.NavigateToString(req.Html);
            await loaded.Task;
            web.NavigationCompleted -= OnNav;

            await web.CoreWebView2.PrintToPdfAsync(req.Path);
            _vm.ReportStatus(Localizer.Instance.Format("status.exported", Path.GetFileName(req.Path)));
        }
        catch (Exception ex)
        {
            _vm.ReportStatus(Localizer.Instance.Format("status.exportPdfFailed", ex.Message));
        }
        finally
        {
            ExportLayer.Children.Remove(web);
            web.Dispose();
        }
    }

    // ===== Drag & Drop =====

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
        foreach (var path in paths)
        {
            if (Directory.Exists(path)) await _vm.OpenFolderPathAsync(path);   // مجلد → مساحة عمل
            else if (File.Exists(path)) await _vm.OpenPathAsync(path);          // ملف → تبويب
        }
    }
}
