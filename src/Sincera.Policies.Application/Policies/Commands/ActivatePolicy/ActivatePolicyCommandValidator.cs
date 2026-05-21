using FluentValidation;

namespace Sincera.Policies.Application.Policies.Commands.ActivatePolicy;

public sealed class ActivatePolicyCommandValidator : AbstractValidator<ActivatePolicyCommand>
{
    public ActivatePolicyCommandValidator()
    {
        RuleFor(x => x.PolicyId.Value)
            .NotEmpty().WithMessage("PolicyId is required.");
    }
}
