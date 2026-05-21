using MediatR;
using Microsoft.Extensions.Options;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Commands.ActivatePolicy;

public sealed class ActivatePolicyCommandHandler : IRequestHandler<ActivatePolicyCommand, ActivatePolicyResponse>
{
    private readonly IPolicyRepository _policies;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PremiumOptions _premiumOptions;

    public ActivatePolicyCommandHandler(
        IPolicyRepository policies,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PremiumOptions> premiumOptions)
    {
        _policies = policies;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _premiumOptions = premiumOptions.Value;
    }

    public async Task<ActivatePolicyResponse> Handle(ActivatePolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _policies.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new DomainException("policy.not_found", $"Policy {request.PolicyId} was not found.");

        var customer = await _customers.GetByIdAsync(policy.CustomerId, cancellationToken)
            ?? throw new DomainException("customer.not_found", $"Customer {policy.CustomerId} was not found.");

        var calculator = new PremiumCalculator(_premiumOptions);
        policy.Activate(customer, calculator, _clock);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivatePolicyResponse(
            policy.Id.Value,
            policy.EffectiveDate!.Value,
            policy.ExpiryDate!.Value,
            policy.AnnualPremium!.Value);
    }
}
