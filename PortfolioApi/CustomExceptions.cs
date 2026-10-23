using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace PortfolioApi;

public class NotFoundException : Exception { public NotFoundException(string message) : base(message) { } }
public class ConflictException : Exception { public ConflictException(string message) : base(message) { } }
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    public ValidationException(string property, string message)
    {
        Errors = new Dictionary<string, string[]> { { property, new[] { message } } };
    }
}

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails();

        switch (exception)
        {
            case ValidationException valEx:
                var valProblem = new ValidationProblemDetails(valEx.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(valProblem, cancellationToken);
                return true;

            case NotFoundException notFoundEx:
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Not found";
                problemDetails.Detail = notFoundEx.Message;
                break;

            case ConflictException conflictEx:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = conflictEx.Message.StartsWith("Cannot sell") ? "Insufficient holdings" : "Conflict state encountered";
                problemDetails.Detail = conflictEx.Message;
                break;

            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "An unexpected error occurred.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
