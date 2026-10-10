using Institute.Application.Common;
using Institute.Application.Courses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

public sealed record UpdateCourseRequest(string Title, string? Description, int DurationInWeeks);

public sealed record ChangeCourseFeeRequest(decimal FeeAmount, string Currency);

[Route("api/v1/courses")]
public sealed class CoursesController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedList<CourseResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        OkOrProblem(await Sender.Send(new GetCoursesQuery(search, departmentId, isActive, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetCourseByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateCourseCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(
            new UpdateCourseCommand(id, request.Title, request.Description, request.DurationInWeeks), cancellationToken));

    [HttpPut("{id:guid}/fee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeFee(Guid id, ChangeCourseFeeRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new ChangeCourseFeeCommand(id, request.FeeAmount, request.Currency), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new SetCourseActiveCommand(id, IsActive: true), cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new SetCourseActiveCommand(id, IsActive: false), cancellationToken));
}
