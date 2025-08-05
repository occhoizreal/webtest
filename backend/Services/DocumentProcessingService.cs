using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using backend.Model;
using backend.Response;
using backend.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services
{
    /// <summary>
    /// Service xử lý tài liệu tự động khi khởi động backend với intelligent chunking
    /// </summary>
    public class DocumentProcessingService : BackgroundService
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _documentsFolderPath;

        public DocumentProcessingService(
            ILogger<DocumentProcessingService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            
            // Đường dẫn đến thư mục chứa tài liệu cần xử lý
            _documentsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
            
            // Đảm bảo các thư mục cần thiết tồn tại
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            var directories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "chunk_structures"),
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "processed_chunks"),
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "intelligent_chunks")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _logger.LogInformation("Created directory: {Directory}", dir);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Document Processing Service với Intelligent Chunking đang chạy...");

            try
            {
                // Đợi một chút để cho ứng dụng và Ollama khởi động hoàn chỉnh
                await Task.Delay(10000, stoppingToken);
                
                // Kiểm tra kết nối Ollama
                if (!await CheckOllamaConnection())
                {
                    _logger.LogWarning("Ollama không khả dụng, sử dụng hierarchical chunking làm fallback");
                }
                
                // Xử lý tất cả tài liệu trong thư mục
                await ProcessDocumentsInFolder(_documentsFolderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tài liệu");
            }
        }

        private async Task<bool> CheckOllamaConnection()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task ProcessDocumentsInFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Thư mục tài liệu không tồn tại: {FolderPath}", folderPath);
                return;
            }

            // Search for all supported file types in subdirectories
            var supportedExtensions = new[] { "*.txt", "*.md", "*.pdf" };
            var allFiles = new List<string>();
            
            foreach (var extension in supportedExtensions)
            {
                var files = Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories);
                allFiles.AddRange(files);
            }
            
            _logger.LogInformation("Tìm thấy {FileCount} tài liệu để xử lý bằng Intelligent Chunking", allFiles.Count);

            foreach (var filePath in allFiles)
            {
                await ProcessDocumentFile(filePath);
            }

            _logger.LogInformation("Hoàn tất xử lý tất cả tài liệu với Intelligent Chunking");
        }

        private async Task ProcessDocumentFile(string filePath)
        {
            try
            {
                _logger.LogInformation("Đang xử lý tài liệu với Intelligent Chunking: {FilePath}", filePath);
                
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var documentId = fileName;
                var title = fileName.Replace("-", " ");
                
                var content = await File.ReadAllTextAsync(filePath);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Ưu tiên sử dụng Intelligent Chunking
                    var intelligentChunker = scope.ServiceProvider.GetService<IntelligentChunkingService>();
                    List<DocumentChunk> chunks;
                    
                    if (intelligentChunker != null)
                    {
                        _logger.LogInformation("Sử dụng Intelligent Chunking cho tài liệu {DocumentId}", documentId);
                        chunks = await intelligentChunker.ChunkDocumentIntelligentlyAsync(content, documentId, title);
                        
                        // Lưu chunk structure được tạo bởi LLM
                        await SaveGeneratedChunkStructure(chunks, documentId);
                    }
                    else
                    {
                        _logger.LogWarning("Intelligent Chunking không khả dụng, sử dụng Hierarchical Chunking cho {DocumentId}", documentId);
                        var hierarchicalChunker = scope.ServiceProvider.GetRequiredService<HierarchicalDocumentChunker>();
                        chunks = hierarchicalChunker.ChunkDocument(content, documentId, title);
                    }
                    
                    _logger.LogInformation("Đã xử lý tài liệu {DocumentId}: tạo {ChunkCount} chunks", documentId, chunks.Count);
                    
                    // Lưu các chunk với metadata chi tiết
                    await SaveIntelligentChunks(chunks, documentId);
                    
                    // Tạo báo cáo phân tích
                    await GenerateAnalysisReport(chunks, documentId, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tài liệu: {FilePath}", filePath);
            }
        }

        private async Task SaveGeneratedChunkStructure(List<DocumentChunk> chunks, string documentId)
        {
            try
            {
                var structure = new
                {
                    documentId = documentId,
                    generatedAt = DateTime.UtcNow,
                    generationMethod = "intelligent_chunking",
                    chunks = chunks.Select((chunk, index) => new
                    {
                        s = 0, // These would need to be calculated from the actual content
                        e = chunk.Content.Length,
                        label = chunk.Title,
                        semanticTopic = chunk.Metadata.ContainsKey("semanticTopic") ? chunk.Metadata["semanticTopic"] : "",
                        importance = chunk.Metadata.ContainsKey("importance") ? chunk.Metadata["importance"] : "medium",
                        chunkType = chunk.Metadata.ContainsKey("chunkType") ? chunk.Metadata["chunkType"] : "content"
                    }).ToArray()
                };

                var structureDir = Path.Combine(Directory.GetCurrentDirectory(), "backend", "chunk_structures");
                var structurePath = Path.Combine(structureDir, $"chunks_structure_{documentId}.json");
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(structure, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(structurePath, json);
                
                _logger.LogInformation("Đã lưu cấu trúc chunk được tạo bởi LLM cho {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu cấu trúc chunk cho {DocumentId}", documentId);
            }
        }

        private async Task SaveIntelligentChunks(List<DocumentChunk> chunks, string documentId)
        {
            try
            {
                // Lưu vào thư mục intelligent_chunks
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "backend", "intelligent_chunks");
                var outputPath = Path.Combine(outputDir, $"{documentId}_intelligent_chunks.json");
                
                var enhancedChunks = chunks.Select(chunk => new
                {
                    chunk.Id,
                    chunk.Content,
                    chunk.Title,
                    chunk.SourceDocumentId,
                    chunk.ChunkIndex,
                    chunk.CreatedAt,
                    ContentLength = chunk.Content.Length,
                    Metadata = chunk.Metadata,
                    ChunkingMethod = "intelligent",
                    ProcessingTimestamp = DateTime.UtcNow
                }).ToArray();
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(enhancedChunks, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(outputPath, json);
                
                _logger.LogInformation("Đã lưu {ChunkCount} intelligent chunks cho tài liệu {DocumentId}", chunks.Count, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu intelligent chunks cho {DocumentId}", documentId);
            }
        }

        private async Task GenerateAnalysisReport(List<DocumentChunk> chunks, string documentId, string originalContent)
        {
            try
            {
                var report = new
                {
                    DocumentId = documentId,
                    AnalysisDate = DateTime.UtcNow,
                    OriginalContentLength = originalContent.Length,
                    TotalChunks = chunks.Count,
                    AverageChunkLength = chunks.Average(c => c.Content.Length),
                    ChunkingMethod = chunks.FirstOrDefault()?.Metadata?.ContainsKey("chunkingMethod") == true ? 
                        chunks.First().Metadata["chunkingMethod"] : "unknown",
                    DocumentType = chunks.FirstOrDefault()?.Metadata?.ContainsKey("documentType") == true ? 
                        chunks.First().Metadata["documentType"] : "unknown",
                    ChunkAnalysis = chunks.Select((chunk, index) => new
                    {
                        ChunkIndex = index + 1,
                        chunk.Title,
                        ContentLength = chunk.Content.Length,
                        SemanticTopic = chunk.Metadata.ContainsKey("semanticTopic") ? chunk.Metadata["semanticTopic"] : "",
                        Importance = chunk.Metadata.ContainsKey("importance") ? chunk.Metadata["importance"] : "",
                        ChunkType = chunk.Metadata.ContainsKey("chunkType") ? chunk.Metadata["chunkType"] : ""
                    }).ToArray()
                };

                var reportDir = Path.Combine(Directory.GetCurrentDirectory(), "backend", "intelligent_chunks");
                var reportPath = Path.Combine(reportDir, $"{documentId}_analysis_report.json");
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(reportPath, json);
                
                _logger.LogInformation("Đã tạo báo cáo phân tích cho tài liệu {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo báo cáo phân tích cho {DocumentId}", documentId);
            }
        }
    }
}
