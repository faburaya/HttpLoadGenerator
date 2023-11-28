using System;

namespace HttpLoadGenerator.DTO;

internal class AccumulatedStats
{
    public int CountTotalRequests { get; set; }

    public int CountNotOkayHttpResponses { get; set; }

    public int CountNotSuccessfulResponses { get; set; }

    public TimeSpan TotalElapsedTime { get; set; }

    public double AverageRequestRatePerSec =>
        CountTotalRequests / TotalElapsedTime.TotalSeconds;

    public static AccumulatedStats operator+(AccumulatedStats a, AccumulatedStats b)
    {
         return new AccumulatedStats
         {
             CountTotalRequests =
                a.CountTotalRequests + b.CountTotalRequests,
             CountNotOkayHttpResponses =
                a.CountNotOkayHttpResponses + b.CountNotOkayHttpResponses,
             CountNotSuccessfulResponses =
                a.CountNotSuccessfulResponses + b.CountNotSuccessfulResponses,
             TotalElapsedTime =
                a.TotalElapsedTime + b.TotalElapsedTime
         };
    }
}
