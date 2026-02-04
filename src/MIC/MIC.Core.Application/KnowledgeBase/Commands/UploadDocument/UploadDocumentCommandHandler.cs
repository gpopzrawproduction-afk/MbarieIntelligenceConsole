using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Application.Common.Interfaces;
using System.IO;
using System.Collections.Generic;

namespace MIC.Core.Application.KnowledgeBase.Commands.UploadDocument
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Result>
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ILogger<UploadDocumentCommandHandler> _logger;

        public UploadDocumentCommandHandler(
            IKnowledgeBaseService knowledgeBaseService,
            ILogger<UploadDocumentCommandHandler> logger)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
        }

        public async Task<Result> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Uploading document {FileName} for user {UserId}", request.FileName, request.UserId);

                // Extract text content from the file
                string extractedContent = ExtractTextContent(request.Content, request.ContentType, request.FileName);

                var entry = new KnowledgeEntry
                {
                    Title = request.FileName,
                    Content = extractedContent.Length > 500 ? extractedContent.Substring(0, 500) + "..." : extractedContent,
                    FullContent = extractedContent,
                    SourceType = "Document",
                    SourceId = Guid.NewGuid(), // Document doesn't have a source ID yet
                    UserId = request.UserId,
                    Tags = new List<string> { "document", "upload", Path.GetExtension(request.FileName).TrimStart('.') },
                    FilePath = request.FileName,
                    FileSize = request.FileSize,
                    ContentType = request.ContentType
                };

                await _knowledgeBaseService.CreateEntryAsync(entry, cancellationToken);

                _logger.LogInformation("Document {FileName} uploaded successfully for user {UserId}", request.FileName, request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} for user {UserId}", request.FileName, request.UserId);
                return Result.Failure($"Upload failed: {ex.Message}");
            }
        }

        private string ExtractTextContent(byte[] content, string contentType, string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                // For text files, try to extract text
                if (extension is ".txt" or ".md" or ".csv")
                {
                    return System.Text.Encoding.UTF8.GetString(content);
                }

                // For other files, return a placeholder
                return $"Document uploaded: {fileName} ({contentType}, {content.Length} bytes)";
            }
            catch
            {
                return $"Document uploaded: {fileName} ({contentType}, {content.Length} bytes)";
            }
        }
    }
}