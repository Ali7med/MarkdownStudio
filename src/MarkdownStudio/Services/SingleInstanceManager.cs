using System.IO;
using System.IO.Pipes;

namespace MarkdownStudio.Services;

/// <summary>
/// يضمن نسخة واحدة من التطبيق: النسخة الأولى تُشغّل خادم أنبوب مُسمّى،
/// والنسخ اللاحقة تُرسل وسائطها إليها ثم تُغلق.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "MarkdownStudio.SingleInstance.v1";
    private const string PipeName = "MarkdownStudio.Pipe.v1";

    private Mutex? _mutex;
    private CancellationTokenSource? _cts;

    /// <summary>يُطلق (على خيط الخلفية) عندما تُرسل نسخة أخرى وسائطها.</summary>
    public event Action<string[]>? ArgsReceived;

    /// <summary>يحاول أن يصبح النسخة الأساسية. يعيد false إن كانت هناك نسخة تعمل.</summary>
    public bool TryAcquireOwnership()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        return createdNew;
    }

    /// <summary>يرسل الوسائط إلى النسخة الأساسية (يُستدعى من نسخة ثانوية).</summary>
    public static bool SendToPrimary(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            foreach (var arg in args) writer.WriteLine(arg);
            return true;
        }
        catch { return false; }
    }

    /// <summary>يبدأ خادم الأنبوب لاستقبال وسائط النسخ اللاحقة.</summary>
    public void StartServer()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ServerLoopAsync(_cts.Token));
    }

    private async Task ServerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server);
                var args = new List<string>();
                string? line;
                while ((line = await reader.ReadLineAsync(token)) is not null)
                    args.Add(line);

                if (args.Count > 0)
                    ArgsReceived?.Invoke(args.ToArray());
            }
            catch (OperationCanceledException) { break; }
            catch { /* اتصال تالف: تابع الاستماع */ }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _mutex?.Dispose();
    }
}
