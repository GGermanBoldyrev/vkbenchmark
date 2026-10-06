namespace WukongBench.Tool;

// Что известно об утилите заранее и одинаково на любой машине.
internal static class BenchmarkTool
{
    public const string AppId = "3132990";

    // Где лежит файл настроек относительно папки установки.
    public static readonly string SettingsRelativePath =
        Path.Combine("b1", "Saved", "Config", "Windows", "GameUserSettings.ini");
}
