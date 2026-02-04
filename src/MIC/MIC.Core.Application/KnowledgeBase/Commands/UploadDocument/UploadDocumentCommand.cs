using MediatR;
using System;

namespace MIC.Core.Application.KnowledgeBase.Commands.UploadDocument
{
    public class UploadDocumentCommand : IRequest<Result>
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; } = string.Empty;
        public static Result Success() => new Result { IsSuccess = true };
        public static Result Failure(string error) => new Result { IsSuccess = false, Error = error };
    }
}