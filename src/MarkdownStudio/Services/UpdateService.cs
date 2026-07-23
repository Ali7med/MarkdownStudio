using Velopack;
using Velopack.Sources;

namespace MarkdownStudio.Services;

/// <summary>تطبيق <see cref="IUpdateService"/> عبر Velopack ومصدر GitHub Releases للمستودع العام.</summary>
public sealed class UpdateService : IUpdateService
{
    /// <summary>مصدر التحديث: إصدارات GitHub للمستودع الرسمي.</summary>
    private const string RepoUrl = "https://github.com/Ali7med/MarkdownStudio";

    private readonly UpdateManager _mgr = new(new GithubSource(RepoUrl, null, false, null));

    public bool IsInstalled => _mgr.IsInstalled;

    public async Task CheckAsync(Func<string, Task<bool>> confirmAsync)
    {
        if (!_mgr.IsInstalled) return;   // نسخة تطوير: لا تحديث

        UpdateInfo? info;
        try { info = await _mgr.CheckForUpdatesAsync(); }
        catch { return; }                // انقطاع شبكة / خطأ: تجاهل بصمت

        if (info is null) return;         // محدّث بالفعل

        await _mgr.DownloadUpdatesAsync(info);

        if (await confirmAsync(info.TargetFullRelease.Version.ToString()))
            _mgr.ApplyUpdatesAndRestart(info.TargetFullRelease);
    }
}
