using WukongBench.Exceptions;

namespace WukongBench.Settings.Profiles;

// Читает профили из папки. Каждый .ini в ней — один проход бенчмарка.
public sealed class ProfileLoader
{
    private const string ProfileExtension = ".ini";

    // Порядок проходов — по имени файла: 01-cpu.ini идёт раньше 02-gpu.ini.
    public IReadOnlyList<BenchmarkProfile> LoadAll(string profilesDir)
    {
        if (!Directory.Exists(profilesDir))
        {
            throw new BenchmarkException($"Profiles folder not found: {profilesDir}");
        }

        List<BenchmarkProfile> profiles = new List<BenchmarkProfile>();

        foreach (string file in Directory.GetFiles(profilesDir, $"*{ProfileExtension}").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            profiles.Add(Load(file));
        }

        if (profiles.Count == 0)
        {
            throw new BenchmarkException($"No profiles (*{ProfileExtension}) found in {profilesDir}");
        }

        return profiles;
    }
    
    private static BenchmarkProfile Load(string file)
    {
        string name = Path.GetFileNameWithoutExtension(file);
        List<ProfileSetting> settings = new List<ProfileSetting>();
        string? section = null;

        foreach (string rawLine in File.ReadLines(file))
        {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line.Substring(1, line.Length - 2).Trim();
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new BenchmarkException($"Profile {name}: cannot read the line \"{line}\". Expected Key=Value.");
            }

            if (section is null)
            {
                throw new BenchmarkException($"Profile {name}: the setting \"{line}\" is outside of a [section].");
            }

            string key = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).Trim();

            // Повтор ключа в секции — ошибка в профиле: непонятно, какое из значений нужно.
            foreach (ProfileSetting existing in settings)
            {
                if (string.Equals(existing.Section, section, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BenchmarkException($"Profile {name}: duplicate setting \"{key}\" in [{section}].");
                }
            }

            settings.Add(new ProfileSetting(section, key, value));
        }

        if (settings.Count == 0)
        {
            throw new BenchmarkException($"Profile {name} has no settings.");
        }

        return new BenchmarkProfile(name, settings);
    }
}
