using System;
using System.Text.Json;

using HttpLoadGenerator.DTO;

namespace HttpLoadGenerator.UnitTests;

public class JsonSerializationTests
{
    [Fact]
    public void RequestPayload_Serialization()
    {
        DateTime timestamp = new(2023, 11, 8, 21, 30, 0);
        RequestPayload payload = new("Guy", timestamp);
        string expectedJson = "{\"name\":\"Guy\",\"date\":\"8/11/2023 09:30:00 PM\",\"requests_sent\":1}";
        string actualJson = JsonSerializer.Serialize(payload);
        Assert.Equal(expectedJson, actualJson);
    }
}
