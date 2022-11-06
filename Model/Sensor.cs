using LibreHardwareMonitor.Hardware;

namespace MoBro.Plugin.LibreHardwareMonitor.Model;

internal readonly record struct Sensor(
  string Id,
  string Name,
  float? Value,
  SensorType SensorType,
  HardwareType HardwareType,
  string GroupId
);