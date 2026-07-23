using System.IO;
using System.Windows;
using MarkdownStudio.Services;
using MarkdownStudio.ViewModels;
using MarkdownStudio.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MarkdownStudio;

/// <summary>نقطة الدخول: تبني Generic Host مع حاوية DI وتُطلق النافذة الرئيسية.</summary>
public partial class App : Application
{
    private readonly IHost _host;
    private readonly SingleInstanceManager _singleInstance = new();

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Services (Singletons)
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IRecentFilesService, RecentFilesService>();
                services.AddSingleton<IFileWatcherService, FileWatcherService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<IWorkspaceService, WorkspaceService>();
                services.AddSingleton<IWindowsIntegrationService, WindowsIntegrationService>();
                services.AddSingleton<ProjectSearchService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IChangelogService, ChangelogService>();
                services.AddSingleton<IUpdateService, UpdateService>();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<WhatsNewWindow>();
            })
            .Build();
    }

    /// <summary>حاوية الخدمات — للوصول من عناصر لا تدعم حقن المُنشئ.</summary>
    public static IServiceProvider Services => ((App)Current)._host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // نسخة واحدة: النسخ اللاحقة تُمرّر وسائطها للنسخة الأساسية وتخرج.
        if (!_singleInstance.TryAcquireOwnership())
        {
            SingleInstanceManager.SendToPrimary(e.Args);
            Shutdown();
            return;
        }
        _singleInstance.ArgsReceived += OnSecondaryInstanceArgs;
        _singleInstance.StartServer();

        await _host.StartAsync();

        // تطبيق الثيم واللغة قبل إظهار النافذة (لا وميض).
        var settings0 = Services.GetRequiredService<ISettingsService>().Settings;
        Services.GetRequiredService<IThemeService>().ApplyPreset(settings0.ThemeId ?? "fluent-dark");
        ApplyStartupLanguage();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();

        await ProcessArgsAsync(e.Args);
        MaybeShowWhatsNew(window);
    }

    /// <summary>يطبّق اللغة المحفوظة، أو لغة النظام، أو الافتراضية.</summary>
    private static void ApplyStartupLanguage()
    {
        var settings = Services.GetRequiredService<ISettingsService>().Settings;
        var code = settings.LanguageCode
                   ?? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        Localization.Localizer.Instance.SetLanguage(code);
    }

    /// <summary>يعرض «ما الجديد» تلقائياً مرّة واحدة عند تغيّر الإصدار.</summary>
    private static void MaybeShowWhatsNew(Window owner)
    {
        var settingsService = Services.GetRequiredService<ISettingsService>();
        var changelog = Services.GetRequiredService<IChangelogService>();
        if (changelog.Entries.Count == 0) return;
        if (!changelog.ShouldAutoShow(settingsService.Settings.LastShownWhatsNewVersion)) return;

        var win = Services.GetRequiredService<WhatsNewWindow>();
        win.Owner = owner;
        win.ShowDialog();

        settingsService.Settings.LastShownWhatsNewVersion = AppVersion.Current;
        settingsService.Save();
    }

    /// <summary>يعالج وسائط سطر الأوامر: ملفات، مجلدات، أو روابط markdownstudio://.</summary>
    private static async Task ProcessArgsAsync(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return;
        var vm = Services.GetRequiredService<MainWindowViewModel>();

        foreach (var raw in args)
        {
            var arg = raw;
            if (arg.StartsWith("markdownstudio://", StringComparison.OrdinalIgnoreCase))
                arg = UriProtocol.ExtractPath(arg) ?? arg;

            if (Directory.Exists(arg)) await vm.OpenFolderPathAsync(arg);
            else if (File.Exists(arg)) await vm.OpenPathAsync(arg);
        }
    }

    /// <summary>يُستدعى عند إرسال نسخة ثانية لوسائطها — يُبرز النافذة ويفتحها.</summary>
    private void OnSecondaryInstanceArgs(string[] args)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            if (MainWindow is { } win)
            {
                if (win.WindowState == WindowState.Minimized) win.WindowState = WindowState.Normal;
                win.Activate();
                win.Topmost = true;
                win.Topmost = false;
            }
            await ProcessArgsAsync(args);
        });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _singleInstance.Dispose();
        if (_host.Services is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
