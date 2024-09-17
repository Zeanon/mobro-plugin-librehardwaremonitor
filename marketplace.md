Integrates PC hardware metrics provided
by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

# Disclaimer

This plugin is developed and provided by ModBros and is not affiliated with LibreHardwareMonitor.  
It integrates and utilizes the [LibreHardwareMonitorLib](https://www.nuget.org/packages/LibreHardwareMonitorLib/)
provided by LibreHardwareMonitor to read the PC hardware metrics.

# Setup

No further setup required.  
LibreHardwareMonitor is already integrated in the plugin and does not need to be installed separately.

# Metrics

All metrics available in LibreHardwareMonitor are integrated and made available in MoBro.

Includes information from devices such as:

- Motherboards
- Intel and AMD processors
- NVIDIA and AMD graphics cards
- HDD, SSD and NVMe hard drives
- Network cards
- ...

# Settings

This plugin exposes the following settings:

| Setting          | Default | Explanation                                                                                                                                                         |
|------------------|---------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update frequency | 1000 ms | The frequency (in milliseconds) at which to read and update metrics from shared memory. Lower values will update metrics more frequently but may increase CPU load. |
| Processor        | enabled | Whether to monitor and include processor (CPU) metrics.                                                                                                             |
| Graphics Card    | enabled | Whether to monitor and include graphics card (GPU) metrics.                                                                                                         |
| Memory           | enabled | Whether to monitor and include memory (RAM) metrics.                                                                                                                |
| Motherboard      | enabled | Whether to monitor and include motherboard metrics.                                                                                                                 |
| Drives           | enabled | Whether to monitor and include drive (HDDs, SSDs,...) metrics.                                                                                                      |
| Controller       | enabled | Whether to monitor and include controller metrics.                                                                                                                  |
| Network          | enabled | Whether to monitor and include network (CPU) metrics.                                                                                                               |
| Power supply     | enabled | Whether to monitor and include power supply (PSU) metrics.                                                                                                          |
| Battery          | enabled | Whether to monitor and include battery metrics.                                                                                                                     |

