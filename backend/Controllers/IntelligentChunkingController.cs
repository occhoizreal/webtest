using Microsoft.AspNetCore.Mvc;
using backend.Services;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntelligentChunkingController : ControllerBase
    {
        private readonly ILogger<IntelligentChunkingController> _logger;
        private readonly IntelligentChunkingService _intelligentChunkingService;

        public IntelligentChunkingController(
            ILogger<IntelligentChunkingController> logger,
            IntelligentChunkingService intelligentChunkingService)
        {
            _logger = logger;
            _intelligentChunkingService = intelligentChunkingService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessDocument([FromBody] DocumentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing document with intelligent chunking: {DocumentId}", request.DocumentId);
                
                var chunks = await _intelligentChunkingService.ChunkDocumentIntelligentlyAsync(
                    request.Content, request.DocumentId, request.Title);

                return Ok(new
                {
                    Success = true,
                    DocumentId = request.DocumentId,
                    ChunkCount = chunks.Count,
                    ChunkingMethod = "intelligent",
                    Chunks = chunks.Select(c => new
                    {
                        c.Id,
                        c.Title,
                        ContentLength = c.Content.Length,
                        c.ChunkIndex,
                        Metadata = c.Metadata
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", request.DocumentId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("analyze/{documentId}")]
        public async Task<IActionResult> GetAnalysis(string documentId)
        {
            try
            {
                var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "backend", "intelligent_chunks", 
                    $"{documentId}_analysis_report.json");

                if (!System.IO.File.Exists(reportPath))
                {
                    return NotFound(new { Error = "Analysis report not found" });
                }

                var reportContent = await System.IO.File.ReadAllTextAsync(reportPath);
                return Ok(Newtonsoft.Json.JsonConvert.DeserializeObject(reportContent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis for {DocumentId}", documentId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class DocumentRequest
    {
        public string DocumentId { get; set; } = "";
        public string Content { get; set; } = "";
        public string Title { get; set; } = "";
    }
}