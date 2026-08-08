// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Avalonia - Temporary env-gated flood profiler (RT_FLOOD_PROF=path).

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace RoyalTerminal.Avalonia.Rendering;

/// <summary>
/// Minimal always-available profiler used to locate flood throughput cost
/// centers. Enabled only when RT_FLOOD_PROF names an output file. Zero cost
/// when disabled (a single null check per sample).
/// </summary>
internal static class FloodProfiler
{
    public static readonly bool Enabled =
        Environment.GetEnvironmentVariable("RT_FLOOD_PROF") is { Length: > 0 };

    private static readonly string? s_path = Environment.GetEnvironmentVariable("RT_FLOOD_PROF");
    private static readonly ConcurrentDictionary<string, long[]> s_counters = new();
    private static long s_lastDumpTicks;

    public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0;

    public static void Stop(string name, long startTicks)
    {
        if (!Enabled)
        {
            return;
        }

        long elapsed = Stopwatch.GetTimestamp() - startTicks;
        long[] slot = s_counters.GetOrAdd(name, static _ => new long[2]);
        Interlocked.Increment(ref slot[0]);
        Interlocked.Add(ref slot[1], elapsed);
        MaybeDump();
    }

    public static void Count(string name)
    {
        if (!Enabled)
        {
            return;
        }

        long[] slot = s_counters.GetOrAdd(name, static _ => new long[2]);
        Interlocked.Increment(ref slot[0]);
        MaybeDump();
    }

    private static void MaybeDump()
    {
        long now = Stopwatch.GetTimestamp();
        long last = Interlocked.Read(ref s_lastDumpTicks);
        if (Stopwatch.GetElapsedTime(last, now).TotalMilliseconds < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref s_lastDumpTicks, now, last) != last)
        {
            return;
        }

        try
        {
            StringBuilder sb = new();
            sb.Append("--- ").Append(DateTime.UtcNow.ToString("HH:mm:ss.fff")).Append('\n');
            foreach (KeyValuePair<string, long[]> kv in s_counters)
            {
                long count = Interlocked.Read(ref kv.Value[0]);
                long ticks = Interlocked.Read(ref kv.Value[1]);
                double ms = Stopwatch.GetElapsedTime(0, ticks).TotalMilliseconds;
                sb.Append(kv.Key).Append(" count=").Append(count)
                  .Append(" total_ms=").Append(ms.ToString("F1")).Append('\n');
            }

            File.AppendAllText(s_path!, sb.ToString());
        }
        catch
        {
            // best effort
        }
    }
}
