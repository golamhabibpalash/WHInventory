using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class ResolveTicketResult
{
    public Ticket? Data { get; set; }
}

public class ResolveTicketRequest : IRequest<ResolveTicketResult>
{
    public string? Id { get; init; }
    public string? UpdatedById { get; init; }
}

public class ResolveTicketValidator : AbstractValidator<ResolveTicketRequest>
{
    public ResolveTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

/// <summary>Thin, named shortcut over ChangeTicketStatusHandler — kept a separate command
/// (rather than making the UI call ChangeTicketStatus directly) so the API surface matches the
/// brief's explicit /resolve, /close, /reopen actions, without duplicating transition logic.</summary>
public class ResolveTicketHandler : IRequestHandler<ResolveTicketRequest, ResolveTicketResult>
{
    private readonly ISender _sender;

    public ResolveTicketHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<ResolveTicketResult> Handle(ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangeTicketStatusRequest
        {
            Id = request.Id,
            Status = TicketStatus.Resolved,
            UpdatedById = request.UpdatedById
        }, cancellationToken);

        return new ResolveTicketResult { Data = result.Data };
    }
}
