using System.Net;

namespace HttpLoadGenerator.DTO
{
    internal record ApiResponse<T>(T? Payload, HttpStatusCode HttpCode) where T : class;
}
