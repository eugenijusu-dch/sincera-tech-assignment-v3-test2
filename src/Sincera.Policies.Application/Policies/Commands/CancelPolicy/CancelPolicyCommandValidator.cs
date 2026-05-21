using FluentValidation;

namespace Sincera.Policies.Application.Policies.Commands.CancelPolicy;

public sealed class CancelPolicyCommandValidator : AbstractValidator<CancelPolicyCommand>
{
    public CancelPolicyCommandValidator()
    {
        RuleFor(x => x.PolicyId.Value).NotEmpty().WithMessage("PolicyId is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}
