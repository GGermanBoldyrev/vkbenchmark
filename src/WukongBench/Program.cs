using Spectre.Console.Cli;

using WukongBench.Cli.Run;

namespace WukongBench;

internal static class Program
{
    private static int Main(string[] args)
    {
        CommandApp<RunCommand> app = new CommandApp<RunCommand>();

        app.Configure(config =>
        {
            config.SetApplicationName("WukongBench");
            config.UseStrictParsing();

            // Примеры запуска
            config.AddExample("--tool-dir", @"D:\Games\BenchmarkTool");
        });

        return app.Run(args);
    }
}
