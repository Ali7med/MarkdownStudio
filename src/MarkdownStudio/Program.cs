using System;
using MarkdownStudio.Services;
using Velopack;

namespace MarkdownStudio;

/// <summary>
/// نقطة الدخول الفعلية. يجب أن يُشغَّل <see cref="VelopackApp"/> أولاً كي يعالج خطافات
/// التثبيت/التحديث/الحذف (يخرج فوراً في تلك الحالات) قبل إقلاع WPF.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // خطافات دورة حياة Velopack: عند التثبيت/التحديث نسجّل ربط الملفات، وعند الحذف نلغيه.
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => TryIntegration(register: true))
            .OnAfterUpdateFastCallback(_ => TryIntegration(register: true))
            .OnBeforeUninstallFastCallback(_ => TryIntegration(register: false))
            .Run();

        // أوامر خدمية للإصلاح اليدوي (أو استدعاء من سكربت): تسجيل/إلغاء بلا واجهة.
        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--register":   TryIntegration(register: true);  return;
                case "--unregister": TryIntegration(register: false); return;
            }
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    /// <summary>يسجّل/يلغي تكامل النظام دون إسقاط العملية إن فشل (ليس حرجاً للتثبيت).</summary>
    private static void TryIntegration(bool register)
    {
        try
        {
            var svc = new WindowsIntegrationService();
            if (register) svc.RegisterIntegration();
            else svc.UnregisterIntegration();
        }
        catch { /* تكامل النظام ليس حرجاً لنجاح التثبيت/الحذف */ }
    }
}
