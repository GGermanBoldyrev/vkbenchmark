using System.ComponentModel;
using Spectre.Console.Cli;
using WukongBench.Exceptions;
using WukongBench.Pc;
using WukongBench.Pc.Info;
using WukongBench.Settings;
using WukongBench.Settings.Profiles;
using WukongBench.Tool;

namespace WukongBench.Cli.Run;

// Единственная команда программы: то, что происходит при запуске.
[Description("Runs Black Myth: Wukong Benchmark Tool several times with different settings and prints the results.")]
public sealed class RunCommand : Command<RunSettings>
{
    private const string NotAvailable = "n/a";
    
    private static readonly string DefaultProfilesDir = Path.Combine("Settings", "Profiles", "Default");

    public override int Execute(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            BenchmarkToolInstallation installation = new BenchmarkToolLocator().Find(settings.ToolDir);
            Console.WriteLine($"Benchmark tool: {installation.InstallDir}");
            Console.WriteLine($"Settings file:  {installation.SettingsPath}");

            PcInfo pc = new PcInfoCollector().Collect();
            PrintPcInfo(pc);

            string profilesDir = settings.ProfilesDir
                ?? Path.Combine(AppContext.BaseDirectory, DefaultProfilesDir);
            IReadOnlyList<BenchmarkProfile> profiles = new ProfileLoader().LoadAll(profilesDir);
            Console.WriteLine($"Profiles: {string.Join(", ", profiles.Select(profile => profile.Name))}");

            SettingsFile settingsFile = new SettingsFile(installation.SettingsPath);
            SettingsBackup backup = new SettingsBackup(installation.SettingsPath);
            backup.Create();
            Console.WriteLine("Settings: backed up");

            try
            {
                foreach (BenchmarkProfile profile in profiles)
                {
                    backup.Restore();
                    settingsFile.Apply(profile);
                    Console.WriteLine($"Profile applied: {profile.Name} ({profile.Settings.Count} settings)");

                    // Запуск, ожидание и результаты — следующие этапы.
                }
            }
            finally
            {
                backup.Restore();
                backup.Delete();
                Console.WriteLine("Settings: restored");
            }

            return ExitCodes.Success;
        }
        catch (BenchmarkException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return ExitCodes.Failure;
        }
    }
    
    private static void PrintPcInfo(PcInfo pc)
    {
        Console.WriteLine(
            $"CPU: {Format(pc.Cpu.Name)}, cores: {Format(pc.Cpu.Cores)}, threads: {Format(pc.Cpu.Threads)}");

        Console.WriteLine(
            $"GPU: {Format(pc.Gpu.Name)}, VRAM: {FormatMemory(pc.Gpu.VramMb)}, driver: {Format(pc.Gpu.DriverVersion)}");

        Console.WriteLine($"RAM: {Format(pc.Ram.TotalGb, " GB")}");
        Console.WriteLine($"OS:  {Format(pc.Os.Name)}, build: {Format(pc.Os.Build)}");
    }
    
    private static string Format(object? value, string unit = "")
    {
        if (value is null)
        {
            return NotAvailable;
        }

        return $"{value}{unit}";
    }

    private static string FormatMemory(int? megabytes)
    {
        if (megabytes is null)
        {
            return NotAvailable;
        }

        if (megabytes < 1024)
        {
            return $"{megabytes} MB";
        }

        return $"{Math.Round(megabytes.Value / 1024.0)} GB";
    }
}
