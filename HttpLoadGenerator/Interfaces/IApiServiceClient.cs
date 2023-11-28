using HttpLoadGenerator.DTO;

using System.Threading.Tasks;

namespace HttpLoadGenerator.Interfaces
{
    internal interface IApiServiceClient
    {
        Task<ApiResponse<ResponsePayload>> PostAsync(RequestPayload requestPayload);
    }
}