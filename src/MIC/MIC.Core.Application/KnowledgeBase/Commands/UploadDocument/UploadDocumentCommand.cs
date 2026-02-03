using MediatR;
using System;

namespace MIC.Core.Application.KnowledgeBase.Commands.UploadDocument
{
    public class UploadDocumentCommand : IRequest<Result>
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public Guid UserId { get; set; }
    }

    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public static Result Success() => new Result { IsSuccess = true };
        public static Result Failure(string error) => new Result { IsSuccess = false, Error = error };
    }
}