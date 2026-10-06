namespace WukongBench.Tool;

// Где утилита лежит на этой машине. Всё остальное выводится из папки установки.
internal sealed record BenchmarkToolInstallation(string InstallDir)
{
    public string SettingsPath
    {
        get
        {
            return Path.Combine(InstallDir, BenchmarkTool.SettingsRelativePath);
        }
    }
}
