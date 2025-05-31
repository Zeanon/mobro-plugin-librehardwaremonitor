using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.LibreHardwareMonitor.Extensions;
using MoBro.Plugin.LibreHardwareMonitor.Model;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class LibreHardwareMonitor : IDisposable
{
    private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

    private readonly Computer _computer = new();

    public void Update(IMoBroSettings settings)
    {
        _computer.IsCpuEnabled = settings.GetValue<bool>("cpu_enabled");
        _computer.IsGpuEnabled = settings.GetValue<bool>("gpu_enabled");
        _computer.IsMemoryEnabled = settings.GetValue<bool>("ram_enabled");
        _computer.IsMotherboardEnabled = settings.GetValue<bool>("motherboard_enabled");
        _computer.IsStorageEnabled = settings.GetValue<bool>("hdd_enabled");
        _computer.IsNetworkEnabled = settings.GetValue<bool>("network_enabled");
        _computer.IsControllerEnabled = settings.GetValue<bool>("controller_enabled");
        _computer.IsPsuEnabled = settings.GetValue<bool>("psu_enabled");
        _computer.IsBatteryEnabled = settings.GetValue<bool>("battery_enabled");

        _computer.Reset();
        _computer.Open();
    }

    public IEnumerable<IMoBroItem> GetMetricItems()
    {
        IEnumerable<Sensor> sensors = GetSensors();
        Sensor[] sensorsArray = sensors as Sensor[] ?? sensors.ToArray();
        IEnumerable<IMoBroItem> metrics = sensorsArray
            .Select(IMoBroItem (sensor) => sensor.AsMetric());

        IEnumerable<IMoBroItem> groups = sensorsArray
            .Select(IMoBroItem (sensor) => sensor.AsGroup())
            .DistinctBy(g => g.Id);

        return groups.Concat(metrics);
    }

    public IEnumerable<MetricValue> GetMetricValues()
    {
        return GetSensors().Select(s => s.AsMetricValue());
    }

    private IEnumerable<Sensor> GetSensors()
    {
        return _computer.Hardware.SelectMany(h => GetSensors(h.HardwareType, h));
    }

    private static List<Sensor> GetSensors(HardwareType rootType, IHardware hardware)
    {
        // first update the sensors information 
        hardware.Update();

        List<Sensor> list;

        if (rootType == HardwareType.GpuAmd
            || rootType == HardwareType.GpuNvidia
            || rootType == HardwareType.GpuIntel)
        {
            int i = 0;
            int index = -1;
            float? memoryUsed = null;
            float? memoryTotal = null;
            // parse sensors 
            list = hardware.Sensors.Select(sensor =>
            {
                if (sensor.Name == "GPU Memory Used")
                {
                    memoryUsed = sensor.Value;
                }
                else if (sensor.Name == "GPU Memory Total")
                {
                    memoryTotal = sensor.Value;
                }
                else if (sensor.Name == "GPU Memory" && sensor.SensorType == SensorType.Load)
                {
                    index = i;
                }
                i++;
                return new Sensor(
                  SanitizeId(sensor.Identifier.ToString()),
                  sensor.Name,
                  sensor.Value,
                  sensor.SensorType,
                  rootType,
                  SanitizeId(sensor.Hardware.Identifier.ToString()),
                  sensor.Hardware.Name
                );
            }).ToList();

            // recursively parse and add sensors of sub-hardware
            foreach (IHardware? sub in hardware.SubHardware)
            {
                list.AddRange(GetSensors(rootType, sub));
            }

            if (index != -1 && memoryUsed != null && memoryTotal != null)
            {
                list[index] = new Sensor(
                            list[index].Id,
                            list[index].Name,
                            100 * memoryUsed / memoryTotal,
                            list[index].SensorType,
                            list[index].HardwareType,
                            list[index].GroupId,
                            list[index].GroupName);
            }
        }
        else
        {
            // parse sensors 
            list = hardware.Sensors.Select(sensor => new Sensor(
                SanitizeId(sensor.Identifier.ToString()),
                sensor.Name,
                sensor.Value,
                sensor.SensorType,
                rootType,
                SanitizeId(sensor.Hardware.Identifier.ToString()),
                sensor.Hardware.Name
            )).ToList();

            // recursively parse and add sensors of sub-hardware
            foreach (IHardware? sub in hardware.SubHardware)
            {
                list.AddRange(GetSensors(rootType, sub));
            }
        }

        return list;
    }

    private static string SanitizeId(string id)
    {
        return IdSanitationRegex.Replace(id, "");
    }

    public void Dispose()
    {
        _computer.Close();
    }
}