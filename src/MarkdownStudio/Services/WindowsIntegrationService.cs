using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Win32;

namespace MarkdownStudio.Services;

/// <summary>
/// تطبيق <see cref="IWindowsIntegrationService"/> عبر HKCU (لا يحتاج صلاحيات مدير).
/// يحترم الوضع المحمول: وجود ملف "portable.marker" بجانب التنفيذي يعطّل كل تعديلات الـ Registry.
/// </summary>
public sealed class WindowsIntegrationService : IWindowsIntegrationService
{
    private const string ProgId = "MarkdownStudio.Document";
    private const string AppName = "Markdown Studio";
    private const string CapabilitiesPath = @"Software\MarkdownStudio\Capabilities";
    private static readonly string[] Extensions =
        { ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdtxt" };

    private readonly string _exePath;

    public WindowsIntegrationService()
    {
        _exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var dir = Path.GetDirectoryName(_exePath) ?? "";
        IsPortable = File.Exists(Path.Combine(dir, "portable.marker"));
    }

    public bool IsPortable { get; }

    public bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}");
            return key is not null;
        }
    }

    public void RegisterIntegration()
    {
        if (IsPortable) return;
        var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes");

        // ProgId: أيقونة + أمر الفتح + قائمة السياق.
        using (var prog = classes.CreateSubKey(ProgId))
        {
            prog.SetValue("", "مستند Markdown");
            using (var icon = prog.CreateSubKey("DefaultIcon"))
                icon.SetValue("", $"\"{_exePath}\",0");
            using (var cmd = prog.CreateSubKey(@"shell\open\command"))
                cmd.SetValue("", $"\"{_exePath}\" \"%1\"");

            // قائمة السياق: فتح + معاينة.
            using (var openCmd = prog.CreateSubKey(@"shell\open"))
                openCmd.SetValue("", "فتح في Markdown Studio");
        }

        // إظهار التطبيق في "الفتح باستخدام" لكل امتداد مدعوم.
        foreach (var ext in Extensions)
            using (var extKey = classes.CreateSubKey($@"{ext}\OpenWithProgids"))
                extKey.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);

        // تسجيل التطبيق تحت Applications: يجعله يظهر ضمن اقتراحات «الفتح باستخدام»
        // لأنواع Markdown (عبر SupportedTypes) باسم ودود وأيقونة.
        var appExe = Path.GetFileName(_exePath);
        if (!string.IsNullOrEmpty(appExe))
        {
            using var app = classes.CreateSubKey($@"Applications\{appExe}");
            app.SetValue("FriendlyAppName", AppName);
            using (var icon = app.CreateSubKey("DefaultIcon"))
                icon.SetValue("", $"\"{_exePath}\",0");
            using (var cmd = app.CreateSubKey(@"shell\open\command"))
                cmd.SetValue("", $"\"{_exePath}\" \"%1\"");
            using (var st = app.CreateSubKey("SupportedTypes"))
                foreach (var ext in Extensions) st.SetValue(ext, string.Empty);
        }

        // قائمة سياق لملفات Markdown عموماً (SystemFileAssociations).
        foreach (var ext in Extensions)
            using (var shell = classes.CreateSubKey(
                       $@"SystemFileAssociations\{ext}\shell\MarkdownStudio.Open\command"))
            {
                using var label = classes.CreateSubKey($@"SystemFileAssociations\{ext}\shell\MarkdownStudio.Open");
                label.SetValue("", "فتح في Markdown Studio");
                label.SetValue("Icon", $"\"{_exePath}\",0");
                shell.SetValue("", $"\"{_exePath}\" \"%1\"");
            }

        // بروتوكول markdownstudio://
        using (var proto = classes.CreateSubKey(UriProtocol.Scheme))
        {
            proto.SetValue("", $"URL:{AppName} Protocol");
            proto.SetValue("URL Protocol", "");
            using var cmd = proto.CreateSubKey(@"shell\open\command");
            cmd.SetValue("", $"\"{_exePath}\" \"%1\"");
        }

        // Capabilities + RegisteredApplications: يُظهر التطبيق في «التطبيقات الافتراضية» بويندوز،
        // فيصبح قابلاً للتعيين افتراضياً لملفات Markdown من إعدادات النظام.
        using (var cap = Registry.CurrentUser.CreateSubKey(CapabilitiesPath))
        {
            cap.SetValue("ApplicationName", AppName);
            cap.SetValue("ApplicationDescription", "عارض ومحرّر Markdown");
            using (var fa = cap.CreateSubKey("FileAssociations"))
                foreach (var ext in Extensions) fa.SetValue(ext, ProgId);
            using (var ua = cap.CreateSubKey("URLAssociations"))
                ua.SetValue(UriProtocol.Scheme, ProgId);
        }
        using (var reg = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
            reg.SetValue(AppName, CapabilitiesPath);

        CreateSendToShortcut();
        NotifyShellChanged();
    }

    /// <summary>ينشئ اختصاراً في قائمة "إرسال إلى" (Send To) يشير إلى التطبيق.</summary>
    private void CreateSendToShortcut()
    {
        try
        {
            var sendTo = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
            var linkPath = Path.Combine(sendTo, $"{AppName}.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = _exePath;
            shortcut.IconLocation = $"{_exePath}, 0";
            shortcut.Description = AppName;
            shortcut.Save();
        }
        catch { /* WSH غير متاح: تجاهل */ }
    }

    /// <summary>يفتح صفحة «التطبيقات الافتراضية» في إعدادات ويندوز لتأكيد تعيين البرنامج افتراضياً.</summary>
    public void OpenDefaultAppsSettings()
    {
        try
        {
            // على ويندوز 11 يفتح مباشرة صفحة هذا التطبيق؛ على ويندوز 10 يفتح صفحة التطبيقات الافتراضية.
            var target = $"ms-settings:defaultapps?registeredAppUser={Uri.EscapeDataString(AppName)}";
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch { /* تعذّر فتح الإعدادات: تجاهل */ }
    }

    public void AddToRecentDocs(string path)
    {
        try
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(path + "\0");
            SHAddToRecentDocs(0x0003 /* SHARD_PATHW */, bytes);
        }
        catch { /* تجاهل */ }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern void SHAddToRecentDocs(uint flags, byte[] path);

    public void UnregisterIntegration()
    {
        if (IsPortable) return;
        var classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", writable: true);
        if (classes is null) return;

        classes.DeleteSubKeyTree(ProgId, throwOnMissingSubKey: false);
        classes.DeleteSubKeyTree(UriProtocol.Scheme, throwOnMissingSubKey: false);

        // إزالة Capabilities + RegisteredApplications.
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\MarkdownStudio", throwOnMissingSubKey: false);
        using (var reg = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", writable: true))
            reg?.DeleteValue(AppName, throwOnMissingValue: false);

        // إزالة تسجيل Applications.
        var appExe = Path.GetFileName(_exePath);
        if (!string.IsNullOrEmpty(appExe))
            classes.DeleteSubKeyTree($@"Applications\{appExe}", throwOnMissingSubKey: false);

        foreach (var ext in Extensions)
        {
            using (var owp = classes.OpenSubKey($@"{ext}\OpenWithProgids", writable: true))
                owp?.DeleteValue(ProgId, throwOnMissingValue: false);
            classes.DeleteSubKeyTree($@"SystemFileAssociations\{ext}\shell\MarkdownStudio.Open",
                throwOnMissingSubKey: false);
        }

        try
        {
            var link = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.SendTo), $"{AppName}.lnk");
            if (File.Exists(link)) File.Delete(link);
        }
        catch { /* تجاهل */ }

        NotifyShellChanged();
    }

    public void UpdateJumpList(IEnumerable<string> recentFiles)
    {
        if (Application.Current is null) return;

        var jumpList = new JumpList { ShowRecentCategory = false, ShowFrequentCategory = false };
        foreach (var file in recentFiles.Where(File.Exists).Take(10))
        {
            jumpList.JumpItems.Add(new JumpTask
            {
                Title = Path.GetFileName(file),
                Description = file,
                ApplicationPath = _exePath,
                Arguments = $"\"{file}\"",
                IconResourcePath = _exePath
            });
        }
        JumpList.SetJumpList(Application.Current, jumpList);
        jumpList.Apply();
    }

    /// <summary>يُعلم مستكشف Windows بتغيّر ارتباطات الملفات.</summary>
    private static void NotifyShellChanged()
    {
        try { SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); } catch { }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
