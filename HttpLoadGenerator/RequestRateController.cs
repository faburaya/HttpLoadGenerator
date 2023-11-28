using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HttpLoadGenerator.Interfaces;

namespace HttpLoadGenerator;

/// <summary>
/// Controls the rate of requests during the load test,
/// so that the request loops do not exceed the target rate.
/// </summary>
internal class RequestRateController : IDisposable, IRequestRateController
{
    private static readonly TimeSpan _minTimeQuantum = TimeSpan.FromMilliseconds(1200);

    private static void CalculateAvailableTickets(
        double targetRps,
        out int availableTicketsPerInterval,
        out TimeSpan ticketsRenewalInterval)
    {
        SortedDictionary<double, (int ticketCount, TimeSpan interval)> sortedByDeviation = new();
        
        for (int x = 1; x <= 7; ++x)
        {
            TimeSpan interval = x * _minTimeQuantum;

            int roundedTicketsCountPerInterval =
                (int)Math.Round(targetRps * interval.TotalSeconds);

            double deviation =
                Math.Abs(targetRps - roundedTicketsCountPerInterval / interval.TotalSeconds);

            sortedByDeviation[deviation] = (roundedTicketsCountPerInterval, interval);
        }

        (availableTicketsPerInterval, ticketsRenewalInterval) =
            sortedByDeviation.First().Value;
    }

    private readonly int _ticketsBudgetPerInterval;

    private readonly TimeSpan _ticketsRenewalInterval;

    private readonly Timer _timer;

    private readonly AutoResetEvent _ticketsRenewalEvent;

    private int _availableTicketsCount;

    private int _countOfTicketsLeftLastTime;

    private double _evaluatedRps;

    public RequestRateController(double targetRps)
    {
        CalculateAvailableTickets(
            targetRps,
            out _ticketsBudgetPerInterval,
            out _ticketsRenewalInterval);

        _availableTicketsCount = _ticketsBudgetPerInterval;
        _countOfTicketsLeftLastTime = _ticketsBudgetPerInterval;
        _evaluatedRps = 0.0;

        _ticketsRenewalEvent = new AutoResetEvent(false);
        _timer = new Timer(
            RenewTickets, null, _ticketsRenewalInterval, _ticketsRenewalInterval);
    }

    private void RenewTickets(object? _)
    {
        _countOfTicketsLeftLastTime =
            Math.Max(
                Interlocked.Exchange(
                    ref _availableTicketsCount, _ticketsBudgetPerInterval), 0);

        Interlocked.Exchange(ref _evaluatedRps,
            (_ticketsBudgetPerInterval - _countOfTicketsLeftLastTime)
                / _ticketsRenewalInterval.TotalSeconds);

        _ticketsRenewalEvent.Set();
    }

    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        _timer.Dispose();
        _ticketsRenewalEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public bool TakeTicketToSendRequest()
    {
        return Interlocked.Decrement(ref _availableTicketsCount) >= 0;
    }

    public double WaitAndGetNextRps()
    {
        _ticketsRenewalEvent.WaitOne();
        return Volatile.Read(ref _evaluatedRps);
    }
}
