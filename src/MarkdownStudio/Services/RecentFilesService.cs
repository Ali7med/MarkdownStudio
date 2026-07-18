using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IRecentFilesService"/> يحفظ القائمة كـ JSON في %AppData%.</summary>
public sealed class RecentFilesService : IRecentFilesService
{
    private const int MaxItems = 12;

    private readonly string _storePath;
    private readonly ObservableCollection<string> _files = new();

    public RecentFilesService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownStudio");
        Directory.CreateDirectory(dir);
        _storePath = Path.Combine(dir, "recent.json");

        Files = new ReadOnlyObservableCollection<string>(_files);
        Load();
    }

    public ReadOnlyObservableCollection<string> Files { get; }

    public void Add(string path)
    {
        var full = Path.GetFullPath(path);
        RemoveExisting(full);
        _files.Insert(0, full);
        while (_files.Count > MaxItems)
            _files.RemoveAt(_files.Count - 1);
        Save();
    }

    public void Remove(string path)
    {
        if (RemoveExisting(Path.GetFullPath(path))) Save();
    }

    public void Clear()
    {
        _files.Clear();
        Save();
    }

    private bool RemoveExisting(string full)
    {
        for (var i = 0; i < _files.Count; i++)
            if (string.Equals(_files[i], full, StringComparison.OrdinalIgnoreCase))
            {
                _files.RemoveAt(i);
                return true;
            }
        return false;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_storePath)) return;
            var items = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_storePath));
            if (items is null) return;
            foreach (var item in items.Where(File.Exists).Take(MaxItems))
                _files.Add(item);
        }
        catch { /* ملف تالف: تجاهل */ }
    }

    private void Save()
    {
        try { File.WriteAllText(_storePath, JsonSerializer.Serialize(_files.ToList())); }
        catch { /* تعذّر الحفظ: تجاهل */ }
    }
}
