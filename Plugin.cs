﻿using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;
using System;

namespace MoBro.Plugin.LibreHardwareMonitor;

public class Plugin : IMoBroPlugin, IDisposable
{
    private const int DefaultInitialDelay = 1;
    private const int DefaultUpdateFrequencyMs = 1000;
    private const int DefaultInitDelay = 0;

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
        int initDelay = _settings.GetValue("init_delay", DefaultInitDelay);
        _scheduler.OneOff(InitLibre, TimeSpan.FromSeconds(initDelay));
    }

    private void InitLibre()
    {
        // update LibreHardwareMonitor according to settings
        _libre.Update(_settings);

        // register groups and metrics
        _service.Register(_libre.GetMetricItems());

        // start polling metric values
        int updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);
        _scheduler.Interval(UpdateMetricValues,
            TimeSpan.FromMilliseconds(updateFrequency),
            TimeSpan.FromSeconds(_settings.GetValue("poll_delay", DefaultInitialDelay)));
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