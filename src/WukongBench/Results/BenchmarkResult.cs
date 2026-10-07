namespace WukongBench.Results;

// Что утилита записала о проходе: показатели и настройки, с которыми он шёл.
internal sealed record BenchmarkResult(
    int FpsAverage,
    int FpsMax,
    int FpsMin,
    int FpsPercentile5,
    double VideoMemoryGb,
    double? CpuFrameTimeMs,
    double? GpuFrameTimeMs,
    double? DurationSeconds,
    IReadOnlyList<ReportedSetting> Settings);
