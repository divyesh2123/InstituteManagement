using Institute.Application.Common;
using Institute.Application.Payments;
using Institute.Application.Students;
using Institute.Domain.Students;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

public sealed record UpdateStudentRequest(string FirstName, string LastName, string Email, string Phone, AddressInput Address);

public sealed record ChangeStudentStatusRequest(StudentStatus Status);

[Route("api/v1/students")]
public sealed class StudentsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedList<StudentSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] StudentStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        OkOrProblem(await Sender.Send(new GetStudentsQuery(search, status, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetStudentByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterStudentCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateStudentRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(
            new UpdateStudentCommand(id, request.FirstName, request.LastName, request.Email, request.Phone, request.Address),
            cancellationToken));

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeStudentStatusRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new ChangeStudentStatusCommand(id, request.Status), cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new DeleteStudentCommand(id), cancellationToken));

    [HttpGet("{id:guid}/payments")]
    [ProducesResponseType<IReadOnlyList<PaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetStudentPaymentsQuery(id), cancellationToken));

    [HttpGet("{id:guid}/fee-summary")]
    [ProducesResponseType<StudentFeeSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeeSummary(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetStudentFeeSummaryQuery(id), cancellationToken));
}
