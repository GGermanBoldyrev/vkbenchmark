using WukongBench.Settings.Profiles;

namespace WukongBench.Output;

// Итог одного прохода: с каким профилем шёл и чем закончился.
public sealed record PassResult(BenchmarkProfile Profile, PassStatus Status);
