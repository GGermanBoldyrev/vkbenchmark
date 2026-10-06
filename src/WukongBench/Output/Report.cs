using WukongBench.Pc.Info;
using WukongBench.Tool;

namespace WukongBench.Output;

// Всё, что показывает итоговый отчёт.
internal sealed record Report(PcInfo Pc, BenchmarkToolInstallation Tool, IReadOnlyList<PassResult> Passes);
