using System.IO;
using System.Windows;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IFileWatcherService"/> فوق <see cref="FileSystemWatcher"/>.</summary>
public sealed class FileWatcherService : IFileWatcherService
{
    private FileSystemWatcher? _watcher;
    private string? _path;
    private DateTime _lastRaised = DateTime.MinValue;

    public event EventHandler<string>? FileChanged;

    public void Watch(string? path)
    {
        Stop();
        if (string.IsNullOrEmpty(path)) return;

        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

        _path = path;
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // إزالة ارتداد الأحداث المتكرّرة (يكتب المحرّرون عدة أحداث للحفظة الواحدة).
        var now = DateTime.UtcNow;
        if ((now - _lastRaised).TotalMilliseconds < 300) return;
        _lastRaised = now;

        var path = _path;
        if (path is null) return;

        Application.Current?.Dispatcher.BeginInvoke(() => FileChanged?.Invoke(this, path));
    }

    private void Stop()
    {
        if (_watcher is null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Renamed -= OnChanged;
        _watcher.Dispose();
        _watcher = null;
        _path = null;
    }

    public void Dispose() => Stop();
}
