using System.Text.Json;

using WukongBench.Exceptions;

namespace WukongBench.Results;

// Читает файл результатов утилиты: JSON с итогами, настройками прохода и записью на каждый кадр.
internal sealed class BenchmarkResultReader
{
    // Поля файла с настройками прохода.
    private static readonly string[] SettingNames =
    [
        "ScreenMode",
        "ScreenResolution",
        "QualityLevel",
        "ImageQuality",
        "ViewDistance",
        "AntiAliasing",
        "PostProcessing",
        "ShadowQuality",
        "TextureQuality",
        "MaterialQuality",
        "VegetationQuality",
        "MotionBlur",
        "Rtx",
        "Dlss",
        "InsertFrame",
        "Dx12",
    ];

    public BenchmarkResult Read(string path)
    {
        try
        {
            using (FileStream stream = File.OpenRead(path))
            {
                using (JsonDocument document = JsonDocument.Parse(stream))
                {
                    JsonElement root = document.RootElement;

                    // FPS95 в файле — это «5-й перцентиль» на экране результатов.
                    return new BenchmarkResult(
                        FpsAverage: root.GetProperty("FPSAvg").GetInt32(),
                        FpsMax: root.GetProperty("FPSMax").GetInt32(),
                        FpsMin: root.GetProperty("FPSMin").GetInt32(),
                        FpsPercentile5: root.GetProperty("FPS95").GetInt32(),
                        VideoMemoryGb: root.GetProperty("VideoMem").GetDouble(),
                        CpuFrameTimeMs: AverageOfFrames(root, "CPUFrameTime"),
                        GpuFrameTimeMs: AverageOfFrames(root, "GPUFrameTime"),
                        DurationSeconds: ReadDuration(root),
                        Settings: ReadSettings(root));
                }
            }
        }
        catch (Exception exception)
        {
            throw new BenchmarkException($"Cannot read the results file {path}: {exception.Message}");
        }
    }

    private static List<ReportedSetting> ReadSettings(JsonElement root)
    {
        List<ReportedSetting> settings = new List<ReportedSetting>();

        foreach (string name in SettingNames)
        {
            if (root.TryGetProperty(name, out JsonElement value))
            {
                // В разрешении стоит знак умножения, которого нет в кодировке старой консоли.
                settings.Add(new ReportedSetting(name, value.ToString().Replace(" × ", "x")));
            }
        }

        return settings;
    }

    // Итогового времени кадра в файле нет, поэтому усредняем покадровые записи.
    private static double? AverageOfFrames(JsonElement root, string field)
    {
        if (!TryGetFrames(root, out JsonElement frames))
        {
            return null;
        }

        double sum = 0;
        foreach (JsonElement frame in frames.EnumerateArray())
        {
            sum += frame.GetProperty(field).GetDouble();
        }

        return sum / frames.GetArrayLength();
    }

    private static double? ReadDuration(JsonElement root)
    {
        if (!TryGetFrames(root, out JsonElement frames))
        {
            return null;
        }

        // Отметки времени в файле — в миллисекундах.
        long first = frames[0].GetProperty("TimeStamp").GetInt64();
        long last = frames[frames.GetArrayLength() - 1].GetProperty("TimeStamp").GetInt64();

        return (last - first) / 1000.0;
    }

    private static bool TryGetFrames(JsonElement root, out JsonElement frames)
    {
        return root.TryGetProperty("Records", out frames)
            && frames.ValueKind == JsonValueKind.Array
            && frames.GetArrayLength() > 0;
    }
}
