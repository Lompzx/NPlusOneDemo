using System.Diagnostics;

namespace NPlusOneDemo.Diagnostics;

public static class SimulatedNetwork
{
    /// <summary>
    /// Blocks for the given time. Spins instead of sleeping because Thread.Sleep and
    /// Task.Delay are rounded up to the ~15 ms timer resolution on Windows.
    /// </summary>
    public static void Wait(TimeSpan latency)
    {
        if (latency <= TimeSpan.Zero)
            return;

        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < latency)
            Thread.SpinWait(50);
    }
}
