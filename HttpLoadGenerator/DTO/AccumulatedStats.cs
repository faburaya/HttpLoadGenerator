namespace HttpLoadGenerator.DTO;

internal class AccumulatedStats
{
    public int CountTotalRequests { get; set; }

    public int CountNotOkayHttpResponses { get; set; }

    public int CountNotSuccessfulResponses { get; set; }

    public static AccumulatedStats operator+(AccumulatedStats a, AccumulatedStats b)
    {
         return new AccumulatedStats
         {
             CountTotalRequests =
                a.CountTotalRequests + b.CountTotalRequests,
             CountNotOkayHttpResponses =
                a.CountNotOkayHttpResponses + b.CountNotOkayHttpResponses,
             CountNotSuccessfulResponses =
                a.CountNotSuccessfulResponses + b.CountNotSuccessfulResponses
         };
    }
}
