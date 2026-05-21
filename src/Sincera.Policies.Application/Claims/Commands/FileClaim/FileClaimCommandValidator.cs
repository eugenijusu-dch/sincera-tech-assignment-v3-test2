using FluentValidation;

namespace Sincera.Policies.Application.Claims.Commands.FileClaim;

public sealed class FileClaimCommandValidator : AbstractValidator<FileClaimCommand>
{
    public FileClaimCommandValidator()
    {
        RuleFor(x => x.PolicyId.Value).NotEmpty().WithMessage("PolicyId is required.");
        RuleFor(x => x.ClaimedAmount).GreaterThan(0).WithMessage("Claimed amount must be positive.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
    }
}
