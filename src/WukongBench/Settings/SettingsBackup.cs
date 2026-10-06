using WukongBench.Exceptions;

namespace WukongBench.Settings;

// Резервная копия файла настроек игры: сохранить до наших правок, возвращать перед каждым проходом и в конце.
internal sealed class SettingsBackup(string settingsPath)
{
    // Параметр конструктора можно перезаписать, поле readonly — нельзя.
    private readonly string settingsPath = settingsPath;
    private readonly string backupPath = settingsPath + ".wukongbench-backup";

    public void Create()
    {
        // Копия уже есть: прошлый запуск не дошёл до конца, и в ней настоящие настройки.
        if (File.Exists(backupPath))
        {
            return;
        }

        if (!File.Exists(settingsPath))
        {
            throw new BenchmarkException($"Settings file not found: {settingsPath}");
        }

        try
        {
            File.Copy(settingsPath, backupPath);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException($"Cannot back up the settings file {settingsPath}: {exception.Message}");
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

            File.Copy(backupPath, settingsPath, overwrite: true);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot restore the settings file {settingsPath}: {exception.Message} " +
                $"The original settings are kept in {backupPath}");
        }
    }

    public void Delete()
    {
        try
        {
            File.Delete(backupPath);
        }
        catch (Exception exception)
        {
            throw new BenchmarkException(
                $"Cannot delete the settings backup {backupPath}: {exception.Message}");
        }
    }
}
