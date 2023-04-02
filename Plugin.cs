using System;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class Plugin : IMoBroPlugin
{
  private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(1000);
  private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

  private readonly IMoBroSettings _settings;
  private readonly IMoBroService _service;
  private readonly IMoBroScheduler _scheduler;

  private readonly LibreHardwareMonitor _libre;

  public Plugin(IMoBroSettings settings, IMoBroService service, IMoBroScheduler scheduler)
  {
    _settings = settings;
    _service = service;
    _scheduler = scheduler;
    _libre = new LibreHardwareMonitor();
  }

  public void Init()
  {
    // update LibreHardwareMonitor according to settings
    _libre.Update(_settings);

    // register groups and metrics
    _service.Register(_libre.GetMetricItems());

    // start polling metric values
    _scheduler.Interval(UpdateMetricValues, UpdateInterval, InitialDelay);
  }

  private void UpdateMetricValues()
  {
    _service.UpdateMetricValues(_libre.GetMetricValues());
  }

  public void Dispose()
  {
    _libre.Dispose();
  }
}