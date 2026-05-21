using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapGet("/{id}/policies", ListPolicies)
            .WithName("ListCustomerPolicies")
            .Produces<ListCustomerPoliciesResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> ListPolicies(
        [FromRoute] string id,
        [FromQuery] PolicyStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var response = await mediator.Send(
            new ListCustomerPoliciesQuery(new CustomerId(id), status, page, pageSize),
            cancellationToken);
        return Results.Ok(response);
    }
}
