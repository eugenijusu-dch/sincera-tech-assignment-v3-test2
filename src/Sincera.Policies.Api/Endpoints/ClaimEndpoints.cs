using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sincera.Policies.Api.Contracts.Claims;
using Sincera.Policies.Application.Claims.Commands.FileClaim;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Api.Endpoints;

public static class ClaimEndpoints
{
    public static IEndpointRouteBuilder MapClaimEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/claims").WithTags("Claims");

        group.MapPost("/", File)
            .WithName("FileClaim")
            .Produces<FileClaimResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> File(
        [FromBody] FileClaimRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new FileClaimCommand(
                new PolicyId(request.PolicyId),
                request.IncidentDate,
                request.ClaimedAmount,
                request.Description),
            cancellationToken);
        return Results.Created($"/api/claims/{response.ClaimId}", response);
    }
}
