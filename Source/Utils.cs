using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;


namespace SystemHeat
{
  public enum LogType
  {
    UI,
    Settings,
    Modules,
    Overlay,
    Simulator,
    Any
  }

  public static class Utils
  {
    public static readonly string logTag = "SystemHeat";

    /// <summary>
    /// Is logging enabled for a given subsystem? Use this to avoid formatting
    /// calls if the logging wouldn't happen anyway.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsLogEnabled(LogType type)
    {
      return type switch
      {
        LogType.Settings => SystemHeatSettings.DebugSettings,
        LogType.UI => SystemHeatSettings.DebugUI,
        LogType.Modules => SystemHeatSettings.DebugModules,
        LogType.Overlay => SystemHeatSettings.DebugOverlay,
        LogType.Simulator => SystemHeatSettings.DebugSimulation,
        LogType.Any => true,
        _ => false
      };
    }

    /// <summary>
    /// Log a message with the mod name tag prefixed
    /// </summary>
    /// <param name="str">message string </param>
    /// <param name="logType">the subsystem that is emitting this log</param>
    public static void Log(string str, LogType logType)
    {
      if (IsLogEnabled(logType))
        Debug.Log($"[{logTag}]{str}");
    }

    public static void Log(string str) => Debug.Log($"[{logTag}]{str}");
    public static void LogWarning(string toLog) => Debug.LogWarning($"[{logTag}]{toLog}");
    public static void LogError(string toLog) => Debug.LogError($"[{logTag}]{toLog}");


    /// <summary>
    /// Return true if the Part Action Window for this part is shown, false otherwise
    /// </summary>
    public static bool IsPAWVisible(this Part part)
    {
      return part.PartActionWindow != null && part.PartActionWindow.isActiveAndEnabled;
    }


    public static string ToSI(float d, string format = null, float factor= 1000f)
    {
      if (d == 0.0)
        return d.ToString(format);

      char[] incPrefixes = new[] { 'k', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y' };
      char[] decPrefixes = new[] { 'm', '\u03bc', 'n', 'p', 'f', 'a', 'z', 'y' };

      d *= factor;

      int degree = Mathf.Clamp((int)Math.Floor(Math.Log10(Math.Abs(d)) / 3), -8, 8);
      if (degree == 0)
        return d.ToString(format);

      double scaled = d * Math.Pow(1000, -degree);

      char? prefix = null;

      switch (Math.Sign(degree))
      {
        case 1: prefix = incPrefixes[degree - 1]; break;
        case -1: prefix = decPrefixes[-degree - 1]; break;
      }

      return scaled.ToString(format) + " " + prefix;
    }

    /// <summary>
    /// Get a reference in a child of a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static T FindChildOfType<T>(string name, Transform parent)
    {
      T result = default(T);
      try
      {
        result = parent.FindDeepChild(name).GetComponent<T>();
      }
      catch (NullReferenceException)
      {
        Debug.LogError($"Couldn't find {name} in children of {parent.name}");
      }
      return result;
    }

  }

  public static class TransformDeepChildExtension
  {
    /// <summary>
    /// Find a child recursively by name
    /// </summary>
    /// <param name="aParent"></param>
    /// <param name="aName"></param>
    /// <returns></returns>
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
      Queue<Transform> queue = new Queue<Transform>();
      queue.Enqueue(aParent);
      while (queue.Count > 0)
      {
        var c = queue.Dequeue();
        if (c.name == aName)
          return c;
        foreach (Transform t in c)
          queue.Enqueue(t);
      }
      return null;
    }
  }

  internal static class ProfilingExt
  {
#if ENABLE_PROFILER
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal struct Scope(ProfilerMarker.AutoScope scope) : IDisposable
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public readonly void Dispose() => scope.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Scope ConditionalAuto(this ProfilerMarker marker) => new(marker.Auto());
#else
    internal struct Scope : IDisposable
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public readonly void Dispose() { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Scope ConditionalAuto(this ProfilerMarker _) => default;
#endif
  }
}
