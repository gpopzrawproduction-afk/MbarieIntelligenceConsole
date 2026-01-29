using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;

namespace MIC.Core.Application.Emails.Queries.GetEmailById;

/// <summary>
/// Query to get a single email by ID.
/// </summary>
public record GetEmailByIdQuery(Guid EmailId) : IQuery<EmailDto?>;
