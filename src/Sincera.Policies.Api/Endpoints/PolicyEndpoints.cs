using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sincera.Policies.Application.Policies.Commands.ActivatePolicy;
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
}
