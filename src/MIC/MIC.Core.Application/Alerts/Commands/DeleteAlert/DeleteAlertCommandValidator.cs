using FluentValidation;

namespace MIC.Core.Application.Alerts.Commands.DeleteAlert;

/// <summary>
/// Validator for DeleteAlertCommand.
/// </summary>
public class DeleteAlertCommandValidator : AbstractValidator<DeleteAlertCommand>
{
    public DeleteAlertCommandValidator()
    {
        RuleFor(x => x.AlertId)
            .NotEmpty()
            .WithMessage("Alert ID is required.");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy is required.")
            .MaximumLength(100)
            .WithMessage("DeletedBy cannot exceed 100 characters.");
    }
}
