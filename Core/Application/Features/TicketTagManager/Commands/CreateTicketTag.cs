using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Features.TicketManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TicketTagManager.Commands;

public class CreateTicketTagResult
{
    public TicketTag? Data { get; set; }
}

public class CreateTicketTagRequest : IRequest<CreateTicketTagResult>
{
    public string? Name { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateTicketTagValidator : AbstractValidator<CreateTicketTagRequest>
{
    public CreateTicketTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateTicketTagHandler : IRequestHandler<CreateTicketTagRequest, CreateTicketTagResult>
{
    private readonly ICommandRepository<TicketTag> _repository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTicketTagHandler(
        ICommandRepository<TicketTag> repository,
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

    public async Task<CreateTicketTagResult> Handle(CreateTicketTagRequest request, CancellationToken cancellationToken = default)
    {
        // Any agent may create a tag while working a ticket (matches section 15's "reusable
        // tags" intent) — only category/priority definitions are restricted to the config role.
        if (!_currentUser.IsInRole(TicketAccessGuard.AgentRole) && !_currentUser.IsInRole(TicketAccessGuard.ConfigRole))
        {
            throw new Exception("This action requires the ticket agent role.");
        }

        var nameExists = await _queryContext.TicketTag
            .AnyAsync(x => x.Name!.ToLower() == request.Name!.ToLower() && !x.IsDeleted, cancellationToken);
        if (nameExists)
            throw new Exception("Tag already exists.");

        var entity = new TicketTag { CreatedById = request.CreatedById, Name = request.Name };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTicketTagResult { Data = entity };
    }
}
