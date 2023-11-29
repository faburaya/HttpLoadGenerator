using System;
using System.Net;
using System.Threading.Tasks;

using HttpLoadGenerator.DTO;
using HttpLoadGenerator.Interfaces;

namespace HttpLoadGenerator;

internal class ApiRequestLoop
{
    private readonly IApiServiceClient _client;

    private readonly IRequestRateController _requestRateController;

    public ApiRequestLoop(IApiServiceClient client, IRequestRateController requestRateController)
    {
        _client = client;
        _requestRateController = requestRateController; 
    }

    public async Task<AccumulatedStats> TestApi()
    {
        AccumulatedStats stats = new();
        for (; _requestRateController.TakeTicketToSendRequest(); ++stats.CountTotalRequests)
        {
            ApiResponse<ResponsePayload> response =
                await _client.PostAsync(
                    new RequestPayload("Felipe Vieira Aburaya"));

            if (response.HttpCode != HttpStatusCode.OK)
            {
                ++stats.CountNotOkayHttpResponses;
            }
            else if (response.Payload != null && !response.Payload.Succesful)
            {
                ++stats.CountNotSuccessfulResponses;
            }
        }
        return stats;
    }
}
