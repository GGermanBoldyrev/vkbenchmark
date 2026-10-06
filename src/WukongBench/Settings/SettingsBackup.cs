using WukongBench.Exceptions;

namespace WukongBench.Settings;

// Резервная копия файла настроек игры: сохранить до наших правок, вернуть после.
public sealed class SettingsBackup(string settingsPath)
{
    private readonly string backupPath = settingsPath + ".wukongbench-backup";
    
    public void Create()
    {
        try
        {
            // Копия уже есть: прошлый запуск не дошёл до конца, и в ней настоящие настройки.
            if (File.Exists(backupPath))
            {
                return;
            }

            if (File.Exists(settingsPath))
            {
                File.Copy(settingsPath, backupPath);
            }
            else
            {
                // Настроек ещё нет: пустая копия служит меткой «оригинала не было».
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.WriteAllBytes(backupPath, []);
            }
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot back up the settings file {settingsPath}: {exception.Message}");
        }
    }
    
    public void Restore()
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return;
            }

            if (new FileInfo(backupPath).Length == 0)
            {
                // Оригинала не было
                File.Delete(settingsPath);
            }
            else
            {
                File.Copy(backupPath, settingsPath, overwrite: true);
            }

            File.Delete(backupPath);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot restore the settings file {settingsPath}: {exception.Message} " +
                $"The original settings are kept in {backupPath}");
        }
    }
}
