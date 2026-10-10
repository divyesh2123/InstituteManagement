using Institute.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Institute.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    protected IActionResult OkOrProblem<T>(Result<T> result) => result.IsSuccess ? Ok(result.Value) : Problem(result.Error);

    protected IActionResult NoContentOrProblem(Result result) => result.IsSuccess ? NoContent() : Problem(result.Error);

    protected IActionResult CreatedOrProblem(Result<Guid> result, string getActionName) =>
        result.IsSuccess
            ? CreatedAtAction(getActionName, new { id = result.Value }, new { id = result.Value })
            : Problem(result.Error);

    /// <summary>Maps a domain/application <see cref="Error"/> to an RFC 7807 response.</summary>
    protected IActionResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error is ValidationError validation)
        {
            var modelState = new ModelStateDictionary();
            foreach (var e in validation.Errors)
            {
                modelState.AddModelError(e.Code, e.Description);
            }

            return ValidationProblem(modelState);
        }

        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext, status, title: error.Code, type: $"https://httpstatuses.io/{status}", detail: error.Description);
        problem.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status };
    }
}
