using Spectre.Console;

using WukongBench.Pc.Info;
using WukongBench.Settings.Profiles;

namespace WukongBench.Output;

// Итоговый отчёт из трёх таблиц: система, результаты, настройки.
public sealed class ReportWriter
{
    // Значение не удалось определить.
    private const string NotAvailable = "n/a";

    // Профиль эту настройку не задаёт.
    private const string NotSet = "—";

    public void Write(Report report)
    {
        WriteSystem(report);
        WriteResults(report.Passes);
        WriteSettings(report.Passes);
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

        List<string> statusRow = new List<string> { "Status" };
        foreach (PassResult pass in passes)
        {
            statusRow.Add(FormatStatus(pass.Status));
        }

        table.AddRow(statusRow.ToArray());

        WriteTable("RESULTS", table);
    }

    // Строки — объединение настроек всех профилей в порядке их появления.
    private static void WriteSettings(IReadOnlyList<PassResult> passes)
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
                    cells.Add(NotSet);
                }
                else
                {
                    cells.Add(Markup.Escape(setting.Value));
                }
            }

            table.AddRow(cells.ToArray());
        }

        WriteTable("SETTINGS", table);
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

        if (status == PassStatus.Failed)
        {
            return "[red]FAILED[/]";
        }

        return "[grey]not run[/]";
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
