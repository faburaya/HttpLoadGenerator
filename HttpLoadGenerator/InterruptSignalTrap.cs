using System;
using System.Threading;

namespace HttpLoadGenerator;

internal class InterruptSignalTrap
{
    private static int s_interruptSignalsCount = 0;

    public static bool MustExit() => Volatile.Read(ref s_interruptSignalsCount) != 0;

    public static bool CanContinue() => !MustExit();

    static InterruptSignalTrap()
    {
        // trap Ctrl+C:
        Console.CancelKeyPress +=
            delegate (object? sender, ConsoleCancelEventArgs @event)
            {
                @event.Cancel = true;
                Interlocked.Increment(ref s_interruptSignalsCount);
                Console.WriteLine("\nProgram interruption signal captured: stopping...");
            };
    }
}
