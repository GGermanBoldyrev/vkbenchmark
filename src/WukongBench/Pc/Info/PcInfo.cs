namespace WukongBench.Pc.Info;

public sealed record PcInfo(
    CpuInfo Cpu,
    GpuInfo Gpu,
    RamInfo Ram,
    OsInfo Os);
