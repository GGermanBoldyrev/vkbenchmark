namespace WukongBench.Pc.Info;

internal sealed record PcInfo(
    CpuInfo Cpu,
    GpuInfo Gpu,
    RamInfo Ram,
    OsInfo Os);
