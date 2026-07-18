using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkdownStudio.Models;
using MarkdownStudio.Services;

namespace MarkdownStudio.ViewModels;

/// <summary>الـ ViewModel الرئيسي: يدير المستندات المفتوحة، المعاينة، والثيم.</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileService _files;
    private readonly IMarkdownRenderer _renderer;
    private readonly IThemeService _theme;
    private readonly IRecentFilesService _recent;
    private readonly IFileWatcherService _watcher;
    private readonly IExportService _export;
    private readonly IWorkspaceService _workspace;
    private readonly IWindowsIntegrationService _windows;

    public MainWindowViewModel(
        IFileService files,
        IMarkdownRenderer renderer,
        IThemeService theme,
        IRecentFilesService recent,
        IFileWatcherService watcher,
        IExportService export,
        IWorkspaceService workspace,
        IWindowsIntegrationService windows)
    {
        _files = files;
        _renderer = renderer;
        _theme = theme;
        _recent = recent;
        _watcher = watcher;
        _export = export;
        _workspace = workspace;
        _windows = windows;
        _theme.ThemeChanged += (_, _) => RefreshPreview();
        _watcher.FileChanged += OnFileChangedExternally;
        ((System.Collections.Specialized.INotifyCollectionChanged)_recent.Files).CollectionChanged
            += (_, _) => _windows.UpdateJumpList(_recent.Files);
        _windows.UpdateJumpList(_recent.Files);
        Localization.Localizer.Instance.LanguageChanged += (_, _) => OnLanguageChanged();

        NewDocument();
    }

    private static string L(string key) => Localization.Localizer.Instance[key];
    private static string Lf(string key, params object[] args) => Localization.Localizer.Instance.Format(key, args);

    /// <summary>يحدّث النصوص المشتقّة عند تبديل اللغة.</summary>
    private void OnLanguageChanged()
    {
        UpdateWordCount();
        StatusText = L("status.ready");
    }

    public bool IsWindowsIntegrationRegistered => _windows.IsRegistered;
    public bool IsPortable => _windows.IsPortable;

    [RelayCommand]
    private void RegisterWindowsIntegration()
    {
        _windows.RegisterIntegration();
        OnPropertyChanged(nameof(IsWindowsIntegrationRegistered));
        StatusText = _windows.IsPortable ? L("status.winPortable") : L("status.winRegistered");
    }

    [RelayCommand]
    private void UnregisterWindowsIntegration()
    {
        _windows.UnregisterIntegration();
        OnPropertyChanged(nameof(IsWindowsIntegrationRegistered));
        StatusText = L("status.winUnregistered");
    }

    /// <summary>يطلب من الـ View نقل المؤشّر إلى سطر معيّن وتركيز المحرّر.</summary>
    public event Action<int>? RequestJumpToLine;

    /// <summary>يطلب من الـ View إدراج نص في موضع المؤشّر.</summary>
    public event Action<string>? RequestInsertText;

    /// <summary>يطلب من الـ View تفعيل/تعطيل وضع التحرير المرئي (WYSIWYG).</summary>
    public event Action<bool>? RequestVisualMode;

    private readonly ReverseMarkdown.Converter _htmlToMd = new(new ReverseMarkdown.Config
    {
        GithubFlavored = true
    });

    /// <summary>المستندات المفتوحة (تبويبات).</summary>
    public ObservableCollection<MarkdownDocument> Documents { get; } = new();

    /// <summary>الملفات المفتوحة مؤخراً (للربط بقائمة Recent).</summary>
    public ReadOnlyObservableCollection<string> RecentFiles => _recent.Files;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
    private MarkdownDocument? _activeDocument;

    /// <summary>HTML المعاينة الحالية (يُربط بـ WebView2).</summary>
    [ObservableProperty]
    private string _previewHtml = string.Empty;

    /// <summary>نص شريط الحالة (يمين).</summary>
    [ObservableProperty]
    private string _statusText = Localization.Localizer.Instance["status.ready"];

    /// <summary>رؤية شاشة الترحيب (تظهر عند عدم وجود مستند مفتوح).</summary>
    public Visibility WelcomeVisibility => ActiveDocument is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>رؤية منطقة المحرّر/المعاينة (معكوسة عن الترحيب).</summary>
    public Visibility WorkspaceVisibility => ActiveDocument is null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>يضبطه الـ View: يؤكّد إغلاق مستند معدّل (true = تابع الإغلاق).</summary>
    public Func<MarkdownDocument, Task<bool>>? ConfirmCloseAsync { get; set; }

    /// <summary>عقدة جذر مجلد العمل (null إن لم يُفتح مجلد).</summary>
    [ObservableProperty] private FileSystemItem? _workspaceRoot;

    /// <summary>مسار مجلد العمل الحالي.</summary>
    [ObservableProperty] private string? _workspacePath;

    /// <summary>مخطّط المستند النشط (شجرة العناوين).</summary>
    public ObservableCollection<OutlineItem> Outline { get; } = new();

    /// <summary>إحصاء كلمات/أحرف/أسطر المستند النشط (لشريط الحالة).</summary>
    [ObservableProperty] private string _wordCountText = string.Empty;

    /// <summary>وضع التركيز (إخفاء اللوحات الجانبية).</summary>
    [ObservableProperty] private bool _isFocusMode;

    /// <summary>وضع القراءة (المعاينة فقط).</summary>
    [ObservableProperty] private bool _isReadingMode;

    // رؤية اللوحات (يقرأها الـ View لضبط الأعمدة).
    [ObservableProperty] private bool _isSidebarVisible = true;
    [ObservableProperty] private bool _isPreviewVisible = true;
    [ObservableProperty] private bool _isEditorVisible = true;

    partial void OnIsFocusModeChanged(bool value) => ApplyLayoutFromModes();
    partial void OnIsReadingModeChanged(bool value) => ApplyLayoutFromModes();

    /// <summary>يشتقّ رؤية اللوحات من الأوضاع (تركيز/قراءة/عادي).</summary>
    private void ApplyLayoutFromModes()
    {
        if (IsFocusMode)      { IsSidebarVisible = false; IsEditorVisible = true;  IsPreviewVisible = false; }
        else if (IsReadingMode){ IsSidebarVisible = false; IsEditorVisible = false; IsPreviewVisible = true;  }
        else                  { IsSidebarVisible = true;  IsEditorVisible = true;  IsPreviewVisible = true;  }
    }

    /// <summary>مسار التنقّل للمستند النشط (نسبةً لمساحة العمل).</summary>
    [ObservableProperty] private string _breadcrumb = string.Empty;

    private void RefreshBreadcrumb()
    {
        if (ActiveDocument?.FilePath is not { } path) { Breadcrumb = ActiveDocument?.Title ?? string.Empty; return; }
        var display = WorkspacePath is { } root && path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(root, path)
            : path;
        Breadcrumb = display.Replace(Path.DirectorySeparatorChar, '›');
    }

    private void RefreshOutline()
    {
        Outline.Clear();
        if (ActiveDocument is null) { WordCountText = string.Empty; return; }

        foreach (var item in OutlineService.BuildTree(ActiveDocument.Content))
            Outline.Add(item);

        UpdateWordCount();
    }

    private void UpdateWordCount()
    {
        var text = ActiveDocument?.Content ?? string.Empty;
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var chars = text.Length;
        var lines = text.Length == 0 ? 0 : text.Split('\n').Length;
        var minutes = Math.Max(1, (int)Math.Ceiling(words / 200.0));
        WordCountText = Lf("status.wordcount", words, chars, lines, minutes);
    }

    partial void OnActiveDocumentChanged(MarkdownDocument? oldValue, MarkdownDocument? newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnActiveDocPropertyChanged;
        if (newValue is not null)
            newValue.PropertyChanged += OnActiveDocPropertyChanged;
        _watcher.Watch(newValue?.FilePath);
        OnPropertyChanged(nameof(WelcomeVisibility));
        OnPropertyChanged(nameof(WorkspaceVisibility));
        RefreshPreview();
        RefreshOutline();
        RefreshBreadcrumb();
    }

    /// <summary>يُعاد تحميل الملف عند تعديله خارجياً إن لم تكن هناك تغييرات محلية.</summary>
    private async void OnFileChangedExternally(object? sender, string path)
    {
        var doc = Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (doc is null) return;

        if (doc.IsModified)
        {
            StatusText = Lf("status.changedExternally", Path.GetFileName(path));
            return;
        }

        try
        {
            var reloaded = await _files.OpenAsync(path);
            doc.Content = reloaded.Content;   // يحدّث المحرّر والمعاينة عبر الربط
            doc.IsModified = false;
            StatusText = Lf("status.reloaded", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusText = Lf("status.reloadFailed", ex.Message);
        }
    }

    private void OnActiveDocPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarkdownDocument.Content))
        {
            RefreshPreview();
            RefreshOutline();
        }
    }

    private void RefreshPreview()
    {
        if (ActiveDocument is null)
        {
            PreviewHtml = string.Empty;
            return;
        }

        var baseDir = ActiveDocument.FilePath is { } p ? Path.GetDirectoryName(p) : null;
        PreviewHtml = _renderer.RenderDocument(ActiveDocument.Content, _theme.IsDark, baseDir);
    }

    // ---- Commands ----

    [RelayCommand]
    private void NewDocument()
    {
        var doc = new MarkdownDocument { Content = L("doc.newContent") };
        Documents.Add(doc);
        ActiveDocument = doc;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var path = _files.ShowOpenDialog();
        if (path is null) return;
        await OpenPathAsync(path);
    }

    /// <summary>يفتح مساراً معيّناً (يُستخدم أيضاً من Drag&amp;Drop وسطر الأوامر).</summary>
    public async Task OpenPathAsync(string path)
    {
        var existing = Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) { ActiveDocument = existing; return; }

        try
        {
            var doc = await _files.OpenAsync(path);
            Documents.Add(doc);
            ActiveDocument = doc;
            _recent.Add(path);
            _windows.AddToRecentDocs(path);
            StatusText = Lf("status.opened", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _recent.Remove(path);
            StatusText = Lf("status.openFailed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenRecentAsync(string? path)
    {
        if (!string.IsNullOrEmpty(path))
            await OpenPathAsync(path);
    }

    private bool CanSave() => ActiveDocument is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (ActiveDocument is not null)
            await SaveDocumentAsync(ActiveDocument);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsAsync()
    {
        if (ActiveDocument is not null)
            await SaveDocumentAsync(ActiveDocument, forceDialog: true);
    }

    /// <summary>يحفظ مستنداً؛ يعرض حوار "حفظ باسم" عند غياب المسار. يعيد false إن أُلغي.</summary>
    public async Task<bool> SaveDocumentAsync(MarkdownDocument doc, bool forceDialog = false)
    {
        var needsDialog = forceDialog || string.IsNullOrEmpty(doc.FilePath);
        if (needsDialog)
        {
            var name = doc.FilePath is { } p ? Path.GetFileName(p) : "Untitled.md";
            var path = _files.ShowSaveDialog(name);
            if (path is null) return false;
            await _files.SaveAsAsync(doc, path);
            _recent.Add(path);
            if (ReferenceEquals(doc, ActiveDocument)) _watcher.Watch(path);
        }
        else
        {
            await _files.SaveAsync(doc);
        }
        StatusText = Lf("status.saved", Path.GetFileName(doc.FilePath));
        return true;
    }

    /// <summary>يحفظ كل المستندات المعدّلة. يعيد false إن ألغى المستخدم أحد الحوارات.</summary>
    public async Task<bool> SaveAllModifiedAsync()
    {
        foreach (var doc in Documents.Where(d => d.IsModified).ToList())
            if (!await SaveDocumentAsync(doc))
                return false;
        return true;
    }

    /// <summary>عدد المستندات ذات التعديلات غير المحفوظة.</summary>
    public int ModifiedCount => Documents.Count(d => d.IsModified);

    [RelayCommand]
    private async Task CloseDocumentAsync(MarkdownDocument? doc)
    {
        doc ??= ActiveDocument;
        if (doc is null) return;

        if (doc.IsModified && ConfirmCloseAsync is not null && !await ConfirmCloseAsync(doc))
            return;   // ألغى المستخدم

        var index = Documents.IndexOf(doc);
        Documents.Remove(doc);
        if (ReferenceEquals(doc, ActiveDocument))
            ActiveDocument = Documents.Count == 0 ? null
                : Documents[Math.Min(index, Documents.Count - 1)];
    }

    [RelayCommand]
    private void TogglePin(MarkdownDocument? doc)
    {
        if (doc is not null) doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private async Task CloseOthersAsync(MarkdownDocument? keep)
    {
        keep ??= ActiveDocument;
        foreach (var doc in Documents.Where(d => !ReferenceEquals(d, keep) && !d.IsPinned).ToList())
            await CloseDocumentAsync(doc);
    }

    [RelayCommand]
    private async Task CloseAllAsync()
    {
        foreach (var doc in Documents.Where(d => !d.IsPinned).ToList())
            await CloseDocumentAsync(doc);
    }

    // ---- Workspace / Explorer ----

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var path = _files.ShowFolderDialog();
        if (path is not null) await OpenFolderPathAsync(path);
    }

    /// <summary>يفتح مجلداً كمساحة عمل (يُستخدم أيضاً من سحب المجلد وسطر الأوامر).</summary>
    public async Task OpenFolderPathAsync(string path)
    {
        try
        {
            WorkspaceRoot = await _workspace.OpenFolderAsync(path);
            WorkspacePath = path;
            StatusText = Lf("status.workspace", Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)));
        }
        catch (Exception ex)
        {
            StatusText = Lf("status.folderFailed", ex.Message);
        }
    }

    [RelayCommand]
    private void CloseFolder()
    {
        WorkspaceRoot = null;
        WorkspacePath = null;
    }

    /// <summary>تفعيل عنصر في الشجرة: فتح ملف أو طيّ/فرد مجلد.</summary>
    [RelayCommand]
    private async Task ActivateItemAsync(FileSystemItem? item)
    {
        if (item is null) return;
        if (item.IsDirectory) { item.IsExpanded = !item.IsExpanded; return; }
        await OpenPathAsync(item.FullPath);
    }

    [RelayCommand]
    private void RefreshItem(FileSystemItem? item)
    {
        var target = item is { IsDirectory: true } ? item : WorkspaceRoot;
        if (target is not null) _workspace.Refresh(target);
    }


    [RelayCommand]
    private void NewFileIn(FileSystemItem? folder)
    {
        var dir = FolderPathOf(folder);
        if (dir is null) return;
        var created = _workspace.CreateFile(dir, "بدون عنوان.md");
        RefreshParentOf(created);
        StatusText = Lf("status.created", Path.GetFileName(created));
    }

    [RelayCommand]
    private void NewFolderIn(FileSystemItem? folder)
    {
        var dir = FolderPathOf(folder);
        if (dir is null) return;
        var created = _workspace.CreateFolder(dir, "مجلد جديد");
        RefreshParentOf(created);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(FileSystemItem? item)
    {
        if (item is null || ConfirmDeleteAsync is null) return;
        if (!await ConfirmDeleteAsync(item)) return;
        try
        {
            _workspace.Delete(item.FullPath);
            RefreshParentOf(item.FullPath);
            StatusText = Lf("status.deleted", item.Name);
        }
        catch (Exception ex) { StatusText = Lf("status.deleteFailed", ex.Message); }
    }

    [RelayCommand]
    private void DuplicateItem(FileSystemItem? item)
    {
        if (item is null) return;
        var copy = _workspace.Duplicate(item.FullPath);
        RefreshParentOf(copy);
        StatusText = Lf("status.duplicated", Path.GetFileName(copy));
    }

    /// <summary>يطبّق إعادة تسمية بعد تحرير الاسم في الشجرة.</summary>
    public void CommitRename(FileSystemItem item, string newName)
    {
        item.IsRenaming = false;
        newName = newName.Trim();
        if (string.IsNullOrEmpty(newName) || newName == item.Name) return;
        try
        {
            var newPath = _workspace.Rename(item.FullPath, newName);
            RefreshParentOf(newPath);
        }
        catch (Exception ex) { StatusText = Lf("status.renameFailed", ex.Message); }
    }

    /// <summary>يضبطه الـ View لتأكيد الحذف عبر حوار.</summary>
    public Func<FileSystemItem, Task<bool>>? ConfirmDeleteAsync { get; set; }

    private static string? FolderPathOf(FileSystemItem? item) => item switch
    {
        { IsDirectory: true } => item.FullPath,
        not null => Path.GetDirectoryName(item.FullPath),
        _ => null
    };

    private void RefreshParentOf(string path)
    {
        var parent = Path.GetDirectoryName(path);
        var node = FindNode(WorkspaceRoot, parent);
        if (node is not null) { _workspace.Refresh(node); node.IsExpanded = true; }
        else if (WorkspaceRoot is not null) _workspace.Refresh(WorkspaceRoot);
    }

    private static FileSystemItem? FindNode(FileSystemItem? node, string? fullPath)
    {
        if (node is null || fullPath is null) return null;
        if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase)) return node;
        foreach (var child in node.Children)
            if (child.IsDirectory && FindNode(child, fullPath) is { } found)
                return found;
        return null;
    }

    // ---- Outline / Navigation ----

    [RelayCommand]
    private void JumpToOutline(OutlineItem? item)
    {
        if (item is not null) RequestJumpToLine?.Invoke(item.Line);
    }

    [RelayCommand]
    private void InsertToc()
    {
        if (ActiveDocument is null) return;
        var toc = OutlineService.GenerateToc(ActiveDocument.Content);
        if (toc.Length > 0) RequestInsertText?.Invoke(toc + "\n");
    }

    [RelayCommand]
    private void ToggleFocusMode() => IsFocusMode = !IsFocusMode;

    [RelayCommand]
    private void ToggleReadingMode() => IsReadingMode = !IsReadingMode;

    /// <summary>وضع التحرير المرئي (WYSIWYG): تحرير على المعاينة وتوليد الكود.</summary>
    [ObservableProperty] private bool _isVisualMode;

    partial void OnIsVisualModeChanged(bool value) => RequestVisualMode?.Invoke(value);

    [RelayCommand]
    private void ToggleVisualMode() => IsVisualMode = !IsVisualMode;

    /// <summary>يبني HTML قابلاً للتحرير للمستند النشط (يستدعيه الـ View عند دخول الوضع المرئي).</summary>
    public string BuildEditableHtml()
    {
        if (ActiveDocument is null) return "<!DOCTYPE html><html><body></body></html>";
        var baseDir = ActiveDocument.FilePath is { } p ? Path.GetDirectoryName(p) : null;
        return _renderer.RenderEditable(ActiveDocument.Content, _theme.IsDark, baseDir);
    }

    /// <summary>يعيد رقم سطر الكود المقابل للكتلة رقم <paramref name="blockIndex"/> (لمزامنة المؤشر).</summary>
    public int BlockLineFromVisual(int blockIndex)
        => ActiveDocument is null ? 1 : _renderer.GetBlockLine(ActiveDocument.Content, blockIndex);

    /// <summary>يستقبل HTML المُحرَّر من المعاينة، يحوّله إلى Markdown، ويحدّث المستند.</summary>
    public void ApplyVisualEdit(string html)
    {
        if (ActiveDocument is null) return;
        try
        {
            var markdown = _htmlToMd.Convert(html).Trim() + "\n";
            if (markdown != ActiveDocument.Content)
                ActiveDocument.Content = markdown;   // يحدّث لوحة الكود؛ المعاينة لا تُعاد (Guard في الـ View)
        }
        catch (Exception ex) { StatusText = Lf("status.convertFailed", ex.Message); }
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;

    [RelayCommand]
    private void TogglePreviewPane() => IsPreviewVisible = !IsPreviewVisible;

    [RelayCommand]
    private void FormatDocument()
    {
        if (ActiveDocument is null) return;
        ActiveDocument.Content = MarkdownTools.Format(ActiveDocument.Content);
        StatusText = L("status.formatted");
    }

    [RelayCommand]
    private void AutoNumberHeadings()
    {
        if (ActiveDocument is null) return;
        ActiveDocument.Content = MarkdownTools.AutoNumberHeadings(ActiveDocument.Content);
        StatusText = L("status.renumbered");
    }

    [RelayCommand]
    private void CheckLinks()
    {
        if (ActiveDocument?.FilePath is not { } path)
        {
            StatusText = L("status.saveFirstLinks");
            return;
        }
        var broken = MarkdownTools.FindBrokenLinks(ActiveDocument.Content, Path.GetDirectoryName(path));
        StatusText = broken.Count == 0
            ? L("status.linksOk")
            : Lf("status.linksBroken", broken.Count, broken[0].Target, broken[0].Line);
        if (broken.Count > 0) RequestJumpToLine?.Invoke(broken[0].Line);
    }

    // ---- Export ----

    private string SuggestedName(string extension)
    {
        var stem = ActiveDocument?.FilePath is { } p
            ? Path.GetFileNameWithoutExtension(p)
            : "Untitled";
        return stem + extension;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task ExportHtmlAsync()
    {
        if (ActiveDocument is null) return;
        var path = _files.ShowSaveFileDialog("HTML|*.html", ".html", SuggestedName(".html"));
        if (path is null) return;
        await RunExport(path, () => _export.ExportHtmlAsync(ActiveDocument, path, _theme.IsDark));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task ExportDocxAsync()
    {
        if (ActiveDocument is null) return;
        var path = _files.ShowSaveFileDialog("Word Document|*.docx", ".docx", SuggestedName(".docx"));
        if (path is null) return;
        await RunExport(path, () => _export.ExportDocxAsync(ActiveDocument, path));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task ExportTextAsync()
    {
        if (ActiveDocument is null) return;
        var path = _files.ShowSaveFileDialog("Plain Text|*.txt", ".txt", SuggestedName(".txt"));
        if (path is null) return;
        await RunExport(path, () => _export.ExportPlainTextAsync(ActiveDocument, path));
    }

    private async Task RunExport(string path, Func<Task> export)
    {
        try
        {
            await export();
            StatusText = Lf("status.exported", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            StatusText = Lf("status.exportFailed", ex.Message);
        }
    }

    /// <summary>يجهّز تصدير PDF: يعرض حوار الحفظ ويعيد (HTML، المسار) أو null إن أُلغي.</summary>
    public (string Html, string Path)? PreparePdfExport()
    {
        if (ActiveDocument is null) return null;
        var path = _files.ShowSaveFileDialog("PDF|*.pdf", ".pdf", SuggestedName(".pdf"));
        if (path is null) return null;
        return (_export.BuildPrintableHtml(ActiveDocument), path);
    }

    /// <summary>يُستخدم من الواجهة للإبلاغ عن نتيجة تصدير PDF.</summary>
    public void ReportStatus(string message) => StatusText = message;

    public bool HasActiveDocument => ActiveDocument is not null;

}
