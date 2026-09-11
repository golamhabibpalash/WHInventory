using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketPriorityManager.Commands;

public class CreateTicketPriorityResult
{
    public TicketPriority? Data { get; set; }
}

public class CreateTicketPriorityRequest : IRequest<CreateTicketPriorityResult>
{
    public string? Name { get; init; }
    public string? ColorHex { get; init; }
    public int Level { get; init; }
    public int? SlaResponseHours { get; init; }
    public int? SlaResolutionHours { get; init; }
    public bool IsActive { get; init; } = true;
    public string? CreatedById { get; init; }
}

public class CreateTicketPriorityValidator : AbstractValidator<CreateTicketPriorityRequest>
{
    public CreateTicketPriorityValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Level).GreaterThanOrEqualTo(0);
    }
}

public class CreateTicketPriorityHandler : IRequestHandler<CreateTicketPriorityRequest, CreateTicketPriorityResult>
{
    private readonly ICommandRepository<TicketPriority> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTicketPriorityHandler(
        ICommandRepository<TicketPriority> repository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _repository = repository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateTicketPriorityResult> Handle(CreateTicketPriorityRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket configuration role.");
        }

        var nameExists = await _queryContext.TicketPriority
            .AnyAsync(x => x.Name!.ToLower() == request.Name!.ToLower() && !x.IsDeleted, cancellationToken);
        if (nameExists)
            throw new Exception("Priority name already exists.");

        var entity = new TicketPriority
        {
            CreatedById = request.CreatedById,
            Name = request.Name,
            ColorHex = request.ColorHex,
            Level = request.Level,
            SlaResponseHours = request.SlaResponseHours,
            SlaResolutionHours = request.SlaResolutionHours,
            IsActive = request.IsActive
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTicketPriorityResult { Data = entity };
    }
}
