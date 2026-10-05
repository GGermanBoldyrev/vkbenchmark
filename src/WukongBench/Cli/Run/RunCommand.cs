using System.ComponentModel;
using Spectre.Console.Cli;

namespace WukongBench.Cli.Run;

// Единственная команда программы: то, что происходит при запуске.
[Description("Runs Black Myth: Wukong Benchmark Tool several times with different settings and prints the results.")]
public sealed class RunCommand : Command<RunSettings>
{
    public override int Execute(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        BenchmarkTool? tool = new BenchmarkToolLocator().Find(settings.ToolDir);
        if (tool is null)
        {
            Console.Error.WriteLine(
                $"Benchmark Tool not found. Install it via Steam (AppID {BenchmarkToolLocator.AppId}) " +
                "or pass its install folder with --tool-dir.");
            return 1;
        }

        Console.WriteLine($"Benchmark tool: {tool.InstallDir}");
        Console.WriteLine($"Settings file:  {tool.SettingsPath}");

        PcInfo pc = CollectPcInfo();
        Console.WriteLine($"CPU: {pc.Cpu}");
        Console.WriteLine($"GPU: {pc.Gpu}");
        Console.WriteLine($"RAM: {pc.RamGb} GB");
        Console.WriteLine($"OS:  {pc.Os}");

        string backupPath = BackupSettings(tool.SettingsPath);
        Console.WriteLine($"Settings backup: {backupPath}");

        return 0;
    }

    // Мок: настоящие данные возьмём у Windows.
    private static PcInfo CollectPcInfo()
    {
        return new PcInfo("Mock CPU", "Mock GPU", 32, "Mock OS");
    }

    // Мок: файл не копируется, возвращается только путь будущей копии.
    private static string BackupSettings(string settingsPath)
    {
        return settingsPath + ".bak";
    }
}
