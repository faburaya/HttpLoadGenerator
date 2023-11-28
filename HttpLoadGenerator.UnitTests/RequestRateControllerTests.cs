using System;

namespace HttpLoadGenerator.UnitTests;

public class RequestRateControllerTests
{
    private static void TestTakeAllTickets(double givenRps)
    {
        using RequestRateController requestRateController = new(givenRps);

        DateTime startTime = DateTime.Now;
        int ticketCount = 0;
        while (requestRateController.TakeTicketToSendRequest())
            ++ticketCount;

        double actualRps = requestRateController.WaitAndGetNextRps();
        double expectedRps = ticketCount / (DateTime.Now - startTime).TotalSeconds;

        double epsilon = 0.05;
        double minRatio = 1.0 - epsilon;
        double maxRatio = 1.0 + epsilon;
        Assert.InRange(actualRps / expectedRps, minRatio, maxRatio);
        Assert.InRange(expectedRps / givenRps, minRatio, maxRatio);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void TakeAllTickets_IntegerRps(int rps)
    {
        TestTakeAllTickets(rps);
    }

    [Theory]
    [InlineData(2.1)]
    [InlineData(2.2)]
    [InlineData(2.3)]
    [InlineData(2.4)]
    [InlineData(2.5)]
    [InlineData(2.6)]
    [InlineData(2.7)]
    [InlineData(2.8)]
    [InlineData(2.9)]
    public void TakeAllTickets_NonIntegerRps(double rps)
    {
        TestTakeAllTickets(rps);
    }
}
