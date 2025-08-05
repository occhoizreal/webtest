using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using backend.Model;
using backend.Response;

namespace backend.Services
{
    /// <summary>
    /// Intelligent chunking service that combines LLM analysis with semantic, hierarchical, and agentic chunking
    /// </summary>
    public class IntelligentChunkingService
    {
        private readonly ILogger<IntelligentChunkingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly HierarchicalDocumentChunker _hierarchicalChunker;
        private const string OllamaBaseUrl = "http://localhost:11434";

        public IntelligentChunkingService(
            ILogger<IntelligentChunkingService> logger,
            HttpClient httpClient,
            HierarchicalDocumentChunker hierarchicalChunker)
        {
            _logger = logger;
            _httpClient = httpClient;
            _hierarchicalChunker = hierarchicalChunker;
        }

        /// <summary>
        /// Performs intelligent chunking using LLM analysis combined with multiple chunking strategies
        /// </summary>
        public async Task<List<DocumentChunk>> ChunkDocumentIntelligentlyAsync(
            string content, string documentId, string title)
        {
            try
            {
                _logger.LogInformation("Starting intelligent chunking for document {DocumentId}", documentId);

                // Phase 1: LLM Document Analysis
                var documentAnalysis = await AnalyzeDocumentStructureAsync(content);
                _logger.LogInformation("Document analysis completed for {DocumentId}", documentId);

                // Phase 2: Generate Semantic Boundaries
                var semanticBoundaries = await FindSemanticBoundariesAsync(content, documentAnalysis);
                _logger.LogInformation("Found {Count} semantic boundaries", semanticBoundaries.Count);

                // Phase 3: Hierarchical Structure Detection
                var hierarchicalStructure = await DetectHierarchicalStructureAsync(content, documentAnalysis);
                _logger.LogInformation("Detected hierarchical structure with {Levels} levels", hierarchicalStructure.Levels.Count);

                // Phase 4: Agentic Chunking - Context-Aware Optimization
                var agenticChunks = await OptimizeChunksWithAgenticApproachAsync(
                    content, semanticBoundaries, hierarchicalStructure, documentAnalysis);
                _logger.LogInformation("Agentic optimization completed, created {Count} optimized chunks", agenticChunks.Count);

                // Phase 5: Create Final Chunks
                var finalChunks = await CreateFinalChunksAsync(agenticChunks, documentId, title, documentAnalysis);
                _logger.LogInformation("Created {Count} final chunks for document {DocumentId}", finalChunks.Count, documentId);

                return finalChunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in intelligent chunking for document {DocumentId}", documentId);
                
                // Fallback to hierarchical chunking
                _logger.LogWarning("Falling back to hierarchical chunking for document {DocumentId}", documentId);
                return _hierarchicalChunker.ChunkDocument(content, documentId, title);
            }
        }

        /// <summary>
        /// Phase 1: LLM Document Analysis
        /// </summary>
        private async Task<DocumentAnalysis> AnalyzeDocumentStructureAsync(string content)
        {
            var prompt = $@"
Phân tích cấu trúc của văn bản tiếng Việt sau đây và trả về JSON với thông tin:

1. Loại văn bản (quyết định, thông báo, quy chế, kế hoạch, luật, nghị định, etc.)
2. Cấu trúc phân cấp (chương, điều, khoản, mục, điểm)
3. Các phần chính (mở đầu, nội dung chính, kết luận, phụ lục)
4. Mức độ phức tạp (đơn giản, trung bình, phức tạp)
5. Các ranh giới ngữ nghĩa quan trọng
6. Chủ đề chính của từng phần

Trả về định dạng JSON:
{{
  ""documentType"": ""string"",
  ""hierarchyLevels"": [""chương"", ""điều"", ""khoản""],
  ""mainSections"": [{{""name"": ""string"", ""importance"": ""high/medium/low""}}],
  ""complexity"": ""simple/medium/complex"",
  ""semanticBoundaries"": [{{""position"": number, ""type"": ""string"", ""importance"": number}}],
  ""topics"": [{{""section"": ""string"", ""topic"": ""string"", ""keywords"": [""string""]}}]
}}

Văn bản:
{content.Substring(0, Math.Min(content.Length, 3000))}...";

            try
            {
                var response = await CallOllamaAsync(prompt);
                var analysis = JsonSerializer.Deserialize<DocumentAnalysis>(response);
                return analysis ?? new DocumentAnalysis();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze document structure, using default analysis");
                return new DocumentAnalysis
                {
                    DocumentType = "general",
                    Complexity = "medium",
                    HierarchyLevels = new List<string> { "section", "paragraph" }
                };
            }
        }

        /// <summary>
        /// Phase 2: Semantic Boundary Detection
        /// </summary>
        private async Task<List<SemanticBoundary>> FindSemanticBoundariesAsync(string content, DocumentAnalysis analysis)
        {
            var boundaries = new List<SemanticBoundary>();
            
            // Split content into manageable chunks for analysis
            var segments = SplitContentIntoSegments(content, 1000);
            
            for (int i = 0; i < segments.Count; i++)
            {
                var prompt = $@"
Xác định các ranh giới ngữ nghĩa trong đoạn văn bản tiếng Việt sau.
Tìm các điểm chuyển đổi chủ đề, thay đổi ngữ cảnh, hoặc phân đoạn logic.

Loại văn bản: {analysis.DocumentType}
Cấu trúc: {string.Join(", ", analysis.HierarchyLevels)}

Trả về JSON:
{{
  ""boundaries"": [{{
    ""position"": number,
    ""type"": ""topic_change/context_shift/section_break/logical_division"",
    ""confidence"": number,
    ""reason"": ""string""
  }}]
}}

Văn bản (segment {i + 1}):
{segments[i]}";

                try
                {
                    var response = await CallOllamaAsync(prompt);
                    var result = JsonSerializer.Deserialize<SemanticBoundaryResponse>(response);
                    
                    if (result?.Boundaries != null)
                    {
                        // Adjust positions based on segment offset
                        var segmentOffset = segments.Take(i).Sum(s => s.Length);
                        foreach (var boundary in result.Boundaries)
                        {
                            boundary.Position += segmentOffset;
                            boundaries.Add(boundary);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze semantic boundaries for segment {Segment}", i);
                }
            }

            return boundaries.OrderBy(b => b.Position).ToList();
        }

        /// <summary>
        /// Phase 3: Hierarchical Structure Detection
        /// </summary>
        private async Task<HierarchicalStructure> DetectHierarchicalStructureAsync(string content, DocumentAnalysis analysis)
        {
            var prompt = $@"
Phân tích cấu trúc phân cấp chi tiết của văn bản tiếng Việt.
Xác định các cấp độ: Chương, Điều, Khoản, Mục, Điểm và vị trí của chúng.

Loại văn bản: {analysis.DocumentType}

Trả về JSON với cấu trúc:
{{
  ""levels"": [{{
    ""level"": number,
    ""type"": ""chương/điều/khoản/mục/điểm"",
    ""elements"": [{{
      ""number"": ""string"",
      ""title"": ""string"",
      ""startPosition"": number,
      ""endPosition"": number,
      ""children"": []
    }}]
  }}]
}}

Văn bản:
{content}";

            try
            {
                var response = await CallOllamaAsync(prompt);
                var structure = JsonSerializer.Deserialize<HierarchicalStructure>(response);
                return structure ?? new HierarchicalStructure { Levels = new List<HierarchyLevel>() };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect hierarchical structure, using fallback");
                return new HierarchicalStructure { Levels = new List<HierarchyLevel>() };
            }
        }

        /// <summary>
        /// Phase 4: Agentic Chunking - Context-Aware Optimization
        /// </summary>
        private async Task<List<AgenticChunk>> OptimizeChunksWithAgenticApproachAsync(
            string content, 
            List<SemanticBoundary> semanticBoundaries, 
            HierarchicalStructure hierarchicalStructure,
            DocumentAnalysis analysis)
        {
            var prompt = $@"
Bạn là một AI agent chuyên gia về xử lý văn bản tiếng Việt.
Nhiệm vụ: Tối ưu hóa việc chia văn bản thành các chunk dựa trên:

1. Ranh giới ngữ nghĩa đã xác định
2. Cấu trúc phân cấp
3. Ngữ cảnh và tính liên kết
4. Độ dài phù hợp (1500-3000 ký tự)

Thông tin đã phân tích:
- Loại văn bản: {analysis.DocumentType}
- Số ranh giới ngữ nghĩa: {semanticBoundaries.Count}
- Cấp độ phân cấp: {hierarchicalStructure.Levels.Count}

Quy tắc tối ưu:
- Giữ nguyên tính toàn vẹn ngữ nghĩa
- Đảm bảo mỗi chunk có ngữ cảnh đầy đủ
- Tối thiểu hóa việc cắt giữa các ý tưởng liên quan
- Tối đa hóa khả năng tìm kiếm và truy xuất thông tin

Trả về JSON:
{{
  ""optimizedChunks"": [{{
    ""startPosition"": number,
    ""endPosition"": number,
    ""semanticTopic"": ""string"",
    ""importance"": ""high/medium/low"",
    ""contextDependencies"": [""string""],
    ""suggestedTitle"": ""string"",
    ""chunkType"": ""header/content/conclusion/appendix""
  }}]
}}

Ranh giới ngữ nghĩa: {JsonSerializer.Serialize(semanticBoundaries.Take(10))}
Cấu trúc phân cấp: {JsonSerializer.Serialize(hierarchicalStructure)}";

            try
            {
                var response = await CallOllamaAsync(prompt);
                var result = JsonSerializer.Deserialize<AgenticChunkResponse>(response);
                return result?.OptimizedChunks ?? new List<AgenticChunk>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed agentic optimization, creating fallback chunks");
                return CreateFallbackAgenticChunks(content, semanticBoundaries);
            }
        }

        /// <summary>
        /// Phase 5: Create Final Document Chunks
        /// </summary>
        private async Task<List<DocumentChunk>> CreateFinalChunksAsync(
            List<AgenticChunk> agenticChunks, 
            string documentId, 
            string title,
            DocumentAnalysis analysis)
        {
            var finalChunks = new List<DocumentChunk>();
            var content = ""; // This should be passed as parameter

            for (int i = 0; i < agenticChunks.Count; i++)
            {
                var agenticChunk = agenticChunks[i];
                
                // Extract content based on positions
                var chunkContent = ExtractContentByPosition(content, agenticChunk.StartPosition, agenticChunk.EndPosition);
                
                // Generate enhanced title using LLM
                var enhancedTitle = await GenerateEnhancedTitleAsync(chunkContent, agenticChunk.SuggestedTitle, analysis);
                
                var documentChunk = new DocumentChunk
                {
                    Id = $"{documentId}_intelligent_chunk_{i + 1}",
                    Content = chunkContent,
                    Title = enhancedTitle,
                    SourceDocumentId = documentId,
                    ChunkIndex = i + 1,
                    CreatedAt = DateTime.UtcNow,
                    Embedding = new float[0],
                    // Additional metadata for intelligent chunking
                    Metadata = new Dictionary<string, object>
                    {
                        ["chunkingMethod"] = "intelligent",
                        ["semanticTopic"] = agenticChunk.SemanticTopic,
                        ["importance"] = agenticChunk.Importance,
                        ["chunkType"] = agenticChunk.ChunkType,
                        ["contextDependencies"] = agenticChunk.ContextDependencies,
                        ["documentType"] = analysis.DocumentType,
                        ["processingTimestamp"] = DateTime.UtcNow
                    }
                };

                finalChunks.Add(documentChunk);
            }

            return finalChunks;
        }

        /// <summary>
        /// Enhanced title generation using LLM
        /// </summary>
        private async Task<string> GenerateEnhancedTitleAsync(string content, string suggestedTitle, DocumentAnalysis analysis)
        {
            var prompt = $@"
Tạo tiêu đề súc tích và mô tả cho đoạn văn bản tiếng Việt sau.
Tiêu đề phải:
- Phản ánh chính xác nội dung
- Dễ hiểu và tìm kiếm
- Độ dài 5-15 từ
- Phù hợp với loại văn bản: {analysis.DocumentType}

Tiêu đề gợi ý: {suggestedTitle}

Trả về JSON:
{{
  ""title"": ""string"",
  ""description"": ""string""
}}

Nội dung:
{content.Substring(0, Math.Min(content.Length, 500))}...";

            try
            {
                var response = await CallOllamaAsync(prompt);
                var result = JsonSerializer.Deserialize<TitleResponse>(response);
                return result?.Title ?? suggestedTitle;
            }
            catch
            {
                return suggestedTitle;
            }
        }

        // Helper methods
        private List<string> SplitContentIntoSegments(string content, int maxLength)
        {
            var segments = new List<string>();
            for (int i = 0; i < content.Length; i += maxLength)
            {
                var length = Math.Min(maxLength, content.Length - i);
                segments.Add(content.Substring(i, length));
            }
            return segments;
        }

        private string ExtractContentByPosition(string content, int start, int end)
        {
            start = Math.Max(0, start);
            end = Math.Min(content.Length, end);
            return content.Substring(start, end - start);
        }

        private List<AgenticChunk> CreateFallbackAgenticChunks(string content, List<SemanticBoundary> boundaries)
        {
            var chunks = new List<AgenticChunk>();
            var lastPosition = 0;

            foreach (var boundary in boundaries)
            {
                if (boundary.Position > lastPosition)
                {
                    chunks.Add(new AgenticChunk
                    {
                        StartPosition = lastPosition,
                        EndPosition = boundary.Position,
                        SemanticTopic = "Content Section",
                        Importance = "medium",
                        ChunkType = "content"
                    });
                    lastPosition = boundary.Position;
                }
            }

            // Add final chunk
            if (lastPosition < content.Length)
            {
                chunks.Add(new AgenticChunk
                {
                    StartPosition = lastPosition,
                    EndPosition = content.Length,
                    SemanticTopic = "Final Section",
                    Importance = "medium",
                    ChunkType = "content"
                });
            }

            return chunks;
        }

        private async Task<string> CallOllamaAsync(string prompt)
        {
            var requestBody = new
            {
                model = "llama3.2:3b",
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    top_p = 0.9,
                    max_tokens = 2000
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{OllamaBaseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            return responseObject.GetProperty("response").GetString() ?? "";
        }
    }

    // Supporting classes for intelligent chunking
    public class DocumentAnalysis
    {
        public string DocumentType { get; set; } = "";
        public List<string> HierarchyLevels { get; set; } = new();
        public List<MainSection> MainSections { get; set; } = new();
        public string Complexity { get; set; } = "";
        public List<SemanticBoundary> SemanticBoundaries { get; set; } = new();
        public List<TopicInfo> Topics { get; set; } = new();
    }

    public class MainSection
    {
        public string Name { get; set; } = "";
        public string Importance { get; set; } = "";
    }

    public class SemanticBoundary
    {
        public int Position { get; set; }
        public string Type { get; set; } = "";
        public double Confidence { get; set; }
        public string Reason { get; set; } = "";
    }

    public class TopicInfo
    {
        public string Section { get; set; } = "";
        public string Topic { get; set; } = "";
        public List<string> Keywords { get; set; } = new();
    }

    public class HierarchicalStructure
    {
        public List<HierarchyLevel> Levels { get; set; } = new();
    }

    public class HierarchyLevel
    {
        public int Level { get; set; }
        public string Type { get; set; } = "";
        public List<HierarchyElement> Elements { get; set; } = new();
    }

    public class HierarchyElement
    {
        public string Number { get; set; } = "";
        public string Title { get; set; } = "";
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public List<HierarchyElement> Children { get; set; } = new();
    }

    public class AgenticChunk
    {
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public string SemanticTopic { get; set; } = "";
        public string Importance { get; set; } = "";
        public List<string> ContextDependencies { get; set; } = new();
        public string SuggestedTitle { get; set; } = "";
        public string ChunkType { get; set; } = "";
    }

    // Response classes for JSON deserialización
    public class SemanticBoundaryResponse
    {
        public List<SemanticBoundary> Boundaries { get; set; } = new();
    }

    public class AgenticChunkResponse
    {
        public List<AgenticChunk> OptimizedChunks { get; set; } = new();
    }

    public class TitleResponse
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }
}