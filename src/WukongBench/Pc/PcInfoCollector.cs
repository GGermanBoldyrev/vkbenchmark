using System.Management;
using Vortice.DXGI;
using WukongBench.Pc.Info;

namespace WukongBench.Pc;

// Собирает характеристики компьютера.
public sealed class PcInfoCollector
{
    private const long BytesInMegabyte = 1024 * 1024;

    public PcInfo Collect()
    {
        CpuInfo cpu = CollectCpu();
        GpuInfo gpu = CollectGpu();
        RamInfo ram = CollectRam();
        OsInfo os = CollectOs();

        return new PcInfo(cpu, gpu, ram, os);
    }

    private static CpuInfo CollectCpu()
    {
        try
        {
            Dictionary<string, object?>? row = Query(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor").FirstOrDefault();

            return new CpuInfo(
                Name: ReadText(row, "Name"),
                Cores: (int?)ReadNumber(row, "NumberOfCores"),
                Threads: (int?)ReadNumber(row, "NumberOfLogicalProcessors"));
        }
        catch (Exception)
        {
            return new CpuInfo(Name: null, Cores: null, Threads: null);
        }
    }

    private static GpuInfo CollectGpu()
    {
        try
        {
            using (IDXGIFactory6 factory = DXGI.CreateDXGIFactory1<IDXGIFactory6>())
            {
                using (IDXGIAdapter1 adapter = factory.EnumAdapterByGpuPreference<IDXGIAdapter1>(
                    0, GpuPreference.HighPerformance))
                {
                    AdapterDescription1 description = adapter.Description1;
                    string name = description.Description;
                    long vramBytes = (long)description.DedicatedVideoMemory;

                    return new GpuInfo(
                        Name: name,
                        VramMb: (int)(vramBytes / BytesInMegabyte),
                        DriverVersion: FindDriverVersion(name));
                }
            }
        }
        catch (Exception)
        {
            return new GpuInfo(Name: null, VramMb: null, DriverVersion: null);
        }
    }
    
    private static string? FindDriverVersion(string gpuName)
    {
        try
        {
            foreach (Dictionary<string, object?> row in Query(
                "SELECT Name, DriverVersion FROM Win32_VideoController"))
            {
                if (ReadText(row, "Name") == gpuName)
                {
                    return ReadText(row, "DriverVersion");
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private static RamInfo CollectRam()
    {
        try
        {
            long totalBytes = 0;

            foreach (Dictionary<string, object?> row in Query("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                totalBytes += ReadNumber(row, "Capacity") ?? 0;
            }

            if (totalBytes == 0)
            {
                return new RamInfo(TotalGb: null);
            }

            return new RamInfo(TotalGb: (int)Math.Round(totalBytes / (1024.0 * BytesInMegabyte)));
        }
        catch (Exception)
        {
            return new RamInfo(TotalGb: null);
        }
    }

    private static OsInfo CollectOs()
    {
        try
        {
            Dictionary<string, object?>? row = Query(
                "SELECT Caption, BuildNumber FROM Win32_OperatingSystem").FirstOrDefault();

            return new OsInfo(
                Name: ReadText(row, "Caption"),
                Build: ReadText(row, "BuildNumber"));
        }
        catch (Exception)
        {
            return new OsInfo(Name: null, Build: null);
        }
    }

    // Один запрос к WMI — службе Windows со сведениями о железе.
    // Каждая строка ответа — словарь «имя поля → значение».
    private static List<Dictionary<string, object?>> Query(string query)
    {
        List<Dictionary<string, object?>> rows = new List<Dictionary<string, object?>>();

        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
        {
            using (ManagementObjectCollection found = searcher.Get())
            {
                foreach (ManagementBaseObject item in found)
                {
                    using (item)
                    {
                        Dictionary<string, object?> row = new Dictionary<string, object?>();
                        foreach (PropertyData property in item.Properties)
                        {
                            row[property.Name] = property.Value;
                        }

                        rows.Add(row);
                    }
                }
            }
        }

        return rows;
    }

    private static string? ReadText(Dictionary<string, object?>? row, string field)
    {
        if (row is not null
            && row.TryGetValue(field, out object? value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text))
        {
            return text.Trim();
        }

        return null;
    }

    // WMI отдаёт числа разных типов (uint, ulong), поэтому приводим всё к long.
    private static long? ReadNumber(Dictionary<string, object?>? row, string field)
    {
        if (row is not null && row.TryGetValue(field, out object? value) && value is not null)
        {
            return Convert.ToInt64(value);
        }

        return null;
    }
}
