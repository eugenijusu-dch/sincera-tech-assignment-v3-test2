using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sincera.Policies.Api.Contracts.Policies;
using Sincera.Policies.Application.Policies.Commands.ActivatePolicy;
using Sincera.Policies.Application.Policies.Commands.CancelPolicy;
using Sincera.Policies.Application.Policies.Queries.GetPolicyById;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Api.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/policies")
            .WithTags("Policies");

        group.MapGet("/{id}", GetById)
            .WithName("GetPolicyById")
            .Produces<PolicyDetailsDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/activate", Activate)
            .WithName("ActivatePolicy")
            .Produces<ActivatePolicyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/cancel", Cancel)
            .WithName("CancelPolicy")
            .Produces<CancelPolicyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetById(
        [FromRoute] string id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetPolicyByIdQuery(new PolicyId(id)), cancellationToken);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> Activate(
        [FromRoute] string id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ActivatePolicyCommand(new PolicyId(id)), cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> Cancel(
        [FromRoute] string id,
        [FromBody] CancelPolicyRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new CancelPolicyCommand(new PolicyId(id), request.EffectiveCancellationDate, request.Reason),
            cancellationToken);
        return Results.Ok(response);
    }
}
