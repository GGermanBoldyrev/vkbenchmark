using System.ComponentModel;
using Spectre.Console.Cli;

namespace WukongBench.Cli.Run;

// Всё, что пользователь может задать при запуске.
public sealed class RunSettings : CommandSettings
{
    [CommandOption("--tool-dir <tool-dir>")]
    [Description("Benchmark Tool install folder. Use it when the automatic search via Steam fails.")]
    public string? ToolDir { get; init; }
}
