using System.Text.Json.Serialization;

namespace backend.Response
{
    public class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
