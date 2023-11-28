using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using HttpLoadGenerator.Config;
using HttpLoadGenerator.DTO;
using HttpLoadGenerator.Interfaces;

namespace HttpLoadGenerator;

internal class ApiServiceClient : IApiServiceClient
{
    private readonly HttpClient _httpClient;

    private readonly string _apiUrlPath;

    public ApiServiceClient(HttpClient httpClient, IOptions<ApiClientConfig> config)
    {
        _httpClient = httpClient;

        _httpClient.BaseAddress =
            new Uri(Validate(config.Value.EndpointBaseUrl, "API base URL"));

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient.DefaultRequestHeaders.Add(
            Validate(config.Value.AuthKeyName, "API authorization key name"),
            Validate(config.Value.AuthKeyValue, "API authorization key value"));

        _apiUrlPath = Validate(config.Value.EndpointUrlPath, "API URL path");
    }

    private static string Validate(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} has not been configured!");
        }
        return value;
    }

    public async Task<ApiResponse<ResponsePayload>> PostAsync(RequestPayload requestPayload)
    {
        using StringContent jsonContent =
            new(JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(_apiUrlPath, jsonContent);

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();

            ResponsePayload responsePayload =
                JsonSerializer.Deserialize<ResponsePayload>(jsonResponse)
                    ?? throw new Exception($"Could not deserialize JSON response: {jsonResponse}");

            return new ApiResponse<ResponsePayload>(responsePayload, response.StatusCode);
        }

        return new ApiResponse<ResponsePayload>(null, response.StatusCode);
    }
}
