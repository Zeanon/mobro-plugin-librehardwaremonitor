using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using MoBro.Plugin.LibreHardwareMonitor.Extensions;
using MoBro.Plugin.LibreHardwareMonitor.Model;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class LibreHardwareMonitor : IMoBroPlugin
{
  private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);
  private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(1000);
  private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

  private readonly IMoBroSettings _settings;
  private readonly IMoBroService _service;
  private readonly IMoBroScheduler _scheduler;
  private readonly Computer _computer;

  public LibreHardwareMonitor(IMoBroSettings settings, IMoBroService service, IMoBroScheduler scheduler)
  {
    _settings = settings;
    _service = service;
    _scheduler = scheduler;
    _computer = new Computer();
  }

  public void Init()
  {
    // update computer settings
    _computer.IsCpuEnabled = _settings.GetValue<bool>("cpu_enabled");
    _computer.IsGpuEnabled = _settings.GetValue<bool>("gpu_enabled");
    _computer.IsMemoryEnabled = _settings.GetValue<bool>("ram_enabled");
    _computer.IsMotherboardEnabled = _settings.GetValue<bool>("motherboard_enabled");
    _computer.IsStorageEnabled = _settings.GetValue<bool>("hdd_enabled");
    _computer.IsNetworkEnabled = _settings.GetValue<bool>("network_enabled");
    _computer.IsControllerEnabled = _settings.GetValue<bool>("controller_enabled");
    _computer.IsPsuEnabled = _settings.GetValue<bool>("psu_enabled");
    _computer.IsBatteryEnabled = _settings.GetValue<bool>("battery_enabled");
    _computer.Open();

    // register groups and metrics
    _service.Register(ParseMetricItems());

    // start polling metric values
    _scheduler.Interval(Update, UpdateInterval, InitialDelay);
  }

  private void Update()
  {
    var now = DateTime.UtcNow;
    var values = _computer.Hardware
      .Peek(h => h.Update())
      .SelectMany(GetSensors)
      .Select(s => new MetricValue(s.Id, now, GetMetricValue(s)));

    _service.UpdateMetricValues(values);
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

  public void Dispose()
  {
    _computer.Close();
  }
}