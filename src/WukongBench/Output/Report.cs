using WukongBench.Pc.Info;
using WukongBench.Tool;

namespace WukongBench.Output;

// Всё, что показывает итоговый отчёт.
public sealed record Report(PcInfo Pc, BenchmarkToolInstallation Tool, IReadOnlyList<PassResult> Passes);
