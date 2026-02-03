using MediatR;
using ErrorOr;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Settings.Queries.GetSettings;

public record GetSettingsQuery : IRequest<ErrorOr<AppSettings>>
{
    public Guid UserId { get; init; }
}
