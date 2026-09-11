using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class CloseTicketResult
{
    public Ticket? Data { get; set; }
}

public class CloseTicketRequest : IRequest<CloseTicketResult>
{
    public string? Id { get; init; }
    public string? UpdatedById { get; init; }
}

public class CloseTicketValidator : AbstractValidator<CloseTicketRequest>
{
    public CloseTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CloseTicketHandler : IRequestHandler<CloseTicketRequest, CloseTicketResult>
{
    private readonly ISender _sender;

    public CloseTicketHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<CloseTicketResult> Handle(CloseTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeTicketStatusRequest
        {
            Id = request.Id,
            Status = TicketStatus.Closed,
            UpdatedById = request.UpdatedById
        }, cancellationToken);

        return new CloseTicketResult { Data = result.Data };
    }
}
