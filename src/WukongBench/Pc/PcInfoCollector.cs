using System.Management;

using Vortice.DXGI;

using WukongBench.Pc.Info;

namespace WukongBench.Pc;

// Собирает характеристики компьютера.
internal sealed class PcInfoCollector
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

                    // Программный адаптер Windows — не видеокарта: настоящей в системе нет.
                    if (description.Flags.HasFlag(AdapterFlags.Software))
                    {
                        return new GpuInfo(Name: null, VramMb: null, DriverVersion: null);
                    }

                    // Именно ulong: в uint не помещаются 4 ГБ и больше.
                    ulong vramBytes = (ulong)description.DedicatedVideoMemory;

                    return new GpuInfo(
                        Name: description.Description,
                        VramMb: (int)(vramBytes / BytesInMegabyte),
                        DriverVersion: FindDriverVersion(description.VendorId, description.DeviceId));
                }
            }
        }
        catch (Exception)
        {
            return new GpuInfo(Name: null, VramMb: null, DriverVersion: null);
        }
    }

    // Версии драйвера в DXGI нет, поэтому берём её из WMI. Карту находим по кодам
    // производителя и модели: они есть в обоих источниках и не зависят от написания названия.
    private static string? FindDriverVersion(uint vendorId, uint deviceId)
    {
        try
        {
            string hardwareId = $"VEN_{vendorId:X4}&DEV_{deviceId:X4}";

            foreach (Dictionary<string, object?> row in Query(
                "SELECT PNPDeviceID, DriverVersion FROM Win32_VideoController"))
            {
                string? deviceInstance = ReadText(row, "PNPDeviceID");
                if (deviceInstance is not null
                    && deviceInstance.Contains(hardwareId, StringComparison.OrdinalIgnoreCase))
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
