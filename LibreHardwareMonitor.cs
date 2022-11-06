using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.LibreHardwareMonitor.Extensions;
using MoBro.Plugin.LibreHardwareMonitor.Model;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class LibreHardwareMonitor : IMoBroPlugin
{
  private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

  private readonly Computer _computer;

  public LibreHardwareMonitor()
  {
    _computer = new Computer();
    _computer.Open();
  }

  public Task Init(IPluginSettings settings, IMoBro mobro)
  {
    // update computer settings
    SetComputerSettings(settings);
    _computer.Reset();

    // register groups and metrics
    mobro.Register(ParseMetricItems().ToArray());
    return Task.CompletedTask;
  }

  public Task<IEnumerable<IMetricValue>> GetMetricValues(IList<string> ids)
  {
    var idSet = ids.ToImmutableHashSet();
    var now = DateTime.UtcNow;
    var values = _computer.Hardware
      .Peek(h => h.Update())
      .SelectMany(GetSensors)
      .Where(s => idSet.Contains(s.Id))
      .Select(s => new MetricValue(s.Id, now, GetMetricValue(s)))
      .Cast<IMetricValue>()
      .ToList();
    return Task.FromResult<IEnumerable<IMetricValue>>(values);
  }

  private IEnumerable<IMoBroItem> ParseMetricItems()
  {
    var metrics = _computer.Hardware
      .SelectMany(GetSensors)
      .Select(sensor => MoBroItem
        .CreateMetric()
        .WithId(sensor.Id)
        .WithLabel(sensor.Name)
        .OfType(GetMetricType(sensor.SensorType))
        .OfCategory(GetCategory(sensor.HardwareType))
        .OfGroup(sensor.GroupId)
        .Build()
      )
      .Select(m => m as IMoBroItem);

    var groups = _computer.Hardware
      .Select(h => MoBroItem.CreateGroup()
        .WithId(SanitizeId(h.Identifier.ToString()))
        .WithLabel(h.Name)
        .WithoutIcon()
        .Build()
      )
      .Select(m => m as IMoBroItem)
      .Distinct();

    return metrics.Concat(groups);
  }

  private static object? GetMetricValue(in Sensor sensor)
  {
    if (sensor.Value == null) return null;

    var doubleVal = Convert.ToDouble(sensor.Value);
    return sensor.SensorType switch
    {
      SensorType.Throughput => doubleVal * 8, // bytes => bit
      SensorType.Clock => doubleVal * 1_000_000, // MHz => Hertz
      SensorType.SmallData => doubleVal * 1_000_000, // MB => Byte
      SensorType.Data => doubleVal * 1_000_000_000, // GB => Byte
      SensorType.TimeSpan => TimeSpan.FromSeconds(doubleVal), // convert to TimeSpan
      _ => doubleVal
    };
  }

  private static CoreMetricType GetMetricType(SensorType sensorType)
  {
    switch (sensorType)
    {
      case SensorType.Voltage:
        return CoreMetricType.ElectricPotential;
      case SensorType.Clock:
      case SensorType.Frequency:
        return CoreMetricType.Frequency;
      case SensorType.Temperature:
        return CoreMetricType.Temperature;
      case SensorType.Load:
      case SensorType.Control:
      case SensorType.Level:
        return CoreMetricType.Usage;
      case SensorType.Power:
        return CoreMetricType.Power;
      case SensorType.SmallData:
      case SensorType.Data:
        return CoreMetricType.Data;
      case SensorType.Throughput:
        return CoreMetricType.DataFlow;
      case SensorType.Fan:
        return CoreMetricType.Rotation;
      case SensorType.Factor:
        return CoreMetricType.Multiplier;
      case SensorType.Current:
        return CoreMetricType.ElectricCurrent;
      case SensorType.Flow:
        return CoreMetricType.VolumeFlow;
      case SensorType.TimeSpan:
        return CoreMetricType.Duration;
      case SensorType.Energy:
      default:
        return CoreMetricType.Numeric;
    }
  }

  private static CoreCategory GetCategory(HardwareType hardwareType)
  {
    switch (hardwareType)
    {
      case HardwareType.Cpu:
        return CoreCategory.Cpu;
      case HardwareType.GpuNvidia:
      case HardwareType.GpuAmd:
      case HardwareType.GpuIntel:
        return CoreCategory.Gpu;
      case HardwareType.Memory:
        return CoreCategory.Ram;
      case HardwareType.Storage:
        return CoreCategory.Storage;
      case HardwareType.Motherboard:
        return CoreCategory.Mainboard;
      case HardwareType.Network:
        return CoreCategory.Network;
      case HardwareType.Battery:
        return CoreCategory.Battery;
      case HardwareType.SuperIO:
      case HardwareType.Cooler:
      case HardwareType.EmbeddedController:
      case HardwareType.Psu:
      default:
        return CoreCategory.Miscellaneous;
    }
  }

  private static IEnumerable<Sensor> GetSensors(IHardware hardware)
  {
    List<ISensor> list = new();
    GetSensors(hardware, ref list);
    return list.Select(s => new Sensor(
      SanitizeId(s.Identifier.ToString()),
      s.Name,
      s.Value,
      s.SensorType,
      s.Hardware.HardwareType,
      SanitizeId(s.Hardware.Identifier.ToString())
    ));
  }

  private static void GetSensors(IHardware hardware, ref List<ISensor> collection)
  {
    collection.AddRange(hardware.Sensors);
    foreach (var hw in hardware.SubHardware)
    {
      GetSensors(hw, ref collection);
    }
  }

  private static string SanitizeId(string id)
  {
    return IdSanitationRegex.Replace(id, "");
  }

  private void SetComputerSettings(IPluginSettings settings)
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
  }

  public void Dispose()
  {
    _computer.Close();
  }
}