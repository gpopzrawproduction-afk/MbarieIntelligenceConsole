using MediatR;
using ErrorOr;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Settings.Commands.SaveSettings;

public record SaveSettingsCommand : ICommand<bool>
{
    public Guid UserId { get; init; }
    public AppSettings Settings { get; init; } = new();
}
