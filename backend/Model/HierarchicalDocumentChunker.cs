using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using backend.Model;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Response
{
    public class HierarchicalDocumentChunker
    {
        private readonly ILogger<HierarchicalDocumentChunker> _logger;
        private readonly int _maxChunkSize;
        private readonly int _chunkOverlap;
        private readonly int _minChunkSize;

        public HierarchicalDocumentChunker(ILogger<HierarchicalDocumentChunker> logger,
            int maxChunkSize = 2000, int chunkOverlap = 200, int minChunkSize = 300)
        {
            _logger = logger;
            _maxChunkSize = maxChunkSize;
            _chunkOverlap = chunkOverlap;
            _minChunkSize = minChunkSize;
        }

        public List<DocumentChunk> ChunkDocument(string content, string documentId, string title)
        {
            var chunks = new List<DocumentChunk>();
            
            // Tìm file JSON chứa cấu trúc chunk được định nghĩa sẵn - ưu tiên tìm trong backend/chunk_structures
            string jsonFileName = $"chunks_structure_{documentId}.json";
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "backend", "chunk_structures", jsonFileName);
            
            // Nếu không tìm thấy trong backend/chunk_structures, thử tìm trong thư mục gốc/chunk_structures (cho tương thích ngược)
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "chunk_structures", jsonFileName);
            }
            
            // Nếu không tìm thấy, thử tìm theo tên file trực tiếp
            if (!File.Exists(jsonPath))
            {
                var fileNameOnly = Path.GetFileNameWithoutExtension(documentId);
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "backend", "chunk_structures", $"chunks_structure_{fileNameOnly}.json");
            }

            // Tiền xử lý nội dung
            content = PreprocessContent(content);
            _logger.LogInformation("Content preprocessed for document {DocumentId}, length: {Length}", documentId, content.Length);

            if (File.Exists(jsonPath))
            {
                _logger.LogInformation("Found predefined chunk structure: {JsonPath}", jsonPath);
                
                try
                {
                    // Đọc và phân tích file JSON
                    var json = File.ReadAllText(jsonPath);
                    _logger.LogDebug("Loaded JSON structure: {Length} characters", json.Length);
                    
                    // Phân tích cấu trúc từ JSON
                    dynamic? chunkStructure = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    if (chunkStructure?.chunks != null)
                    {
                        _logger.LogInformation("Applying predefined chunk structure with {Count} sections", 
                            ((Newtonsoft.Json.Linq.JArray)chunkStructure.chunks).Count);
                        
                        int chunkIndex = 0;
                        foreach (var chunk in chunkStructure.chunks)
                        {
                            try
                            {
                                var start = (int)chunk.s;
                                var end = (int)chunk.e;
                                
                                // Kiểm tra giới hạn hợp lệ
                                if (start < 0 || end > content.Length || start >= end)
                                {
                                    _logger.LogWarning("Invalid chunk bounds: s={Start}, e={End}, contentLength={Length}", 
                                        start, end, content.Length);
                                    continue;
                                }
                                
                                var chunkContent = content.Substring(start, end - start);
                                var label = (string)chunk.label;
                                
                                // Tạo chunk mới với cấu trúc đã định nghĩa
                                chunks.Add(new DocumentChunk
                                {
                                    Id = $"{documentId}_chunk_{chunkIndex + 1}",
                                    Content = chunkContent,
                                    Title = $"{title} - {label}",
                                    Embedding = new float[0],
                                    CreatedAt = DateTime.UtcNow,
                                    SourceDocumentId = documentId,
                                    ChunkIndex = chunkIndex + 1
                                });
                                
                                chunkIndex++;
                            }
                            catch (Exception ex)
                            {
                                ((ILogger)_logger).LogError(ex, "Error processing chunk: {ChunkLabel}", (string)chunk.label);
                            }
                        }
                        
                        _logger.LogInformation("Created {Count} chunks using predefined structure", chunks.Count);
                        return PostProcessChunks(chunks, ClassifyDocument(content));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing JSON chunk structure, falling back to algorithmic chunking");
                }
            }
            
            _logger.LogInformation("No valid predefined structure found, using algorithmic chunking");
            
            // Fallback: Sử dụng phương pháp chunking theo thuật toán nếu không tìm thấy cấu trúc định nghĩa sẵn
            var documentType = ClassifyDocument(content);
            _logger.LogInformation("Classified document {DocumentId} as {DocumentType}", documentId, documentType);
            
            chunks = ChunkByHierarchy(content, documentId, title, documentType);
            chunks = PostProcessChunks(chunks, documentType);
            
            return chunks;
        }

        // Giai đoạn 1: Tiền xử lý
        private string PreprocessContent(string content)
        {
            // Phương pháp 1: Xử lý lỗi OCR - loại bỏ ký tự lặp
            content = Regex.Replace(content, @"([a-zA-Z])\1{2,}", "$1"); // Loại bỏ ký tự lặp 3 lần trở lên
            content = Regex.Replace(content, @"[ ]{2,}", " "); // Nhiều khoảng trắng thành 1
            content = Regex.Replace(content, @"[\r\n]{3,}", "\n\n"); // Nhiều xuống dòng thành 2
            content = Regex.Replace(content, @"!\[\]\[[^\]]*\]", "");

            return content.Trim();
        }

        // Giai đoạn 2: Phân loại văn bản
        private DocumentType ClassifyDocument(string content)
        {
            var first70Words = string.Join(" ", content.Split(' ').Take(70));

            if (first70Words.Contains("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"))
            {
                if (first70Words.Contains("QUYẾT ĐỊNH")) return DocumentType.Decision;
                if (first70Words.Contains("THÔNG BÁO")) return DocumentType.Notification;
                if (first70Words.Contains("KẾ HOẠCH")) return DocumentType.Plan;
                if (first70Words.Contains("QUY CHẾ")) return DocumentType.Regulation;
                return DocumentType.OfficialDocument;
            }

            if (first70Words.Contains("MỤC LỤC")) return DocumentType.TableOfContents;
            if (content.Contains("|") && content.Contains("---")) return DocumentType.TableDocument;

            return DocumentType.General;
        }

        // Giai đoạn 3: Chunking theo cấu trúc phân cấp
        private List<DocumentChunk> ChunkByHierarchy(string content, string documentId, string title, DocumentType docType)
        {
            var chunks = new List<DocumentChunk>();

            // Phương pháp 3: Cắt theo dấu phân cách cứng (---)
            var majorSections = SplitByHardSeparator(content);

            foreach (var section in majorSections)
            {
                // Phương pháp 4: Trích xuất bảng Markdown
                var tablesAndContent = ExtractTables(section.Content);

                foreach (var item in tablesAndContent)
                {
                    if (item.IsTable)
                    {
                        chunks.Add(CreateChunk(item.Content, documentId, $"{title} - Bảng {chunks.Count + 1}",
                            section.SectionNumber, chunks.Count));
                    }
                    else
                    {
                        // Chunking theo cấu trúc phân cấp
                        var hierarchicalChunks = ChunkByDocumentStructure(item.Content, documentId, title, docType,
                            section.SectionNumber, chunks.Count);
                        chunks.AddRange(hierarchicalChunks);
                    }
                }
            }

            return chunks;
        }

        private List<DocumentChunk> ChunkByDocumentStructure(string content, string documentId, string title,
            DocumentType docType, int sectionNumber, int startIndex)
        {
            var chunks = new List<DocumentChunk>();

            // Xử lý theo cấu trúc phân cấp: Chương > Điều > Khoản
            var chapters = ExtractChapters(content);
            _logger.LogDebug("Found {ChapterCount} chapters", chapters.Count);

            if (chapters.Count > 0)
            {
                // Có cấu trúc chương
                foreach (var chapter in chapters)
                {
                    var chapterChunks = ProcessChapter(chapter, documentId, title, sectionNumber, startIndex + chunks.Count);
                    chunks.AddRange(chapterChunks);
                }
            }
            else
            {
                // Không có cấu trúc chương, tìm điều
                var articles = ExtractArticles(content);
                _logger.LogDebug("Found {ArticleCount} articles", articles.Count);

                if (articles.Count > 0)
                {
                    foreach (var article in articles)
                    {
                        var articleChunks = ProcessArticle(article, documentId, title, sectionNumber, startIndex + chunks.Count);
                        chunks.AddRange(articleChunks);
                    }
                }
                else
                {
                    // Không có cấu trúc điều, kiểm tra danh sách
                    var listItems = ExtractListItems(content);
                    _logger.LogDebug("Found {ListItemCount} list items", listItems.Count);

                    if (listItems.Count > 0)
                    {
                        // Chỉ xử lý như danh sách nếu danh sách chiếm phần lớn nội dung
                        var listContentLength = listItems.Sum(item => item.Length);
                        var totalContentLength = content.Length;

                        if (listContentLength > totalContentLength * 0.6) // 60% nội dung là danh sách
                        {
                            chunks.AddRange(ProcessListItems(listItems, documentId, title, sectionNumber, startIndex));
                        }
                        else
                        {
                            // Danh sách không chiếm phần lớn, xử lý theo kích thước
                            chunks.AddRange(ChunkBySize(content, documentId, title, sectionNumber, startIndex));
                        }
                    }
                    else
                    {
                        // Phương pháp 9: Fallback - chia theo kích thước
                        chunks.AddRange(ChunkBySize(content, documentId, title, sectionNumber, startIndex));
                    }
                }
            }

            return chunks;
        }

        private List<HierarchicalSection> ExtractChapters(string content)
        {
            var chapters = new List<HierarchicalSection>();
            var chapterPattern = @"(?:^|\n)\s*\*?\*?(Chương|CHƯƠNG)\s+([IVXLCDM]+|[0-9]+)\s*\*?\*?\s*\n?\s*\*?\*?([^\n]+?)(?:\*?\*?)?\s*(?=\n|$)";
            var matches = Regex.Matches(content, chapterPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            _logger.LogDebug("Chapter pattern matches: {Count}", matches.Count);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var startIndex = match.Index;
                var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;

                var chapterContent = content.Substring(startIndex, endIndex - startIndex).Trim();

                chapters.Add(new HierarchicalSection
                {
                    Number = match.Groups[2].Value,
                    Title = match.Groups[3].Value.Trim().Replace("*", ""),
                    Content = chapterContent,
                    Level = 1,
                    Type = "Chương"
                });

                _logger.LogDebug("Found chapter: {Number} - {Title}", match.Groups[2].Value, match.Groups[3].Value.Trim());
            }

            return chapters;
        }

        private List<HierarchicalSection> ExtractArticles(string content)
        {
            var articles = new List<HierarchicalSection>();
            var articlePattern = @"(?:^|\n)\s*\*?\*?(Điều|ĐIỀU)\s+([0-9]+)\s*\.\s*\*?\*?([^\n]+?)(?:\*?\*?)?\s*(?=\n|$)";
            var numberPattern = @"(?:^|\n)\s*\*?\*?([0-9]+)\\\.\s*\*?\*?([^\n]+?)(?:\*?\*?)?\s*(?=\n|$)";
            var matches = Regex.Matches(content, articlePattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            _logger.LogDebug("Article pattern matches: {Count}", matches.Count);

            if (matches.Count == 0)
            {
                matches = Regex.Matches(content, numberPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    var startIndex = match.Index;
                    var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;

                    var articleContent = content.Substring(startIndex, endIndex - startIndex).Trim();

                    articles.Add(new HierarchicalSection
                    {
                        Number = match.Groups[1].Value,
                        Title = match.Groups[2].Value.Trim().Replace("*", ""),
                        Content = articleContent,
                        Level = 2,
                        Type = "Mục" 
                    });
                }
            }
            else
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    var startIndex = match.Index;
                    var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;

                    var articleContent = content.Substring(startIndex, endIndex - startIndex).Trim();

                    articles.Add(new HierarchicalSection
                    {
                        Number = match.Groups[2].Value,
                        Title = match.Groups[3].Value.Trim().Replace("*", ""),
                        Content = articleContent,
                        Level = 2,
                        Type = "Điều"
                    });
                }
            }


            return articles;
        }

        private List<HierarchicalSection> ExtractParagraphs(string content)
        {
            var paragraphs = new List<HierarchicalSection>();
            var paragraphPattern = @"(?:^|\n)\s*([0-9]+)\s*\.\s*([^\n]+?)(?=\n|$)";
            var matches = Regex.Matches(content, paragraphPattern, RegexOptions.Multiline);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var startIndex = match.Index;
                var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;

                var paragraphContent = content.Substring(startIndex, endIndex - startIndex).Trim();

                paragraphs.Add(new HierarchicalSection
                {
                    Number = match.Groups[1].Value,
                    Title = match.Groups[2].Value.Trim(),
                    Content = paragraphContent,
                    Level = 3,
                    Type = "Khoản"
                });
            }

            return paragraphs;
        }

        private List<DocumentChunk> ProcessChapter(HierarchicalSection chapter, string documentId, string title,
            int sectionNumber, int startIndex)
        {
            var chunks = new List<DocumentChunk>();

            // Kiểm tra nếu chương quá dài
            if (chapter.Content.Length > _maxChunkSize)
            {
                // Chia nhỏ thành các điều
                var articles = ExtractArticles(chapter.Content);

                if (articles.Count > 0)
                {
                    foreach (var article in articles)
                    {
                        var articleChunks = ProcessArticle(article, documentId,
                            $"{title} - {chapter.Type} {chapter.Number}", sectionNumber, startIndex + chunks.Count);
                        chunks.AddRange(articleChunks);
                    }
                }
                else
                {
                    // Chia theo kích thước
                    chunks.AddRange(ChunkBySize(chapter.Content, documentId,
                        $"{title} - {chapter.Type} {chapter.Number}", sectionNumber, startIndex));
                }
            }
            else
            {
                // Chương không quá dài, tạo chunk trực tiếp
                chunks.Add(CreateChunk(chapter.Content, documentId,
                    $"{title} - {chapter.Type} {chapter.Number}: {chapter.Title}",
                    sectionNumber, startIndex));
            }

            return chunks;
        }

        private List<DocumentChunk> ProcessArticle(HierarchicalSection article, string documentId, string title,
            int sectionNumber, int startIndex)
        {
            var chunks = new List<DocumentChunk>();

            // Kiểm tra nếu điều quá dài
            if (article.Content.Length > _maxChunkSize)
            {
                // Chia nhỏ thành các khoản
                var paragraphs = ExtractParagraphs(article.Content);

                if (paragraphs.Count > 0)
                {
                    foreach (var paragraph in paragraphs)
                    {
                        chunks.Add(CreateChunk(paragraph.Content, documentId,
                            $"{title} - {article.Type} {article.Number} - {paragraph.Type} {paragraph.Number}",
                            sectionNumber, startIndex + chunks.Count));
                    }
                }
                else
                {
                    // Chia theo kích thước
                    chunks.AddRange(ChunkBySize(article.Content, documentId,
                        $"{title} - {article.Type} {article.Number}", sectionNumber, startIndex));
                }
            }
            else
            {
                // Điều không quá dài, tạo chunk trực tiếp
                chunks.Add(CreateChunk(article.Content, documentId,
                    $"{title} - {article.Type} {article.Number}: {article.Title}",
                    sectionNumber, startIndex));
            }

            return chunks;
        }

        private List<DocumentChunk> ProcessListItems(List<string> listItems, string documentId, string title,
            int sectionNumber, int startIndex)
        {
            var chunks = new List<DocumentChunk>();
            var currentChunk = new StringBuilder();
            var itemCount = 0;

            foreach (var item in listItems)
            {
                if (currentChunk.Length + item.Length > _maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString(), documentId,
                        $"{title} - Danh sách {chunks.Count + 1}", sectionNumber, startIndex + chunks.Count));
                    currentChunk.Clear();
                    itemCount = 0;
                }

                currentChunk.AppendLine(item);
                itemCount++;
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(currentChunk.ToString(), documentId,
                    $"{title} - Danh sách {chunks.Count + 1}", sectionNumber, startIndex + chunks.Count));
            }

            return chunks;
        }

        private List<DocumentChunk> ChunkBySize(string content, string documentId, string title,
            int sectionNumber, int startIndex)
        {
            var chunks = new List<DocumentChunk>();

            // Nếu content quá ngắn hoặc chỉ có ký tự đặc biệt, bỏ qua
            if (string.IsNullOrWhiteSpace(content) || content.Length < _minChunkSize)
            {
                // Chỉ thêm vào nếu có nội dung thực sự
                if (!string.IsNullOrWhiteSpace(content) && !IsOnlySpecialChars(content))
                {
                    chunks.Add(CreateChunk(content, documentId, $"{title} - Phần {startIndex + 1}", sectionNumber, startIndex));
                }
                return chunks;
            }

            // Chia theo đoạn văn trước
            var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                var trimmedParagraph = paragraph.Trim();
                if (string.IsNullOrWhiteSpace(trimmedParagraph))
                    continue;

                if (currentChunk.Length + trimmedParagraph.Length > _maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString(), documentId,
                        $"{title} - Phần {chunks.Count + 1}", sectionNumber, startIndex + chunks.Count));
                    currentChunk.Clear();
                }

                if (currentChunk.Length > 0)
                    currentChunk.AppendLine();
                currentChunk.Append(trimmedParagraph);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(currentChunk.ToString(), documentId,
                    $"{title} - Phần {chunks.Count + 1}", sectionNumber, startIndex + chunks.Count));
            }

            return chunks;
        }

        // Giai đoạn 4: Xử lý hậu kỳ
        private List<DocumentChunk> PostProcessChunks(List<DocumentChunk> chunks, DocumentType docType)
        {
            var processedChunks = new List<DocumentChunk>();

            foreach (var chunk in chunks)
            {
                // Phương pháp 8: Loại bỏ thông tin liên hệ học vụ
                if (IsContactInfo(chunk.Content))
                {
                    _logger.LogDebug("Removed contact info chunk: {ChunkId}", chunk.Id);
                    continue;
                }

                // Loại bỏ chunk chỉ có ký tự đặc biệt
                if (IsOnlySpecialChars(chunk.Content))
                {
                    _logger.LogDebug("Removed special chars only chunk: {ChunkId}", chunk.Id);
                    continue;
                }

                // Loại bỏ chunk chỉ có tiêu đề không có nội dung
                if (IsOnlyHeaders(chunk.Content))
                {
                    _logger.LogDebug("Removed header-only chunk: {ChunkId}", chunk.Id);
                    continue;
                }

                processedChunks.Add(chunk);
            }

            return processedChunks;
        }

        // Utility methods
        private List<MajorSection> SplitByHardSeparator(string content)
        {
            var sections = new List<MajorSection>();
            var separators = new[] { "---", "═══", "___", "-- |", "--" };

            var separatorPattern = string.Join("|", separators.Select(s => Regex.Escape(s)));
            var matches = Regex.Matches(content, separatorPattern, RegexOptions.Multiline);

            if (matches.Count == 0)
            {
                sections.Add(new MajorSection { Content = content, SectionNumber = 1 });
                return sections;
            }

            int lastIndex = 0;
            int sectionNumber = 1;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    var sectionContent = content.Substring(lastIndex, match.Index - lastIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(sectionContent))
                    {
                        sections.Add(new MajorSection
                        {
                            Content = sectionContent,
                            SectionNumber = sectionNumber++
                        });
                    }
                }
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < content.Length)
            {
                var remainingContent = content.Substring(lastIndex).Trim();
                if (!string.IsNullOrWhiteSpace(remainingContent))
                {
                    sections.Add(new MajorSection
                    {
                        Content = remainingContent,
                        SectionNumber = sectionNumber
                    });
                }
            }

            if (sections.Count == 0)
            {
                sections.Add(new MajorSection { Content = content, SectionNumber = 1 });
            }

            return sections;
        }

        private List<TableOrContent> ExtractTables(string content)
        {
            var results = new List<TableOrContent>();
            var tablePattern = @"(\|[^\n]*\|[\n\r]*\|[-:| ]+\|[\n\r]*(?:\|[^\n]*\|[\n\r]*)+)";
            var matches = Regex.Matches(content, tablePattern, RegexOptions.Multiline);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    var beforeTable = content.Substring(lastIndex, match.Index - lastIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(beforeTable))
                    {
                        results.Add(new TableOrContent { Content = beforeTable, IsTable = false });
                    }
                }

                results.Add(new TableOrContent { Content = match.Value, IsTable = true });
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < content.Length)
            {
                var remaining = content.Substring(lastIndex).Trim();
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    results.Add(new TableOrContent { Content = remaining, IsTable = false });
                }
            }

            if (results.Count == 0)
            {
                results.Add(new TableOrContent { Content = content, IsTable = false });
            }

            return results;
        }

        private List<string> ExtractListItems(string content)
        {
            var items = new List<string>();
            // Cải thiện pattern để tránh nhầm lẫn với cấu trúc chính
            var listPattern = @"(?:^|\n)\s*(?:(?![Điều|ĐIỀU|Chương|CHƯƠNG]\s+[\dIVXLCDM]+)(?![0-9]+\\\.\s)[0-9]+\.|[a-zA-Z]\.|[-*+])\s*([^\n]+)";
            var matches = Regex.Matches(content, listPattern, RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var item = match.Value.Trim();
                if (!item.Contains("Điều ") && !item.Contains("ĐIỀU ") &&
                    !item.Contains("Chương ") && !item.Contains("CHƯƠNG ") &&
                    !Regex.IsMatch(item, @"^[0-9]+\\\.\s"))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private bool IsContactInfo(string content)
        {
            var contactKeywords = new[] { "điện thoại", "email", "fax", "địa chỉ", "liên hệ", "hotline" };
            var lowerContent = content.ToLower();

            return contactKeywords.Any(keyword => lowerContent.Contains(keyword)) &&
                   (content.Contains("@") || Regex.IsMatch(content, @"\b\d{3,4}[-.\s]?\d{3,4}[-.\s]?\d{3,4}\b"));
        }

        private bool IsOnlySpecialChars(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return true;

            var trimmed = content.Trim();

            return trimmed.Length <= 5 &&
                   (trimmed.All(c => !char.IsLetterOrDigit(c)) ||
                    Regex.IsMatch(trimmed, @"^[-|:\s\d]*$"));
        }

        private bool IsOnlyHeaders(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return true;

            var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            // Nếu chỉ có 1-2 dòng và toàn bộ là tiêu đề (chữ in hoa hoặc có dấu **)
            if (lines.Count <= 2)
            {
                return lines.All(line =>
                    line.Trim().StartsWith("**") ||
                    line.Trim().ToUpper() == line.Trim() ||
                    Regex.IsMatch(line.Trim(), @"^(Chương|CHƯƠNG|Điều|ĐIỀU)\s+[\dIVXLCDM]+"));
            }

            return false;
        }

        private DocumentChunk CreateChunk(string content, string documentId, string title, int sectionNumber, int chunkIndex)
        {
            var cleanContent = content.Trim();
            var chunkId = $"{documentId}_section{sectionNumber}_chunk{chunkIndex}";

            return new DocumentChunk
            {
                Id = chunkId,
                Content = cleanContent,
                Title = title,
                SourceDocumentId = documentId,
                ChunkIndex = chunkIndex,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }

    // Supporting classes
    public enum DocumentType
    {
        General,
        OfficialDocument,
        Decision,
        Notification,
        Plan,
        Regulation,
        TableOfContents,
        TableDocument
    }

    public class HierarchicalSection
    {
        public string Number { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Level { get; set; } // 1=Chương, 2=Điều, 3=Khoản
        public string Type { get; set; } = string.Empty; // Chương, Điều, Khoản
    }

    public class MajorSection
    {
        public string Content { get; set; } = string.Empty;
        public int SectionNumber { get; set; }
    }

    public class TableOrContent
    {
        public string Content { get; set; } = string.Empty;
        public bool IsTable { get; set; }
    }
}