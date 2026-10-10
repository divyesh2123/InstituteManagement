using Institute.Application.Batches;
using Institute.Application.Common;
using Institute.Domain.Batches;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

public sealed record AssignInstructorRequest(Guid InstructorId);

public sealed record EnrollStudentRequest(Guid StudentId);

[Route("api/v1/batches")]
public sealed class BatchesController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedList<BatchSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? courseId,
        [FromQuery] BatchStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        OkOrProblem(await Sender.Send(new GetBatchesQuery(courseId, status, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BatchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetBatchByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(ScheduleBatchCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new ChangeBatchStatusCommand(id, BatchStatus.Ongoing), cancellationToken));

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new ChangeBatchStatusCommand(id, BatchStatus.Completed), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new ChangeBatchStatusCommand(id, BatchStatus.Cancelled), cancellationToken));

    [HttpPut("{id:guid}/instructor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignInstructor(Guid id, AssignInstructorRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new AssignBatchInstructorCommand(id, request.InstructorId), cancellationToken));

    [HttpPost("{id:guid}/enrollments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(Guid id, EnrollStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new EnrollStudentCommand(id, request.StudentId), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id }, new { enrollmentId = result.Value })
            : Problem(result.Error);
    }

    [HttpDelete("{id:guid}/enrollments/{studentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEnrollment(Guid id, Guid studentId, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new CancelEnrollmentCommand(id, studentId), cancellationToken));
}
