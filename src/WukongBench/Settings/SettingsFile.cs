using System.Text;

using WukongBench.Exceptions;
using WukongBench.Settings.Profiles;

namespace WukongBench.Settings;

// Файл настроек игры: умеет принять профиль, не трогая остальное содержимое.
internal sealed class SettingsFile(string path)
{
    private const string WindowsNewLine = "\r\n";

    // Параметр конструктора можно перезаписать, поле readonly — нельзя.
    private readonly string path = path;

    public void Apply(BenchmarkProfile profile)
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

            foreach (ProfileSetting setting in profile.Settings)
            {
                Set(lines, setting);
            }

            File.WriteAllText(path, string.Join(newLine, lines) + newLine, encoding);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot apply the profile {profile.Name} to the settings file {path}: {exception.Message}");
        }
    }

    // Ключ есть — заменяем строку. Ключа нет — добавляем в конец секции. Секции нет — дописываем её.
    private static void Set(List<string> lines, ProfileSetting setting)
    {
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
