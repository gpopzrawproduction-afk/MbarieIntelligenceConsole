using FluentValidation;

namespace MIC.Core.Application.Alerts.Commands.CreateAlert;

/// <summary>
/// Validator for CreateAlertCommand
/// </summary>
public class CreateAlertCommandValidator : AbstractValidator<CreateAlertCommand>
{
    public CreateAlertCommandValidator()
    {
        RuleFor(x => x.AlertName)
            .NotEmpty().WithMessage("Alert name is required")
            .MaximumLength(200).WithMessage("Alert name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required")
            .MaximumLength(100).WithMessage("Source must not exceed 100 characters");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid alert severity");
    }
}
