using FluentValidation;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Commands.UpdateAlert;

/// <summary>
/// Validator for UpdateAlertCommand.
/// </summary>
public class UpdateAlertCommandValidator : AbstractValidator<UpdateAlertCommand>
{
    public UpdateAlertCommandValidator()
    {
        RuleFor(x => x.AlertId)
            .NotEmpty()
            .WithMessage("Alert ID is required.");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("UpdatedBy is required.")
            .MaximumLength(100)
            .WithMessage("UpdatedBy cannot exceed 100 characters.");

        RuleFor(x => x.ResolutionNotes)
            .NotEmpty()
            .When(x => x.NewStatus == AlertStatus.Resolved)
            .WithMessage("Resolution notes are required when resolving an alert.")
            .MaximumLength(2000)
            .WithMessage("Resolution notes cannot exceed 2000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes cannot exceed 2000 characters.");
    }
}
