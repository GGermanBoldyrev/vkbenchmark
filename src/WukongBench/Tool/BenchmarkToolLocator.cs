using System.Text.RegularExpressions;
using Microsoft.Win32;
using WukongBench.Exceptions;

namespace WukongBench.Tool;

// Отвечает на один вопрос: где на этой машине установлен Benchmark Tool.
public sealed class BenchmarkToolLocator
{
    private const string DefaultSteamDir = @"C:\Program Files (x86)\Steam";
    
    public BenchmarkToolInstallation Find(string? toolDir = null)
    {
        string installDir = toolDir ?? FindInstallDirViaSteam();

        return ToInstallation(installDir);
    }
    
    private string FindInstallDirViaSteam()
    {
        string steamDir = FindSteamDir();

        string? installDir = FindInSteam(steamDir);
        if (installDir is null)
        {
            throw new BenchmarkException(
                $"Benchmark Tool is not installed. Install it via Steam (AppID {BenchmarkTool.AppId}).");
        }

        return installDir;
    }
    
    private static string FindSteamDir()
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

        // Запасной вариант: папка, куда Steam ставится по умолчанию.
        if (Directory.Exists(DefaultSteamDir))
        {
            return DefaultSteamDir;
        }

        throw new BenchmarkException("Steam installation not found.");
    }
    
    public string? FindInSteam(string steamDir)
    {
        foreach (string library in ReadLibraries(steamDir))
        {
            // Манифест лежит только в той библиотеке, куда установлена утилита.
            string manifest = Path.Combine(library, "steamapps", $"appmanifest_{BenchmarkTool.AppId}.acf");
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
                return installDir;
            }
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

    // Файлы Steam (.vdf, .acf) состоят из строк вида: "ключ" "значение"
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

    // Общая проверка для обоих источников: годится ли папка как установка утилиты.
    private static BenchmarkToolInstallation ToInstallation(string installDir)
    {
        if (!Directory.Exists(installDir))
        {
            throw new BenchmarkException($"Folder not found: {installDir}");
        }

        return new BenchmarkToolInstallation(installDir);
    }
}
