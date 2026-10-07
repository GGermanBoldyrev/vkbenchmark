using System.Text;
using System.Text.RegularExpressions;

using WukongBench.Exceptions;
using WukongBench.Settings.Profiles;

namespace WukongBench.Settings;

// Файл настроек игры: умеет принять профиль, не трогая остальное содержимое.
internal sealed class SettingsFile(string path)
{
    private const string WindowsNewLine = "\r\n";

    // Строка со значениями меню утилиты. В профиле ей соответствует секция с тем же именем.
    private const string MenuSettingsKey = "UISettingData";

    // Параметр конструктора можно перезаписать, поле readonly — нельзя.
    private readonly string path = path;

    // Возвращает профиль с теми значениями, которые попали в файл.
    public BenchmarkProfile Apply(BenchmarkProfile profile)
    {
        try
        {
            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string text;

            // Кодировку определяем по метке в начале файла. Без метки считаем, что это UTF-8.
            using (StreamReader reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            string newLine = text.Contains(WindowsNewLine) ? WindowsNewLine : "\n";
            List<string> lines = text.Split(newLine).ToList();

            // После последнего перевода строки остаётся пустой хвост: убираем его и вернём при записи.
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            List<ProfileSetting> applied = new List<ProfileSetting>();
            foreach (ProfileSetting setting in profile.Settings)
            {
                ProfileSetting resolved = setting with { Value = Resolve(lines, setting.Value) };
                Set(lines, resolved);
                applied.Add(resolved);
            }

            File.WriteAllText(path, string.Join(newLine, lines) + newLine, encoding);

            return profile with { Settings = applied };
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot apply the profile {profile.Name} to the settings file {path}: {exception.Message}");
        }
    }

    // Значение вида {Ключ} берём из самого файла: так профиль ссылается на разрешение экрана.
    private static string Resolve(List<string> lines, string value)
    {
        if (!value.StartsWith('{') || !value.EndsWith('}'))
        {
            return value;
        }

        string key = value.Substring(1, value.Length - 2).Trim();
        string? line = lines.FirstOrDefault(item => IsKey(item, key));
        if (line is null)
        {
            throw new InvalidOperationException($"the key \"{key}\" is missing.");
        }

        return line.Substring(line.IndexOf('=') + 1).Trim();
    }

    // Ключ есть — заменяем строку. Ключа нет — добавляем в конец секции. Секции нет — дописываем её.
    private static void Set(List<string> lines, ProfileSetting setting)
    {
        if (string.Equals(setting.Section, MenuSettingsKey, StringComparison.OrdinalIgnoreCase))
        {
            SetMenuSetting(lines, setting);
            return;
        }

        string settingLine = $"{setting.Key}={setting.Value}";

        int sectionStart = lines.FindIndex(line => IsSection(line, setting.Section));
        if (sectionStart < 0)
        {
            if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length > 0)
            {
                lines.Add(string.Empty);
            }

            lines.Add($"[{setting.Section}]");
            lines.Add(settingLine);
            return;
        }

        // Секция тянется до следующего заголовка в скобках или до конца файла.
        int sectionEnd = sectionStart + 1;
        while (sectionEnd < lines.Count && !lines[sectionEnd].TrimStart().StartsWith('['))
        {
            if (IsKey(lines[sectionEnd], setting.Key))
            {
                lines[sectionEnd] = settingLine;
                return;
            }

            sectionEnd++;
        }

        // Новый ключ ставим перед пустыми строками, которые отделяют секцию от следующей.
        int insertAt = sectionEnd;
        while (insertAt > sectionStart + 1 && lines[insertAt - 1].Trim().Length == 0)
        {
            insertAt--;
        }

        lines.Insert(insertAt, settingLine);
    }

    // Значения меню лежат одной строкой: UISettingData=(("Имя", "значение"),...).
    private static void SetMenuSetting(List<string> lines, ProfileSetting setting)
    {
        int index = lines.FindIndex(line => IsKey(line, MenuSettingsKey));
        if (index < 0)
        {
            throw new InvalidOperationException($"the {MenuSettingsKey} line is missing.");
        }

        Regex pair = new Regex($"""\("{Regex.Escape(setting.Key)}",\s*"[^"]*"\)""");
        if (!pair.IsMatch(lines[index]))
        {
            throw new InvalidOperationException($"{MenuSettingsKey} has no setting \"{setting.Key}\".");
        }

        lines[index] = pair.Replace(lines[index], match => $"""("{setting.Key}", "{setting.Value}")""");
    }

    // Unreal не различает регистр в именах секций и ключей.
    private static bool IsSection(string line, string section)
    {
        return string.Equals(line.Trim(), $"[{section}]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKey(string line, string key)
    {
        int separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        return string.Equals(line.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase);
    }
}
