using System.IO;
using MarkdownStudio.Models;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IWorkspaceService"/> فوق نظام الملفات.</summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private static readonly HashSet<string> IgnoredDirs = new(StringComparer.OrdinalIgnoreCase)
        { ".git", ".svn", ".hg", "node_modules", "bin", "obj", ".vs", ".idea", "dist", ".next", "__pycache__" };

    public async Task<FileSystemItem> OpenFolderAsync(string path)
    {
        var root = new FileSystemItem(path, isDirectory: true) { IsExpanded = true };
        await Task.Run(() => Populate(root));
        return root;
    }

    public void Refresh(FileSystemItem folder)
    {
        if (!folder.IsDirectory) return;
        folder.Children.Clear();
        Populate(folder);
    }

    /// <summary>يملأ أبناء العقدة من القرص (مجلدات أولاً ثم ملفات، أبجدياً).</summary>
    private static void Populate(FileSystemItem node)
    {
        DirectoryInfo dir;
        try { dir = new DirectoryInfo(node.FullPath); if (!dir.Exists) return; }
        catch { return; }

        IEnumerable<DirectoryInfo> dirs;
        IEnumerable<FileInfo> files;
        try
        {
            dirs = dir.EnumerateDirectories()
                      .Where(d => !IgnoredDirs.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.Hidden))
                      .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
            files = dir.EnumerateFiles()
                       .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                       .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException) { return; }

        foreach (var d in dirs)
        {
            var child = new FileSystemItem(d.FullName, isDirectory: true);
            Populate(child);   // فحص تدرّجي كامل (مع تجاهل المجلدات الثقيلة)
            node.Children.Add(child);
        }
        foreach (var f in files)
            node.Children.Add(new FileSystemItem(f.FullName, isDirectory: false));
    }

    public IEnumerable<string> EnumerateMarkdownFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            string[] subDirs, files;
            try
            {
                subDirs = Directory.GetDirectories(dir);
                files = Directory.GetFiles(dir);
            }
            catch { continue; }

            foreach (var f in files)
                if (IsMarkdown(f)) yield return f;

            foreach (var sub in subDirs)
                if (!IgnoredDirs.Contains(Path.GetFileName(sub)))
                    pending.Push(sub);
        }
    }

    public string CreateFile(string parentDir, string name)
    {
        var path = UniquePath(Path.Combine(parentDir, name));
        File.WriteAllText(path, string.Empty);
        return path;
    }

    public string CreateFolder(string parentDir, string name)
    {
        var path = UniquePath(Path.Combine(parentDir, name));
        Directory.CreateDirectory(path);
        return path;
    }

    public string Rename(string path, string newName)
    {
        var parent = Path.GetDirectoryName(path)!;
        var target = Path.Combine(parent, newName);
        if (string.Equals(path, target, StringComparison.Ordinal)) return path;

        if (Directory.Exists(path)) Directory.Move(path, target);
        else File.Move(path, target);
        return target;
    }

    public void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        else if (File.Exists(path)) File.Delete(path);
    }

    public string Duplicate(string path)
    {
        if (Directory.Exists(path))
        {
            var dest = UniquePath(path);
            CopyDirectory(path, dest);
            return dest;
        }
        else
        {
            var dir = Path.GetDirectoryName(path)!;
            var stem = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var dest = UniquePath(Path.Combine(dir, $"{stem} - نسخة{ext}"));
            File.Copy(path, dest);
            return dest;
        }
    }

    private static bool IsMarkdown(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".mdown", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".mkd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".mkdn", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".mdtxt", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>يضيف لاحقة رقمية إن كان المسار موجوداً مسبقاً.</summary>
    private static string UniquePath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var isDir = Directory.Exists(path);
        var stem = isDir ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
        var ext = isDir ? string.Empty : Path.GetExtension(path);

        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        foreach (var sub in Directory.GetDirectories(source))
            CopyDirectory(sub, Path.Combine(dest, Path.GetFileName(sub)));
    }
}
