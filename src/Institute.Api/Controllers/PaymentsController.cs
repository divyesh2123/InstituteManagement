using Institute.Application.Payments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Institute.Api.Controllers;

public sealed record RefundPaymentRequest(string Reason);

[Route("api/v1/payments")]
public sealed class PaymentsController(ISender sender) : ApiController(sender)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        OkOrProblem(await Sender.Send(new GetPaymentByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Record(RecordPaymentCommand command, CancellationToken cancellationToken) =>
        CreatedOrProblem(await Sender.Send(command, cancellationToken), nameof(GetById));

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Refund(Guid id, RefundPaymentRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Sender.Send(new RefundPaymentCommand(id, request.Reason), cancellationToken));
}
