using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WukongBench;

// Отвечает на один вопрос: где на этой машине установлен Benchmark Tool.
public sealed class BenchmarkToolLocator
{
    public const string AppId = "3132990";

    private const string DefaultSteamDir = @"C:\Program Files (x86)\Steam";

    // Путь, заданный вручную, важнее автопоиска: если он неверный, Steam не обыскиваем,
    // чтобы молча не подставить другую установку.
    public BenchmarkTool? Find(string? toolDir = null)
    {
        if (toolDir is not null)
        {
            if (!Directory.Exists(toolDir))
            {
                return null;
            }

            return ToBenchmarkTool(toolDir);
        }

        string? steamDir = FindSteamDir();
        if (steamDir is null)
        {
            return null;
        }

        return FindInSteam(steamDir);
    }

    // Не зависит от ОС: только читает текстовые файлы Steam в указанной папке.
    public BenchmarkTool? FindInSteam(string steamDir)
    {
        foreach (string library in ReadLibraries(steamDir))
        {
            // Манифест лежит только в той библиотеке, куда установлена утилита.
            string manifest = Path.Combine(library, "steamapps", $"appmanifest_{AppId}.acf");
            if (!File.Exists(manifest))
            {
                continue;
            }

            // Имя папки утилиты записано в манифесте, угадывать его не нужно.
            string? folderName = ReadValues(manifest, "installdir").FirstOrDefault();
            if (folderName is null)
            {
                continue;
            }

            // Манифест может остаться после удаления, поэтому проверяем саму папку.
            string installDir = Path.Combine(library, "steamapps", "common", folderName);
            if (Directory.Exists(installDir))
            {
                return ToBenchmarkTool(installDir);
            }
        }

        return null;
    }

    // Единственный шаг, зависящий от ОС: где установлен сам Steam.
    private static string? FindSteamDir()
    {
        if (OperatingSystem.IsWindows())
        {
            // Steam записывает свою папку в реестр: для пользователя и для всей машины.
            string?[] fromRegistry =
            [
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string,
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string,
            ];

            foreach (string? dir in fromRegistry)
            {
                if (dir is not null && Directory.Exists(dir))
                {
                    return dir;
                }
            }
        }

        // Запасной вариант: папка, куда Steam ставится по умолчанию.
        if (Directory.Exists(DefaultSteamDir))
        {
            return DefaultSteamDir;
        }

        return null;
    }

    // Библиотеки — папки на разных дисках, куда Steam ставит игры.
    private static List<string> ReadLibraries(string steamDir)
    {
        // Папка самого Steam — тоже библиотека.
        List<string> libraries = [steamDir];

        string listFile = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        if (File.Exists(listFile))
        {
            libraries.AddRange(ReadValues(listFile, "path"));
        }

        return libraries;
    }

    // Файлы Steam (.vdf, .acf) состоят из строк вида:  "ключ"    "значение"
    private static List<string> ReadValues(string file, string key)
    {
        Regex pattern = new Regex($"""^\s*"{Regex.Escape(key)}"\s+"(.*)"\s*$""", RegexOptions.IgnoreCase);
        List<string> values = new List<string>();

        foreach (string line in File.ReadLines(file))
        {
            Match match = pattern.Match(line);
            if (match.Success)
            {
                // Обратная косая черта в этих файлах удвоена: "D:\\SteamLibrary".
                values.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
            }
        }

        return values;
    }

    private static BenchmarkTool ToBenchmarkTool(string installDir)
    {
        string settingsPath = Path.Combine(installDir, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

        return new BenchmarkTool(installDir, settingsPath);
    }
}
