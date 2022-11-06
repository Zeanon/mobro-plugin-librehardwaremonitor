using System;
using System.Collections.Generic;

namespace MoBro.Plugin.LibreHardwareMonitor.Extensions;

public static class EnumerableExtensions
{
  public static IEnumerable<T> Peek<T>(this IEnumerable<T> source, Action<T> action)
  {
    if (source == null) throw new ArgumentNullException(nameof(source));
    if (action == null) throw new ArgumentNullException(nameof(action));

    return Iterator();

    IEnumerable<T> Iterator()
    {
      foreach (var item in source)
      {
        action(item);
        yield return item;
      }
    }
  }
}