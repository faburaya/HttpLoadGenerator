using System;
using System.Threading;

namespace HttpLoadGenerator;

internal class InterruptSignalTrap
{
    private static int s_interruptSignalsCount = 0;

    static InterruptSignalTrap()
    {
        Console.CancelKeyPress +=
            delegate (object? sender, ConsoleCancelEventArgs @event)
            {
                @event.Cancel = true;
                Interlocked.Increment(ref s_interruptSignalsCount);
            };
    }

    public static bool MustExit() => Volatile.Read(ref s_interruptSignalsCount) != 0;

    public static bool CanContinue() => !MustExit();

    public static void OnSignalInterception(Action callback)
    {
        Console.CancelKeyPress +=
            (object? sender, ConsoleCancelEventArgs @event) => callback();
    }
}
