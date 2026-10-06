using System.ComponentModel;
using Spectre.Console.Cli;
using WukongBench.Exceptions;
using WukongBench.Output;
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
    private static readonly string DefaultProfilesDir = Path.Combine("Settings", "Profiles", "Default");

    public override int Execute(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        ProgressLog log = new ProgressLog();

        try
        {
            BenchmarkToolInstallation installation = new BenchmarkToolLocator().Find(settings.ToolDir);
            log.Step("Benchmark Tool found");

            PcInfo pc = new PcInfoCollector().Collect();
            log.Step("PC info collected");

            string profilesDir = settings.ProfilesDir
                ?? Path.Combine(AppContext.BaseDirectory, DefaultProfilesDir);
            IReadOnlyList<BenchmarkProfile> profiles = new ProfileLoader().LoadAll(profilesDir);
            log.Step($"Profiles loaded: {profiles.Count}");

            SettingsFile settingsFile = new SettingsFile(installation.SettingsPath);
            SettingsBackup backup = new SettingsBackup(installation.SettingsPath);
            backup.Create();
            log.Step("Settings backed up");

            List<PassResult> passes = new List<PassResult>();

            try
            {
                for (int index = 0; index < profiles.Count; index++)
                {
                    BenchmarkProfile profile = profiles[index];
                    log.Step($"Pass {index + 1} of {profiles.Count}: {profile.Name}");

                    backup.Restore();
                    settingsFile.Apply(profile);
                    log.Detail("settings applied");

                    // Запуск, ожидание и результаты — следующие этапы.
                    passes.Add(new PassResult(profile, PassStatus.NotRun));
                }
            }
            finally
            {
                backup.Restore();
                backup.Delete();
                log.Step("Settings restored");
            }

            new ReportWriter().Write(new Report(pc, installation, passes));

            return ExitCodes.Success;
        }
        catch (BenchmarkException exception)
        {
            log.Error(exception.Message);
            return ExitCodes.Failure;
        }
    }
}
