using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using HttpLoadGenerator.Config;
using HttpLoadGenerator.DTO;

namespace HttpLoadGenerator.UnitTests;

public class ApiServiceClientTests
{
    [Fact]
    public async Task PostAsync()
    {
        string json = File.ReadAllText("appsettings.json");

        JsonNode jsonRoot = JsonNode.Parse(json)
            ?? throw new Exception("Cannot parse JSON configuration file!");

        JsonNode apiConfigSection = jsonRoot[ApiClientConfig.SectionName]
            ?? throw new Exception("Cannot retrieve API configuration from file!");

        ApiClientConfig config = apiConfigSection.Deserialize<ApiClientConfig>()
            ?? throw new Exception("Cannot deserialize JSON configuration!");

        using HttpClient httpClient = new();
        ApiServiceClient apiClient = new(httpClient, Options.Create(config));

        ApiResponse<ResponsePayload> response =
            await apiClient.PostAsync(new RequestPayload("Felipe Vieira Aburaya"));

        Assert.Equal(HttpStatusCode.OK, response.HttpCode);
        Assert.NotNull(response.Payload);
        Assert.True(response.Payload?.Succesful);
    }
}