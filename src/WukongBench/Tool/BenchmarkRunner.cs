using System.Diagnostics;

using WukongBench.Exceptions;

namespace WukongBench.Tool;

// Один проход бенчмарка: запускает утилиту, ждёт файл результатов и закрывает её.
internal sealed class BenchmarkRunner(BenchmarkToolInstallation installation)
{
    // С запасом: перед тестом утилита может компилировать шейдеры.
    private static readonly TimeSpan ResultsTimeout = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    // Параметр конструктора можно перезаписать, поле readonly — нельзя.
    private readonly BenchmarkToolInstallation installation = installation;

    private readonly string resultsDir = Path.Combine(Path.GetTempPath(), BenchmarkTool.ResultsRelativePath);

    public void EnsureNotRunning()
    {
        if (IsRunning(BenchmarkTool.ProcessName) || IsRunning(BenchmarkTool.LauncherProcessName))
        {
            throw new BenchmarkException("Benchmark Tool is already running. Close it and try again.");
        }
    }

    // Возвращает путь к файлу результатов.
    public string Run(CancellationToken cancellationToken)
    {
        EnsureNotRunning();

        // Утилита хранит и прошлые результаты: наш файл — тот, которого до запуска не было.
        HashSet<string> knownResults = ListResults();

        using (Process launcher = Start())
        {
            try
            {
                string resultsPath = WaitForResults(launcher, knownResults, cancellationToken);
                WaitUntilWritten(resultsPath, cancellationToken);

                return resultsPath;
            }
            finally
            {
                Close(launcher);
            }
        }
    }

    private Process Start()
    {
        if (!File.Exists(installation.LauncherPath))
        {
            throw new BenchmarkException($"Benchmark Tool launcher not found: {installation.LauncherPath}");
        }

        // Вывод перенаправляем, чтобы служебные строки утилиты и Steam не попали в наш.
        ProcessStartInfo startInfo = new ProcessStartInfo(installation.LauncherPath)
        {
            WorkingDirectory = installation.InstallDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(BenchmarkTool.AutoStartArgument);

        // Без этих переменных утилита перезапускается через Steam, и тот просит подтвердить запуск.
        startInfo.Environment["SteamAppId"] = BenchmarkTool.AppId;
        startInfo.Environment["SteamGameId"] = BenchmarkTool.AppId;

        Process launcher = new Process { StartInfo = startInfo };

        try
        {
            launcher.Start();

            // Непрочитанный вывод остановил бы утилиту, когда заполнится буфер.
            launcher.BeginOutputReadLine();
            launcher.BeginErrorReadLine();
        }
        catch (Exception exception)
        {
            launcher.Dispose();

            throw new BenchmarkException($"Cannot start Benchmark Tool {installation.LauncherPath}: {exception.Message}");
        }

        return launcher;
    }

    private string WaitForResults(Process launcher, HashSet<string> knownResults, CancellationToken cancellationToken)
    {
        Stopwatch waiting = Stopwatch.StartNew();

        while (waiting.Elapsed < ResultsTimeout)
        {
            foreach (string name in ListResults())
            {
                if (!knownResults.Contains(name))
                {
                    return Path.Combine(resultsDir, name);
                }
            }

            // Загрузчик работает, пока открыта утилита.
            if (launcher.HasExited)
            {
                throw new BenchmarkException("Benchmark Tool closed before the benchmark finished.");
            }

            Wait(cancellationToken);
        }

        throw new BenchmarkException(
            $"Benchmark Tool did not write the results in {ResultsTimeout.TotalMinutes:0} minutes.");
    }

    // Файл появляется раньше, чем дописан: ждём, пока его размер перестанет меняться.
    private static void WaitUntilWritten(string path, CancellationToken cancellationToken)
    {
        long previousLength = -1;

        while (true)
        {
            long length = new FileInfo(path).Length;
            if (length > 0 && length == previousLength)
            {
                return;
            }

            previousLength = length;
            Wait(cancellationToken);
        }
    }

    // Ждём выхода: закрываясь, утилита пересохраняет файл настроек.
    private static void Close(Process launcher)
    {
        try
        {
            foreach (Process process in Process.GetProcessesByName(BenchmarkTool.ProcessName))
            {
                using (process)
                {
                    process.CloseMainWindow();
                }
            }

            if (launcher.WaitForExit(ExitTimeout))
            {
                return;
            }

            launcher.Kill(entireProcessTree: true);
            launcher.WaitForExit(ExitTimeout);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException($"Cannot close Benchmark Tool: {exception.Message}");
        }
    }

    private HashSet<string> ListResults()
    {
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(resultsDir))
        {
            foreach (string file in Directory.GetFiles(resultsDir))
            {
                names.Add(Path.GetFileName(file));
            }
        }

        return names;
    }

    private static bool IsRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        foreach (Process process in processes)
        {
            process.Dispose();
        }

        return processes.Length > 0;
    }

    private static void Wait(CancellationToken cancellationToken)
    {
        cancellationToken.WaitHandle.WaitOne(PollInterval);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
