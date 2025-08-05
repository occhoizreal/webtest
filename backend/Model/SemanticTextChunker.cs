using System.Text;
using System.Text.RegularExpressions;

namespace backend.Model
{
    public class SemanticTextChunker
    {
        private readonly ILogger<SemanticTextChunker> _logger;
        private readonly int _maxChunkSize;
        private readonly int _chunkOverlap;
        private readonly int _minChunkSize;

        public SemanticTextChunker(ILogger<SemanticTextChunker> logger, int maxChunkSize = 1000, int chunkOverlap = 200, int minChunkSize = 100)
        {
            _logger = logger;
            _maxChunkSize = maxChunkSize;
            _chunkOverlap = chunkOverlap;
            _minChunkSize = minChunkSize;
        }

        public List<DocumentChunk> ChunkDocument(string content, string documentId, string title) 
        {
            var chunks = new List<DocumentChunk>();
            try
            {
                var semanticChunk = ChunkBySemanticsections(content);
                var finalChunks = new List<string>();
                foreach(var chunk in semanticChunk)
                {
                    if(chunk.Length <= _maxChunkSize)
                    {
                        finalChunks.Add(chunk);
                    }
                    else
                    {
                        var subChunks = SplitLargeChunk(chunk);
                        finalChunks.AddRange(subChunks);
                    }
                }
                for(int i = 0; i < finalChunks.Count; i++)
                {
                    var chunkContent = finalChunks[i].Trim();
                    if(chunkContent.Length >= _minChunkSize)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Id = $"{documentId}_chunk_{i + 1}",
                            Content = chunkContent,
                            Title = $"{title} - Part {i+1}",
                            Embedding = [],
                            CreatedAt = DateTime.UtcNow,
                            SourceDocumentId = documentId,
                            ChunkIndex = i + 1
                        });
                    }
                }
                _logger.LogInformation("Document {DocumentId} chunked into {ChunkCount} semantic chunks", documentId, chunks.Count);
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error chunking document {DocumentId}", documentId);
                // Fallback: create a single chunk with the entire content
                chunks.Add(new DocumentChunk
                {
                    Id = $"{documentId}_chunk_1",
                    Content = content.Trim(),
                    Title = title,
                    Embedding = new float[0],
                    CreatedAt = DateTime.UtcNow,
                    SourceDocumentId = documentId,
                    ChunkIndex = 1
                });
            }
            return chunks;
        }

        private List<string> ChunkBySemanticsections(string content)
        {
            var chunks = new List<string>();

            //Split by markdown headers (for .md files)
            var headerChunks = SplitByHeaders(content);
            if(headerChunks.Count > 1)
            {
                return headerChunks;
            }

            // Strategy 2: Split by double line breaks (paragraph separation)
            var paragraphChunks = SplitByParagraphs(content);
            if(paragraphChunks.Count > 1)
            {
                return paragraphChunks;
            }

            // Strategy 3: Split by sentences while maintaining context
            var sentencesChunks = SplitBySentences(content);  
            return sentencesChunks;
        }

        private List<string> SplitByHeaders(string content)
        {
            var chunks = new List<string>();

            var headerPattern = @"^(#{1,6})\s+(.+)$";
            var lines = content.Split('\n');

            var currentChunk = new StringBuilder();
            var currentHeader = "";

            foreach(var line in lines)
            {
                var match = Regex.Match(line, headerPattern, RegexOptions.Multiline);

                if (match.Success)
                {
                    //Found a header (maybe)
                    if(currentChunk.Length > 0)
                    {
                        var chunkContent = currentChunk.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(chunkContent))
                        {
                            chunks.Add(chunkContent);
                        }
                        currentChunk.Clear();
                    }
                    currentHeader = line;
                    currentChunk.AppendLine(line);
                }
                else
                {
                    currentChunk.AppendLine(line);
                }
            }
            if(currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(chunkContent);
                }
            }
            return chunks.Count > 1 ? chunks : new List<string> { content };
        }

        private List<string> SplitByParagraphs(string content)
        {
            var chunks = new List<string>();

            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if(paragraphs.Length <= 0)
            {
                return new List<string> { content };
            }

            var currentChunk = new StringBuilder();
            foreach(var paragraph in paragraphs)
            {
                var trimmedParagraph = paragraph.Trim();
                if (string.IsNullOrWhiteSpace(trimmedParagraph))
                {
                    continue;
                }
                if(currentChunk.Length + trimmedParagraph.Length + 4 > _maxChunkSize && currentChunk.Length > 0)
                {
                    var chunkContent = currentChunk.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(chunkContent))
                    {
                        chunks.Add(chunkContent);
                    }
                    currentChunk.Clear();

                    if(chunks.Count > 0)
                    {
                        var overlapText = GetOverlapText(chunks.Last(), _chunkOverlap);
                        if (!string.IsNullOrWhiteSpace(overlapText))
                        {
                            currentChunk.AppendLine(overlapText);
                            currentChunk.AppendLine("---");
                        }
                    }
                }

                currentChunk.AppendLine(trimmedParagraph);
                currentChunk.AppendLine();
            }

            if(currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(chunkContent);
                }
            }

            return chunks.Count > 0 ? chunks : [content];
        }

        private List<string> SplitBySentences(string content)
        {
            var chunks = new List<string>();

            // Split by sentence boundaries
            var sentences = SplitIntoSentences(content);

            if (sentences.Count <= 1)
            {
                return new List<string> { content };
            }

            var currentChunk = new StringBuilder();

            for (int i = 0; i < sentences.Count; i++)
            {
                var sentence = sentences[i].Trim();
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;

                // Check if adding this sentence would exceed max chunk size
                if (currentChunk.Length + sentence.Length + 2 > _maxChunkSize && currentChunk.Length > 0)
                {
                    // Save current chunk and start new one
                    var chunkContent = currentChunk.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(chunkContent))
                    {
                        chunks.Add(chunkContent);
                    }

                    currentChunk.Clear();

                    // Add overlap from previous sentences
                    if (chunks.Count > 0)
                    {
                        var overlapStart = Math.Max(0, i - 2); // Include 2 previous sentences for context
                        var overlapSentences = sentences.Skip(overlapStart).Take(i - overlapStart).ToList();
                        var overlapText = string.Join(" ", overlapSentences).Trim();

                        if (!string.IsNullOrWhiteSpace(overlapText) && overlapText.Length <= _chunkOverlap)
                        {
                            currentChunk.AppendLine(overlapText);
                            currentChunk.AppendLine("---"); // Separator to indicate overlap
                        }
                    }
                }

                currentChunk.Append(sentence);

                // Add proper spacing between sentences
                if (i < sentences.Count - 1)
                {
                    currentChunk.Append(" ");
                }
            }

            // Add the last chunk
            if (currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(chunkContent);
                }
            }

            return chunks.Count > 0 ? chunks : [content];
        }

        private List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();

            // Enhanced sentence splitting pattern
            var sentencePattern = @"(?<=[.!?])\s+(?=[A-Z])";
            var potentialSentences = Regex.Split(text, sentencePattern);

            foreach (var sentence in potentialSentences)
            {
                var trimmed = sentence.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sentences.Add(trimmed);
                }
            }

            return sentences;
        }

        private List<string> SplitLargeChunk(string chunk)
        {
            var subChunks = new List<string>();

            // If chunk is still too large, split it more aggressively
            if (chunk.Length <= _maxChunkSize)
            {
                return new List<string> { chunk };
            }

            // Try paragraph splitting first
            var paragraphs = chunk.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentSubChunk = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                if (currentSubChunk.Length + paragraph.Length + 4 > _maxChunkSize && currentSubChunk.Length > 0)
                {
                    // Save current sub-chunk
                    var subChunkContent = currentSubChunk.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(subChunkContent))
                    {
                        subChunks.Add(subChunkContent);
                    }

                    currentSubChunk.Clear();

                    // Add overlap
                    if (subChunks.Count > 0)
                    {
                        var overlapText = GetOverlapText(subChunks.Last(), _chunkOverlap);
                        if (!string.IsNullOrWhiteSpace(overlapText))
                        {
                            currentSubChunk.AppendLine(overlapText);
                            currentSubChunk.AppendLine("---");
                        }
                    }
                }

                currentSubChunk.AppendLine(paragraph);
                currentSubChunk.AppendLine();
            }

            // Add the last sub-chunk
            if (currentSubChunk.Length > 0)
            {
                var subChunkContent = currentSubChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(subChunkContent))
                {
                    subChunks.Add(subChunkContent);
                }
            }

            return subChunks.Count > 0 ? subChunks : [chunk];
        }

        private string GetOverlapText(string text, int maxOverLapLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxOverLapLength){
                return text;
            }
            var overlapCandidate = text[Math.Max(0, text.Length - maxOverLapLength)..];
            var lastSentenceEnd = overlapCandidate.LastIndexOfAny(['.', '!', '?']);
            if (lastSentenceEnd > 0)
            {
                return overlapCandidate[(lastSentenceEnd + 1)..].Trim();
            }

            // Look for paragraph boundaries
            var lastParagraphBreak = overlapCandidate.LastIndexOf("\n\n");
            if (lastParagraphBreak > 0)
            {
                return overlapCandidate[(lastParagraphBreak + 2)..].Trim();
            }

            var words = overlapCandidate.Split(' ');
            if(words.Length > 1)
            {
                return string.Join(" ", words.Skip(1));
            }

            return overlapCandidate;
        }
    }
}
