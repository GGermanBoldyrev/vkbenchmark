namespace WukongBench.Tool;

// Что известно об утилите заранее и одинаково на любой машине.
internal static class BenchmarkTool
{
    public const string AppId = "3132990";

    // Загрузчик запускает саму утилиту и передаёт ей свои параметры.
    public const string LauncherFileName = "b1_benchmark.exe";

    public const string LauncherProcessName = "b1_benchmark";

    // Процесс самой утилиты: окно принадлежит ему.
    public const string ProcessName = "b1-Win64-Shipping";

    // С этим параметром тест начинается сам, без заставки, меню и подтверждения.
    public const string AutoStartArgument = "-benchmark";

    // Где лежит файл настроек относительно папки установки.
    public static readonly string SettingsRelativePath =
        Path.Combine("b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

    // Куда утилита пишет результаты относительно временной папки: по файлу на проход.
    public static readonly string ResultsRelativePath =
        Path.Combine("b1", "BenchMarkHistory", "Tool");
}
