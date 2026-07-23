namespace MarkdownStudio.Services;

/// <summary>يفحص تحديثات التطبيق من GitHub Releases (عبر Velopack) ويطبّقها بعد تأكيد المستخدم.</summary>
public interface IUpdateService
{
    /// <summary>true إن كان التطبيق مثبّتاً عبر Velopack (وليس نسخة تطوير/محمولة).</summary>
    bool IsInstalled { get; }

    /// <summary>
    /// يفحص التحديثات؛ وإن وُجد تحديث حمّله ثم استدعى <paramref name="confirmAsync"/> (بالنسخة الجديدة)،
    /// فإن وافق المستخدم طبّق التحديث وأعاد تشغيل التطبيق. آمن ضد الأخطاء (يتجاهل انقطاع الشبكة بصمت).
    /// </summary>
    Task CheckAsync(Func<string, Task<bool>> confirmAsync);
}
