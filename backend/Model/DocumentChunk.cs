using System;
using System.Collections.Generic;

namespace backend.Model
{
    public class DocumentChunk
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = new float[0];
        public DateTime CreatedAt { get; set; }
        public string SourceDocumentId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        
        // Enhanced metadata for intelligent chunking
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public int ContentLength => Content?.Length ?? 0;
    }
}
