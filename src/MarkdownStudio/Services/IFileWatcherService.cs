namespace MarkdownStudio.Services;

/// <summary>يراقب ملفاً واحداً على القرص ويُبلّغ عند تغيّره خارج البرنامج.</summary>
public interface IFileWatcherService : IDisposable
{
    /// <summary>يُطلق (على خيط الواجهة) عند تعديل الملف المُراقَب خارجياً.</summary>
    event EventHandler<string>? FileChanged;

    /// <summary>يبدأ مراقبة المسار المحدّد (يستبدل أي مراقبة سابقة). null يوقف المراقبة.</summary>
    void Watch(string? path);
}
