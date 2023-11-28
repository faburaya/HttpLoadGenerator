namespace HttpLoadGenerator.Config
{
    internal class ApiClientConfig
    {
        public static string SectionName => "ApiClient";

        public string? EndpointBaseUrl { get; set; }

        public string? EndpointUrlPath { get; set; }

        public string? AuthKeyName { get; set; }

        public string? AuthKeyValue { get; set; }
    }
}
