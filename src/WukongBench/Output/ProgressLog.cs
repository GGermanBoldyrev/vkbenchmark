using System.Diagnostics;

using Spectre.Console;

namespace WukongBench.Output;

// Ход работы: что программа делает сейчас, со временем от старта.
// Пишется в поток ошибок, чтобы в стандартном выводе оставался только отчёт.
internal sealed class ProgressLog
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    private readonly IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error),
    });

    // Этап верхнего уровня.
    public void Step(string message)
    {
        console.MarkupLine($"{Timestamp()} {Markup.Escape(message)}");
    }

    public void Detail(string message)
    {
        console.MarkupLine($"{Timestamp()}   {Markup.Escape(message)}");
    }

    public void Error(string message)
    {
        console.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    private string Timestamp()
    {
        TimeSpan elapsed = stopwatch.Elapsed;

        return $"[grey][[{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}]][/]";
    }
}
