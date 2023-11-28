using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using HttpLoadGenerator.DTO;
using HttpLoadGenerator.Interfaces;

namespace HttpLoadGenerator.UnitTests;

public class ApiRequestLoopTests
{
    private static TimeSpan ApiCallLatency { get; } = TimeSpan.FromMilliseconds(3);

    private static Mock<IRequestRateController> CreateRequestRateControllerMock(
        int totalCountRequests)
    {
        Mock<IRequestRateController> rpsControllerMock = new(MockBehavior.Strict);

        rpsControllerMock
            .Setup(obj => obj.WaitAndGetNextRps())
            .Callback(() => Thread.Sleep(ApiCallLatency / 2))
            .Returns(1000 / ApiCallLatency.TotalMilliseconds);

        var setupSequence =
            rpsControllerMock.SetupSequence(
                obj => obj.TakeTicketToSendRequest());

        for (int count = 0; count <= totalCountRequests; ++count)
        {
            setupSequence.Returns(count < totalCountRequests);
        }
        return rpsControllerMock;
    }

    private static Mock<IApiServiceClient> CreateApiClientMock(
        int countHttpOkay, int countHttpNotOkay, int countNotSuccessful)
    {
        Mock<IApiServiceClient> apiClientMock = new(MockBehavior.Strict);
        MockSequence callSequence = new();

        SetupClientPost(
            apiClientMock,
            callSequence,
            countHttpOkay,
            new ResponsePayload(true),
            HttpStatusCode.OK);

        SetupClientPost(
            apiClientMock,
            callSequence,
            countNotSuccessful,
            new ResponsePayload(false),
            HttpStatusCode.OK);

        SetupClientPost(
            apiClientMock,
            callSequence,
            countHttpNotOkay,
            responsePayload: null,
            HttpStatusCode.Forbidden);

        return apiClientMock;
    }

    private static void SetupClientPost(
        Mock<IApiServiceClient> apiClientMock,
        MockSequence callSequence,
        int callCount,
        ResponsePayload? responsePayload,
        HttpStatusCode httpStatus)
    {
        for (int n = 1; n <= callCount; ++n)
        {
            apiClientMock
                .InSequence(callSequence)
                .Setup(obj => obj.PostAsync(It.IsAny<RequestPayload>()))
                .Callback(() => Thread.Sleep(ApiCallLatency))
                .ReturnsAsync(
                    new ApiResponse<ResponsePayload>(responsePayload, httpStatus));
        }
    }

    [Theory]
    [InlineData(7, 0, 0)]
    [InlineData(0, 7, 0)]
    [InlineData(7, 7, 0)]
    [InlineData(0, 0, 7)]
    [InlineData(7, 0, 7)]
    [InlineData(0, 7, 7)]
    [InlineData(7, 7, 7)]
    public async Task TestApi(
        int countHttpOkay, int countHttpNotOkay, int countNotSuccessful)
    {
        int totalCountRequests = countHttpOkay + countHttpNotOkay + countNotSuccessful;

        Mock<IRequestRateController> rpsControllerMock  =
            CreateRequestRateControllerMock(totalCountRequests);

        Mock<IApiServiceClient> apiClientMock =
            CreateApiClientMock(countHttpOkay, countHttpNotOkay, countNotSuccessful);

        ApiRequestLoop requestLoop = new(apiClientMock.Object, rpsControllerMock.Object);

        DateTime timeBeforeCall = DateTime.Now;
        AccumulatedStats stats = await requestLoop.TestApi();
        TimeSpan expectedElapsedTime = DateTime.Now - timeBeforeCall;

        Assert.Equal(totalCountRequests, stats.CountTotalRequests);
        Assert.Equal(countHttpNotOkay, stats.CountNotOkayHttpResponses);
        Assert.Equal(countNotSuccessful, stats.CountNotSuccessfulResponses);

        const double epsilon = 0.05;
        const double minRatio = 1 - epsilon;
        const double maxRatio = 1 + epsilon;

        Assert.InRange(
            stats.TotalElapsedTime.TotalSeconds / expectedElapsedTime.TotalSeconds,
            minRatio, maxRatio);

        double expectedRps = totalCountRequests / expectedElapsedTime.TotalSeconds;
        Assert.InRange(
            expectedRps / stats.AverageRequestRatePerSec,
            minRatio, maxRatio);
    }
}
