using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using backend.Model;
using backend.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly HierarchicalDocumentChunker _chunker;

        public DocumentController(
            ILogger<DocumentController> logger,
            HierarchicalDocumentChunker chunker)
        {
            _logger = logger;
            _chunker = chunker;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessDocument([FromForm] IFormFile file, [FromForm] string documentId = null, [FromForm] string title = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Không tìm thấy file");
                }

                // Sử dụng tên file nếu không cung cấp documentId
                if (string.IsNullOrEmpty(documentId))
                {
                    documentId = Path.GetFileNameWithoutExtension(file.FileName);
                }

                // Sử dụng tên file cho tiêu đề nếu không cung cấp
                if (string.IsNullOrEmpty(title))
                {
                    title = documentId.Replace("-", " ");
                }

                // Đọc nội dung file
                string content;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    content = await reader.ReadToEndAsync();
                }

                _logger.LogInformation("Bắt đầu xử lý tài liệu: {DocumentId}", documentId);

                // Gọi phương thức ChunkDocument
                var chunks = _chunker.ChunkDocument(content, documentId, title);

                _logger.LogInformation("Đã xử lý tài liệu {DocumentId}: tạo {ChunkCount} chunks", documentId, chunks.Count);

                // Lưu nội dung tài liệu vào thư mục data để tự động xử lý sau này
                var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                var filePath = Path.Combine(dataDirectory, $"{documentId}.txt");
                await System.IO.File.WriteAllTextAsync(filePath, content);

                // Lưu các chunk
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "processed_chunks");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var outputPath = Path.Combine(outputDir, $"{documentId}_chunks.json");
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(chunks, Newtonsoft.Json.Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(outputPath, json);

                return Ok(new
                {
                    DocumentId = documentId,
                    Title = title,
                    ChunkCount = chunks.Count,
                    Chunks = chunks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý tài liệu");
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpGet("list")]
        public IActionResult ListProcessedDocuments()
        {
            try
            {
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "processed_chunks");
                if (!Directory.Exists(outputDir))
                {
                    return Ok(new { Documents = new List<string>() });
                }

                var files = Directory.GetFiles(outputDir, "*_chunks.json");
                var documents = new List<string>();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var documentId = fileName.Replace("_chunks.json", "");
                    documents.Add(documentId);
                }

                return Ok(new { Documents = documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi liệt kê tài liệu đã xử lý");
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpGet("{documentId}/chunks")]
        public IActionResult GetDocumentChunks(string documentId)
        {
            try
            {
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "processed_chunks", $"{documentId}_chunks.json");
                if (!System.IO.File.Exists(outputPath))
                {
                    return NotFound($"Không tìm thấy tài liệu với ID: {documentId}");
                }

                var json = System.IO.File.ReadAllText(outputPath);
                var chunks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DocumentChunk>>(json);

                return Ok(new
                {
                    DocumentId = documentId,
                    ChunkCount = chunks.Count,
                    Chunks = chunks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chunks cho tài liệu {DocumentId}", documentId);
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
    }
}
