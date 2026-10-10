using Institute.Application.Departments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

[Route("api/v1/departments")]
public sealed class DepartmentsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetDepartmentsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetDepartmentByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateDepartmentCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));
}
