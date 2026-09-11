using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketManager.Commands;

public class SetTicketTagsResult
{
    public List<string>? TagIds { get; set; }
}

/// <summary>Replaces a ticket's full tag set in one call — simpler for a multi-select UI than
/// granular add/remove endpoints, and just as easy to reason about (idempotent, no ordering
/// dependency between calls).</summary>
public class SetTicketTagsRequest : IRequest<SetTicketTagsResult>
{
    public string? TicketId { get; init; }
    public List<string>? TagIds { get; init; }
    public string? UpdatedById { get; init; }
}

public class SetTicketTagsValidator : AbstractValidator<SetTicketTagsRequest>
{
    public SetTicketTagsValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}

public class SetTicketTagsHandler : IRequestHandler<SetTicketTagsRequest, SetTicketTagsResult>
{
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly ICommandRepository<TicketTagMap> _mapRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SetTicketTagsHandler(
        ICommandRepository<Ticket> ticketRepository,
        ICommandRepository<TicketTagMap> mapRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _ticketRepository = ticketRepository;
        _mapRepository = mapRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<SetTicketTagsResult> Handle(SetTicketTagsRequest request, CancellationToken cancellationToken)
    {
        TicketAccessGuard.EnsureIsAgent(_currentUser.IsInRole(TicketAccessGuard.AgentRole));

        var ticket = await _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken);
        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.TicketId}");
        }

        var requestedTagIds = (request.TagIds ?? new List<string>()).Distinct().ToList();

        var validTagIds = await _queryContext.TicketTag
            .AsNoTracking()
            .Where(x => requestedTagIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingMaps = _mapRepository.GetQuery().Where(x => x.TicketId == request.TicketId).ToList();

        foreach (var stale in existingMaps.Where(m => !validTagIds.Contains(m.TicketTagId ?? string.Empty)))
        {
            _mapRepository.Delete(stale);
        }

        foreach (var newTagId in validTagIds.Where(id => !existingMaps.Any(m => m.TicketTagId == id)))
        {
            await _mapRepository.CreateAsync(new TicketTagMap { TicketId = request.TicketId, TicketTagId = newTagId }, cancellationToken);
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new SetTicketTagsResult { TagIds = validTagIds };
    }
}
