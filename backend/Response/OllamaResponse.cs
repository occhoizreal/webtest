using System.Text.Json.Serialization;

namespace backend.Response
{
    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = string.Empty;
    }
}
