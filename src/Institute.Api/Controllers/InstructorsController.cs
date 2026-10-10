using Institute.Application.Common;
using Institute.Application.Instructors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

[Route("api/v1/instructors")]
public sealed class InstructorsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedList<InstructorResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        OkOrProblem(await Sender.Send(new GetInstructorsQuery(search, departmentId, isActive, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<InstructorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetInstructorByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Hire(HireInstructorCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new DeactivateInstructorCommand(id), cancellationToken));
}
