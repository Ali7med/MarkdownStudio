namespace MarkdownStudio.Services;

/// <summary>أدوات لبروتوكول markdownstudio:// (فتح ملف/مجلد عبر رابط).</summary>
public static class UriProtocol
{
    public const string Scheme = "markdownstudio";

    /// <summary>يستخرج مسار الملف/المجلد من رابط مثل markdownstudio://open?file=README.md</summary>
    public static string? ExtractPath(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) return null;

        var query = u.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var key = pair[..eq];
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            if (key.Equals("file", StringComparison.OrdinalIgnoreCase)
                || key.Equals("folder", StringComparison.OrdinalIgnoreCase)
                || key.Equals("path", StringComparison.OrdinalIgnoreCase))
                return value;
        }

        // بديل: markdownstudio://C:/path/file.md
        var fallback = Uri.UnescapeDataString(u.Host + u.AbsolutePath);
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }
}
