using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Queries;

public record GetTicketAttachmentDto
{
    public string? Id { get; init; }
    public string? OriginalName { get; init; }
    public string? Extension { get; init; }
    public long? FileSize { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTicketAttachmentsResult
{
    public List<GetTicketAttachmentDto>? Data { get; init; }
}

public class GetTicketAttachmentsRequest : IRequest<GetTicketAttachmentsResult>
{
    public string? TicketId { get; init; }
}

public class GetTicketAttachmentsValidator : AbstractValidator<GetTicketAttachmentsRequest>
{
    public GetTicketAttachmentsValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}

public class GetTicketAttachmentsHandler : IRequestHandler<GetTicketAttachmentsRequest, GetTicketAttachmentsResult>
{
    private readonly IQueryContext _context;
    private readonly ICommandRepository<Domain.Entities.Ticket> _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketAttachmentsHandler(
        IQueryContext context,
        ICommandRepository<Domain.Entities.Ticket> ticketRepository,
        ICurrentUserService currentUser
        )
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<GetTicketAttachmentsResult> Handle(GetTicketAttachmentsRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken);
        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.TicketId}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);

        var entities = await _context.FileDocument
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == "Ticket" && x.ModuleId == request.TicketId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new GetTicketAttachmentDto
            {
                Id = x.Id,
                OriginalName = x.OriginalName,
                Extension = x.Extension,
                FileSize = x.FileSize,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new GetTicketAttachmentsResult { Data = entities };
    }
}
