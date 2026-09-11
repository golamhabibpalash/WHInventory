using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class ReopenTicketResult
{
    public Ticket? Data { get; set; }
}

public class ReopenTicketRequest : IRequest<ReopenTicketResult>
{
    public string? Id { get; init; }
    public string? UpdatedById { get; init; }
}

public class ReopenTicketValidator : AbstractValidator<ReopenTicketRequest>
{
    public ReopenTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReopenTicketHandler : IRequestHandler<ReopenTicketRequest, ReopenTicketResult>
{
    private readonly ISender _sender;

    public ReopenTicketHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<ReopenTicketResult> Handle(ReopenTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeTicketStatusRequest
        {
            Id = request.Id,
            Status = TicketStatus.Reopened,
            UpdatedById = request.UpdatedById
        }, cancellationToken);

        return new ReopenTicketResult { Data = result.Data };
    }
}
