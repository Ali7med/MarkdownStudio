namespace MarkdownStudio.Models;

/// <summary>وضع التطبيق العام: عارض للقراءة أو محرّر.</summary>
public enum AppMode
{
    /// <summary>عارض: معاينة مُنسّقة للقراءة فقط (الوضع الافتراضي).</summary>
    Viewer,

    /// <summary>محرّر: تحرير كود Markdown مع معاينة حيّة.</summary>
    Editor
}
