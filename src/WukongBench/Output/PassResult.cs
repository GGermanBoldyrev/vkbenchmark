using WukongBench.Results;
using WukongBench.Settings.Profiles;

namespace WukongBench.Output;

// Итог одного прохода: с каким профилем шёл, чем закончился и что показала утилита.
internal sealed record PassResult(BenchmarkProfile Profile, PassStatus Status, BenchmarkResult? Result);
