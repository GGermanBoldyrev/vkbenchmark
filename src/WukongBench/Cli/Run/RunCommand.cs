using System.ComponentModel;
using Spectre.Console.Cli;
using WukongBench.Exceptions;
using WukongBench.Tool;

namespace WukongBench.Cli.Run;

// Единственная команда программы: то, что происходит при запуске.
[Description("Runs Black Myth: Wukong Benchmark Tool several times with different settings and prints the results.")]
public sealed class RunCommand : Command<RunSettings>
{
    public override int Execute(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            BenchmarkToolInstallation installation = new BenchmarkToolLocator().Find(settings.ToolDir);
            Console.WriteLine($"Benchmark tool: {installation.InstallDir}");
            Console.WriteLine($"Settings file:  {installation.SettingsPath}");

            PcInfo pc = CollectPcInfo();
            Console.WriteLine($"CPU: {pc.Cpu}");
            Console.WriteLine($"GPU: {pc.Gpu}");
            Console.WriteLine($"RAM: {pc.RamGb} GB");
            Console.WriteLine($"OS:  {pc.Os}");

            string backupPath = BackupSettings(installation.SettingsPath);
            Console.WriteLine($"Settings backup: {backupPath}");

            return ExitCodes.Success;
        }
        catch (BenchmarkException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return ExitCodes.Failure;
        }
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
