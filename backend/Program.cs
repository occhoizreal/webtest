using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using backend.Response;
using backend.Model;
using backend.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Đăng ký HierarchicalDocumentChunker
builder.Services.AddSingleton<HierarchicalDocumentChunker>();

// Đăng ký IntelligentChunkingService
builder.Services.AddSingleton<IntelligentChunkingService>();

// Đăng ký DocumentProcessingService như một hosted service
builder.Services.AddHostedService<DocumentProcessingService>();

// Add CORS for frontend communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add our RAG service
builder.Services.AddSingleton<SimpleRagService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();

app.Urls.Add("https://0.0.0.0:7218");
// Add a simple root endpoint
app.MapGet("/", () => "RAG Service is running! Try /swagger for API documentation.");

// Initialize the RAG service
using (var scope = app.Services.CreateScope())
{
    var ragService = scope.ServiceProvider.GetRequiredService<SimpleRagService>();
    await ragService.InitializeAsync();
}

app.Run();

// Simple RAG implementation using direct HTTP calls to Ollama
public class SimpleRagService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SimpleRagService> _logger;
    private readonly ConcurrentDictionary<string, DocumentChunk> _documents = new();
    private readonly ConcurrentDictionary<string, List<ConversationTurn>> _conversations = new();
    private readonly HierarchicalDocumentChunker _textChunker;
    private const string OllamaBaseUrl = "http://localhost:11434";
    private const string DocumentsFolder = "Documents"; // Folder containing company documents

    public SimpleRagService(HttpClient httpClient, ILogger<SimpleRagService> logger, ILogger<HierarchicalDocumentChunker> chunkLogger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _textChunker = new HierarchicalDocumentChunker(chunkLogger, maxChunkSize: 1000, chunkOverlap: 200, minChunkSize: 0);
    }

    public async Task InitializeAsync()
    {
        // Load documents from folder
        await LoadDocumentsFromFolderAsync();

        // If no documents found, add sample ones
        if (!_documents.Any())
        {
            await AddSampleDocumentsAsync();
        }

        _logger.LogInformation("RAG service initialized with {Count} documents", _documents.Count);
    }

    private async Task LoadDocumentsFromFolderAsync()
    {
        var documentsPath = Path.Combine(Directory.GetCurrentDirectory(), DocumentsFolder);

        if (!Directory.Exists(documentsPath))
        {
            _logger.LogWarning("Documents folder not found at {Path}. Creating folder...", documentsPath);
            Directory.CreateDirectory(documentsPath);
            return;
        }

        var supportedExtensions = new[] { ".txt", ".md", ".pdf", ".docx", ".doc" };
        var files = Directory.GetFiles(documentsPath, "*.*", SearchOption.AllDirectories)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        _logger.LogInformation("Found {Count} document files to process", files.Count);

        foreach (var filePath in files)
        {
            try
            {
                var content = await ReadDocumentContentAsync(filePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var relativePath = Path.GetRelativePath(documentsPath, filePath);
                    var documentId = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    var title = Path.GetFileNameWithoutExtension(filePath);

                    //await AddDocumentAsync(documentId, content, title);
                    await AddDocumentWithChunkingAsync(documentId, content, title);
                    _logger.LogInformation("Loaded document: {Title} ({Id})", title, documentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading document: {FilePath}", filePath);
            }
        }
    }

    private async Task<bool> AddDocumentWithChunkingAsync(string documentId, string content, string? title = null)
    {
        try
        {
            //Console.WriteLine($"\n{'=' * 80}");
            //Console.WriteLine($"PROCESSING DOCUMENT: {documentId}");
            //Console.WriteLine($"TITLE: {title ?? documentId}");
            //Console.WriteLine($"ORIGINAL CONTENT LENGTH: {content.Length} characters");
            //Console.WriteLine($"{'=' * 80}");
            //Console.WriteLine("ORIGINAL CONTENT:");
            //Console.WriteLine(content);
            //Console.WriteLine($"{'=' * 80}");

            var chunks = _textChunker.ChunkDocument(content, documentId, title ?? documentId);
            _logger.LogInformation("Document {DocumentId} split into {ChunkCount} chunks", documentId, chunks.Count);

            Console.WriteLine($"\nCHUNKS CREATED ({chunks.Count} total):");
            Console.WriteLine($"{'-' * 80}");
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                Console.WriteLine($"\nCHUNK {i + 1} of {chunks.Count}:");
                Console.WriteLine($"ID: {chunk.Id}");
                Console.WriteLine($"Title: {chunk.Title}");
                //Console.WriteLine($"Content Length: {chunk.ContentLength} characters");
                //Console.WriteLine($"Source Document: {chunk.SourceDocumentId}");
                //Console.WriteLine($"Chunk Index: {chunk.ChunkIndex}");
                //Console.WriteLine($"Created At: {chunk.CreatedAt}");
                // if (chunk.Metadata.Count > 0)
                // {
                //     Console.WriteLine("Metadata:");
                //     foreach (var meta in chunk.Metadata)
                //     {
                //         Console.WriteLine($"  {meta.Key}: {meta.Value}");
                //     }
                // }
                Console.WriteLine($"{'-' * 40}");
                //Console.WriteLine("CHUNK CONTENT:");
                Console.WriteLine(chunk.Content);
                //Console.WriteLine($"{'-' * 80}");
            }
            foreach (var chunk in chunks)
            {
                var embedding = await GetEmbeddingAsync(chunk.Content);
                chunk.Embedding = embedding;
                _documents.AddOrUpdate(chunk.Id, chunk, (key, oldValue) => chunk);

                _logger.LogDebug("Added chunk {ChunkId} with {ContentLength} characters",
                    chunk.Id, chunk.ContentLength);
            }
            return true;
        }
        catch(Exception e)
        {
            _logger.LogError(e, "Error adding document {DocumentId} with chunking", documentId);
            return false;
        }
    }

    private async Task<string> ReadDocumentContentAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        switch (extension)
        {
            case ".txt":
            case ".md":
                return await File.ReadAllTextAsync(filePath);

            case ".pdf":
                return await ReadPdfContentAsync(filePath);

            case ".docx":
            case ".doc":
                return await ReadWordDocumentAsync(filePath);

            default:
                _logger.LogWarning("Unsupported file type: {Extension}", extension);
                return string.Empty;
        }
    }

    private async Task<string> ReadPdfContentAsync(string filePath)
    {
        try
        {
            // Basic PDF text extraction - we use UglyToad
            try
            {
                var text = new StringBuilder();
                using (var document = PdfDocument.Open(filePath))
                {
                    foreach (var page in document.GetPages())
                    {
                        var pageText = page.Text;
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            //this one is like printing () = whitespace
                            text.AppendLine($"--- Page {page.Number} ---");
                            text.AppendLine(pageText);
                            text.AppendLine();
                        }

                        //Add more data extraction
                        var words = page.GetWords();
                        var letters = page.Letters;
                        var images = page.GetImages();

                        _logger.LogInformation("Page {PageNum}: {WordCount} words, {LetterCount} letters, {ImageCount} images",
                            page.Number, words.Count(), letters.Count, images.Count());
                    }
                }
                var extractedContent = text.ToString().Trim();
                if (string.IsNullOrWhiteSpace(extractedContent))
                {
                    _logger.LogWarning("No text content extracted from PDF: {FilePath}", filePath);
                    return $"PDF file {Path.GetFileName(filePath)} - No readable text content found";
                }
                _logger.LogInformation("Successfully extracted {Length} characters from PDF: {FilePath}",
                    extractedContent.Length, filePath);
                return extractedContent;
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error reading PDF: {FilePath}", filePath);
                return string.Empty;
            }
            //_logger.LogWarning("PDF reading not implemented yet: {FilePath}", filePath);
            //return $"PDF Content from {Path.GetFileName(filePath)} - Content extraction not implemented";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading PDF: {FilePath}", filePath);
            return string.Empty;
        }
    }

    private async Task<string> ReadWordDocumentAsync(string filePath)
    {
        try
        {
            // Basic Word document reading - you might want to use DocumentFormat.OpenXml
            _logger.LogWarning("Word document reading not implemented yet: {FilePath}", filePath);
            return $"Word Document Content from {Path.GetFileName(filePath)} - Content extraction not implemented";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Word document: {FilePath}", filePath);
            return string.Empty;
        }
    }

    private async Task AddSampleDocumentsAsync()
    {
        var sampleDocs = new[]
        {
            new { Id = "company/about", Content = "We are a leading software development company specializing in web development, mobile apps, and AI solutions. Founded in 2020, we serve clients worldwide with cutting-edge technology solutions.", Title = "About Our Company" },
            new { Id = "services/web-dev", Content = "We specialize in web development using React, Vue.js, Angular, and modern JavaScript frameworks. Our team builds responsive, scalable web applications with modern UI/UX design principles.", Title = "Web Development Services" },
            new { Id = "services/backend", Content = "Our backend services include .NET Core, Node.js, Python FastAPI, and Go development. We create robust APIs, microservices, and database integration solutions.", Title = "Backend Development" },
            new { Id = "services/ai", Content = "We offer AI and machine learning services including chatbot development, RAG systems, computer vision, natural language processing, and custom AI model development.", Title = "AI & ML Services" },
            new { Id = "support/hours", Content = "Our support team is available Monday through Friday, 9 AM to 6 PM EST. We provide 24/7 emergency support for critical production issues. Response times vary by plan.", Title = "Support Hours" },
            new { Id = "pricing/plans", Content = "We offer three service tiers: Basic Plan ($99/month) includes up to 5 projects and email support. Professional Plan ($299/month) includes up to 15 projects and priority support. Enterprise Plan ($999/month) includes unlimited projects and dedicated support.", Title = "Pricing Plans" }
        };

        foreach (var doc in sampleDocs)
        {
            await AddDocumentAsync(doc.Id, doc.Content, doc.Title);
        }

        _logger.LogInformation("Added {Count} sample documents", sampleDocs.Length);
    }

    // Add method to reload documents from folder
    public async Task<bool> ReloadDocumentsAsync()
    {
        try
        {
            _documents.Clear();
            await LoadDocumentsFromFolderAsync();

            if (!_documents.Any())
            {
                await AddSampleDocumentsAsync();
            }

            _logger.LogInformation("Documents reloaded successfully. Total: {Count}", _documents.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading documents");
            return false;
        }
    }

    // Add method to get document list
    public List<DocumentInfo> GetDocumentList()
    {
        var documentGroups = _documents.Values
            .GroupBy(chunk => chunk.SourceDocumentId)
            .Select(group => new DocumentInfo
            {
                Id = group.Key,
                Title = group.First().Title.Replace(" - Part 1", ""), // Remove part suffix from title
                ContentPreview = group.First().Content.Length > 100
                    ? group.First().Content.Substring(0, 100) + "..."
                    : group.First().Content,
                CreatedAt = group.First().CreatedAt,
                ChunkCount = group.Count(),
                TotalContentLength = group.Sum(c => c.ContentLength)
            })
            .OrderBy(d => d.Title)
            .ToList();

        return documentGroups;
    }

    public async Task<ChatResponse> GetResponseAsync(string message, string conversationId = "default")
    {
        try
        {
            // Find relevant documents
            var relevantDocs = await FindRelevantDocumentsAsync(message, 3);
            var context = string.Join("\n\n", relevantDocs.Select(d => $"Document: {d.Title}\nContent: {d.Content}"));

            // Get conversation history
            var history = GetConversationHistory(conversationId);
            var historyText = string.Join("\n", history.TakeLast(2).Select(h => $"User: {h.UserMessage}\nAssistant: {h.BotResponse}"));

            // Build prompt
            var systemPrompt = @"You are a helpful company representative. 
                Answer questions directly and naturally using the information provided.
                Never mention documents, sources, or context in your responses.
                Respond as if you have direct knowledge about the company.
                Be professional, helpful, and conversational.";

            var userPrompt = $@"CONTEXT FROM COMPANY DOCUMENTS:
{context}

CONVERSATION HISTORY:
{historyText}

USER MESSAGE: {message}

Please provide a helpful response based on the company information provided.";

            // Get response from Ollama
            var response = await CallOllamaAsync(systemPrompt, userPrompt);

            // Save to conversation history
            SaveConversationTurn(conversationId, message, response);

            return new ChatResponse
            {
                Response = response,
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow,
                RelevantSources = relevantDocs.Select(d => d.Id).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response for message: {Message}", message);
            return new ChatResponse
            {
                Response = "I apologize, but I encountered an error processing your request. Please make sure Ollama is running and try again.",
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow,
                RelevantSources = new List<string>()
            };
        }
    }

    private async Task<string> CallOllamaAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            // First, check if Ollama is available
            var healthResponse = await _httpClient.GetAsync($"{OllamaBaseUrl}/api/tags");
            if (!healthResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama is not running or not accessible at {Url}", OllamaBaseUrl);
                return "I'm sorry, the AI service is currently unavailable. Please make sure Ollama is running with the llama3.1:8b model.";
            }

            var requestBody = new
            {
                model = "llama3.1:8b",
                prompt = $"System: {systemPrompt}\n\nUser: {userPrompt}\n\nAssistant:",
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    num_predict = 500  // Changed from max_tokens to num_predict
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to Ollama: {Request}", json);

            var response = await _httpClient.PostAsync($"{OllamaBaseUrl}/api/generate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //_logger.LogInformation("Ollama response status: {Status}", response.StatusCode);
            //_logger.LogInformation("Ollama response content: {Content}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama API call failed with status: {StatusCode}, content: {Content}", 
                    response.StatusCode, responseContent);
                return "I'm sorry, I'm having trouble connecting to the AI service. Please try again later.";
            }

            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

            if (ollamaResponse == null || string.IsNullOrEmpty(ollamaResponse.Response))
            {
                _logger.LogError("Ollama returned empty response: {Response}", responseContent);
                return "I'm sorry, I couldn't generate a response. Please try rephrasing your question.";
            }

            return ollamaResponse.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Ollama API");
            return "I'm sorry, I can't connect to the AI service. Please make sure Ollama is running on http://localhost:11434";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from Ollama response");
            return "I'm sorry, I received an invalid response from the AI service. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Ollama API");
            return "I'm sorry, I encountered an unexpected error. Please try again.";
        }
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                model = "nomic-embed-text",
                prompt = text
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{OllamaBaseUrl}/api/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding API call failed, using simple text matching");
                return new float[0]; // Return empty array to fall back to text matching
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent);

            return embeddingResponse?.Embedding ?? new float[0];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting embedding, falling back to text matching");
            return new float[0];
        }
    }

    public async Task<bool> AddDocumentAsync(string id, string content, string? title = null)
    {
        try
        {
            // Try to generate embedding, fall back to text matching if it fails
            var embedding = await GetEmbeddingAsync(content);

            var document = new DocumentChunk
            {
                Id = id,
                Content = content,
                Title = title ?? id,
                Embedding = embedding,
                CreatedAt = DateTime.UtcNow
            };

            _documents.AddOrUpdate(id, document, (key, oldValue) => document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document {Id}", id);
            return false;
        }
    }

    private async Task<List<DocumentChunk>> FindRelevantDocumentsAsync(string query, int limit = 3)
    {
        if (!_documents.Any())
            return new List<DocumentChunk>();

        try
        {
            // Try semantic search first
            var queryEmbedding = await GetEmbeddingAsync(query);

            if (queryEmbedding.Length > 0)
            {
                // Use embedding similarity
                var similarities = _documents.Values
                    .Where(chunk => chunk.Embedding.Length > 0)
                    .Select(chunk => new
                    {
                        Document = chunk,
                        Similarity = CosineSimilarity(queryEmbedding, chunk.Embedding)
                    })
                    .OrderByDescending(x => x.Similarity)
                    .Take(limit)
                    .Where(x => x.Similarity > 0.3)
                    .Select(x => x.Document)
                    .ToList();

                if (similarities.Any())
                {
                    _logger.LogInformation("Found {Count} relevant chunks using semantic search", similarities.Count);
                    return similarities;
                }
            }

            // Fall back to simple text matching
            var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var textMatches = _documents.Values
                .Select(chunk => new
                {
                    Document = chunk,
                    Score = CalculateTextSimilarity(queryWords, chunk.Content.ToLower())
                })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Where(x => x.Score > 0)
                .Select(x => x.Document)
                .ToList();

            _logger.LogInformation("Found {Count} relevant chunks using text matching", textMatches.Count);
            return textMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding relevant documents");
            return new List<DocumentChunk>();
        }
    }

    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length || vectorA.Length == 0)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static int CalculateTextSimilarity(string[] queryWords, string documentText)
    {
        return queryWords.Count(word => documentText.Contains(word));
    }

    private List<ConversationTurn> GetConversationHistory(string conversationId)
    {
        return _conversations.GetValueOrDefault(conversationId, new List<ConversationTurn>());
    }

    private void SaveConversationTurn(string conversationId, string userMessage, string botResponse)
    {
        var turn = new ConversationTurn
        {
            UserMessage = userMessage,
            BotResponse = botResponse,
            Timestamp = DateTime.UtcNow
        };

        _conversations.AddOrUpdate(conversationId,
            new List<ConversationTurn> { turn },
            (key, existing) =>
            {
                existing.Add(turn);
                return existing.TakeLast(10).ToList();
            });
    }

    public async Task<List<SearchResult>> SearchDocumentsAsync(string query, int limit = 5)
    {
        var relevantChunks = await FindRelevantDocumentsAsync(query, limit);
        return relevantChunks.Select(chunk => new SearchResult
        {
            Id = chunk.Id,
            Title = chunk.Title,
            Content = chunk.Content,
            RelevanceScore = 1.0,
            SourceDocumentId = chunk.SourceDocumentId,
            ChunkIndex = chunk.ChunkIndex
        }).ToList();
    }
}

// Response models for Ollama API
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

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

// Data models


public class DocumentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ChunkCount { get; set; }
    public int TotalContentLength { get; set; }
}

public class ConversationTurn
{
    public string UserMessage { get; set; } = string.Empty;
    public string BotResponse { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = "default";
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<string> RelevantSources { get; set; } = new();
}

public class DocumentRequest
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string SourceDocumentId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; } 
}

// Controllers
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly SimpleRagService _ragService;

    public ChatController(SimpleRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        var response = await _ragService.GetResponseAsync(request.Message, request.ConversationId);
        return Ok(response);
    }

    [HttpPost("document")]
    public async Task<ActionResult> AddDocument([FromBody] DocumentRequest request)
    {
        var success = await _ragService.AddDocumentAsync(request.Id, request.Content, request.Title);
        if (success)
            return Ok(new { message = "Document added successfully" });
        else
            return StatusCode(500, new { error = "Failed to add document" });
    }

    [HttpGet("documents")]
    public ActionResult<List<DocumentInfo>> GetDocuments()
    {
        var documents = _ragService.GetDocumentList();
        return Ok(documents);
    }

    [HttpPost("reload")]
    public async Task<ActionResult> ReloadDocuments()
    {
        var success = await _ragService.ReloadDocumentsAsync();
        if (success)
            return Ok(new { message = "Documents reloaded successfully" });
        else
            return StatusCode(500, new { error = "Failed to reload documents" });
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<SearchResult>>> SearchDocuments([FromQuery] string query, [FromQuery] int limit = 5)
    {
        var results = await _ragService.SearchDocumentsAsync(query, limit);
        return Ok(results);
    }

    [HttpGet("health")]
    public ActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}