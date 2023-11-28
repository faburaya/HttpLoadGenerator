using System.Text.Json.Serialization;

namespace HttpLoadGenerator.DTO
{
    internal class ResponsePayload
    {
        [JsonPropertyName("successful")]
        public bool Succesful { get; init; }

        [JsonConstructor]
        public ResponsePayload(bool succesful)
        {
            Succesful = succesful;
        }
    }
}
