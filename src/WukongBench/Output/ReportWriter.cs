using System.Globalization;

using Spectre.Console;

using WukongBench.Pc.Info;
using WukongBench.Results;
using WukongBench.Settings.Profiles;

namespace WukongBench.Output;

// Итоговый отчёт: система, результаты, настройки профилей и настройки по данным утилиты.
internal sealed class ReportWriter
{
    // Значение не удалось определить.
    private const string NotAvailable = "n/a";

    // Значения нет: профиль настройку не задаёт или проход не дал результата.
    private const string NoValue = "—";

    public void Write(Report report)
    {
        WriteSystem(report);
        WriteResults(report.Passes);
        WriteProfileSettings(report.Passes);
        WriteReportedSettings(report.Passes);
    }

    private static void WriteSystem(Report report)
    {
        PcInfo pc = report.Pc;

        Table table = NewTable();
        table.HideHeaders();
        table.AddColumn(string.Empty);
        table.AddColumn(string.Empty);

        AddRow(table, "CPU", JoinKnown(pc.Cpu.Name, FormatCores(pc.Cpu)));
        AddRow(table, "GPU", JoinKnown(pc.Gpu.Name, FormatMemory(pc.Gpu.VramMb), WithLabel("driver", pc.Gpu.DriverVersion)));
        AddRow(table, "RAM", JoinKnown(WithUnit(pc.Ram.TotalGb, "GB")));
        AddRow(table, "OS", JoinKnown(pc.Os.Name, WithLabel("build", pc.Os.Build)));
        AddRow(table, "Benchmark Tool", report.Tool.InstallDir);

        WriteTable("SYSTEM", table);
    }

    // Столбцы — проходы: сколько профилей, столько столбцов.
    private static void WriteResults(IReadOnlyList<PassResult> passes)
    {
        Table table = NewTable();
        AddPassColumns(table, passes);

        AddPassRow(table, passes, "Status", pass => FormatStatus(pass.Status));
        AddPassRow(table, passes, "Average FPS", pass => FormatNumber(pass.Result?.FpsAverage));
        AddPassRow(table, passes, "Maximum FPS", pass => FormatNumber(pass.Result?.FpsMax));
        AddPassRow(table, passes, "Minimum FPS", pass => FormatNumber(pass.Result?.FpsMin));
        AddPassRow(table, passes, "5th percentile FPS", pass => FormatNumber(pass.Result?.FpsPercentile5));
        AddPassRow(table, passes, "CPU frame time, ms", pass => FormatNumber(pass.Result?.CpuFrameTimeMs));
        AddPassRow(table, passes, "GPU frame time, ms", pass => FormatNumber(pass.Result?.GpuFrameTimeMs));
        AddPassRow(table, passes, "Video memory used, GB", pass => FormatNumber(pass.Result?.VideoMemoryGb));
        AddPassRow(table, passes, "Duration, s", pass => FormatNumber(pass.Result?.DurationSeconds));

        WriteTable("RESULTS", table);
    }

    // Строки — объединение настроек всех профилей в порядке их появления.
    private static void WriteProfileSettings(IReadOnlyList<PassResult> passes)
    {
        Table table = NewTable();
        AddPassColumns(table, passes);

        List<ProfileSetting> rows = new List<ProfileSetting>();
        foreach (PassResult pass in passes)
        {
            foreach (ProfileSetting setting in pass.Profile.Settings)
            {
                if (!rows.Any(row => IsSameSetting(row, setting)))
                {
                    rows.Add(setting);
                }
            }
        }

        foreach (ProfileSetting row in rows)
        {
            List<string> cells = new List<string> { Markup.Escape(row.Key) };
            foreach (PassResult pass in passes)
            {
                ProfileSetting? setting = pass.Profile.Settings.FirstOrDefault(item => IsSameSetting(item, row));
                if (setting is null)
                {
                    cells.Add(NoValue);
                }
                else
                {
                    cells.Add(Markup.Escape(setting.Value));
                }
            }

            table.AddRow(cells.ToArray());
        }

        WriteTable("PROFILE SETTINGS", table);
    }

    // Что о проходе записала сама утилита: видно, какие настройки профиля она приняла.
    private static void WriteReportedSettings(IReadOnlyList<PassResult> passes)
    {
        List<string> names = new List<string>();
        foreach (PassResult pass in passes)
        {
            if (pass.Result is null)
            {
                continue;
            }

            foreach (ReportedSetting setting in pass.Result.Settings)
            {
                if (!names.Contains(setting.Name))
                {
                    names.Add(setting.Name);
                }
            }
        }

        // Ни один проход не дал результата: показывать нечего.
        if (names.Count == 0)
        {
            return;
        }

        Table table = NewTable();
        AddPassColumns(table, passes);

        foreach (string name in names)
        {
            AddPassRow(table, passes, name, pass => FindReportedValue(pass, name));
        }

        WriteTable("SETTINGS REPORTED BY BENCHMARK TOOL", table);
    }

    private static string? FindReportedValue(PassResult pass, string name)
    {
        ReportedSetting? setting = pass.Result?.Settings.FirstOrDefault(item => item.Name == name);
        if (setting is null)
        {
            return null;
        }

        return Markup.Escape(setting.Value);
    }

    // Строка таблицы: подпись и по ячейке на проход.
    private static void AddPassRow(
        Table table,
        IReadOnlyList<PassResult> passes,
        string label,
        Func<PassResult, string?> cell)
    {
        List<string> cells = new List<string> { Markup.Escape(label) };
        foreach (PassResult pass in passes)
        {
            cells.Add(cell(pass) ?? NoValue);
        }

        table.AddRow(cells.ToArray());
    }

    private static Table NewTable()
    {
        Table table = new Table();
        table.Border(TableBorder.Square);

        return table;
    }

    private static void AddPassColumns(Table table, IReadOnlyList<PassResult> passes)
    {
        table.AddColumn(string.Empty);
        foreach (PassResult pass in passes)
        {
            table.AddColumn(Markup.Escape(pass.Profile.Name));
        }
    }

    // Текст из данных экранируем: квадратные скобки в нём библиотека приняла бы за разметку.
    private static void AddRow(Table table, string label, string value)
    {
        table.AddRow(Markup.Escape(label), Markup.Escape(value));
    }

    private static void WriteTable(string title, Table table)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]{title}[/]");
        AnsiConsole.Write(table);
    }

    private static bool IsSameSetting(ProfileSetting first, ProfileSetting second)
    {
        return string.Equals(first.Section, second.Section, StringComparison.OrdinalIgnoreCase)
            && string.Equals(first.Key, second.Key, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatStatus(PassStatus status)
    {
        if (status == PassStatus.Completed)
        {
            return "[green]OK[/]";
        }

        return "[red]FAILED[/]";
    }

    // Точка, а не запятая, на любом языке системы.
    private static string? FormatNumber(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    private static string? FormatNumber(double? value)
    {
        return value?.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string JoinKnown(params string?[] parts)
    {
        string? joined = JoinOrNull(", ", parts);
        if (joined is null)
        {
            return NotAvailable;
        }

        return joined;
    }

    private static string? FormatCores(CpuInfo cpu)
    {
        return JoinOrNull(" / ", WithUnit(cpu.Cores, "cores"), WithUnit(cpu.Threads, "threads"));
    }

    private static string? FormatMemory(int? megabytes)
    {
        if (megabytes is null)
        {
            return null;
        }

        if (megabytes < 1024)
        {
            return $"{megabytes} MB";
        }

        return $"{Math.Round(megabytes.Value / 1024.0)} GB";
    }

    private static string? WithUnit(int? value, string unit)
    {
        if (value is null)
        {
            return null;
        }

        return $"{value} {unit}";
    }

    private static string? WithLabel(string label, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return $"{label} {value}";
    }

    private static string? JoinOrNull(string separator, params string?[] parts)
    {
        List<string> known = new List<string>();
        foreach (string? part in parts)
        {
            if (part is not null)
            {
                known.Add(part);
            }
        }

        if (known.Count == 0)
        {
            return null;
        }

        return string.Join(separator, known);
    }
}
