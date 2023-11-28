using System;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;

namespace HttpLoadGenerator.DTO
{
    internal class RequestPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("date")]
        public string Timestamp { get; init; }

        [JsonPropertyName("requests_sent")]
        public long SentRequestsCount { get; init; }

        private static long s_instanceCount = 0;

        internal RequestPayload(string name, DateTime timestamp)
        {
            Name = name;
            Timestamp = timestamp.ToString("d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            SentRequestsCount = Interlocked.Increment(ref s_instanceCount);
        }

        public RequestPayload(string name)
            : this(name, DateTime.UtcNow)
        {
        }
    }
}
