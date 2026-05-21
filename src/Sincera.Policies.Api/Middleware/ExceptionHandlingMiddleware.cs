using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Sincera.Policies.Domain.Exceptions;

namespace Sincera.Policies.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteValidationProblemAsync(context, ex);
        }
        catch (InvalidPolicyTransitionException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Invalid policy transition",
                ex.Code, ex.Message);
        }
        catch (DomainException ex)
        {
            var status = ex.Code.EndsWith(".not_found", StringComparison.Ordinal)
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            await WriteProblemAsync(context, status, "Domain rule violation", ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Unhandled error",
                "internal", "An unexpected error occurred.");
        }
    }

    private static Task WriteProblemAsync(HttpContext ctx, int status, string title, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://sincera.example/errors/{code}"
        };
        problem.Extensions["code"] = code;
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        return ctx.Response.WriteAsJsonAsync(problem);
    }

    private static Task WriteValidationProblemAsync(HttpContext ctx, ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://sincera.example/errors/validation"
        };
        problem.Extensions["code"] = "validation";
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/problem+json";
        return ctx.Response.WriteAsJsonAsync(problem);
    }
}
