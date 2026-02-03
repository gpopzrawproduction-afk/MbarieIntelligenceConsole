using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Application.Common.Interfaces;

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

                var entry = new KnowledgeEntry
                {
                    UserId = request.UserId,
                    Title = request.FileName,
                    Content = $"Uploaded document ({request.FileSize} bytes)",
                    SourceType = "Document",
                    Tags = new System.Collections.Generic.List<string> { "upload" },
                    CreatedAt = System.DateTime.UtcNow,
                    UpdatedAt = System.DateTime.UtcNow
                };

                await _knowledgeBaseService.CreateEntryAsync(entry);

                _logger.LogInformation("Document {FileName} uploaded successfully for user {UserId}", request.FileName, request.UserId);

                return Result.Success();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName} for user {UserId}", request.FileName, request.UserId);
                return Result.Failure($"Upload failed: {ex.Message}");
            }
        }
    }
}