using Application.Common.Repositories;
using Application.Common.Services.SubscriptionManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.SubscriptionRequestManager.Commands;

public class CancelSubscriptionRequestResult
{
    public SubscriptionChangeRequest? Data { get; set; }
}

/// <summary>A tenant withdraws its own pending plan request. Exempt from read-only enforcement.</summary>
public class CancelSubscriptionRequestRequest : IRequest<CancelSubscriptionRequestResult>, ISubscriptionExempt
{
    public string? Id { get; init; }
    public string? UpdatedById { get; init; }
}

public class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequestRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CancelSubscriptionRequestHandler : IRequestHandler<CancelSubscriptionRequestRequest, CancelSubscriptionRequestResult>
{
    private readonly ICommandRepository<SubscriptionChangeRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSubscriptionRequestHandler(ICommandRepository<SubscriptionChangeRequest> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CancelSubscriptionRequestResult> Handle(CancelSubscriptionRequestRequest request, CancellationToken cancellationToken)
    {
        // The tenant filter on the repository scopes this to the caller, so a tenant can only reach
        // its own request.
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken)
            ?? throw new ValidationException("Request not found.");

        if (entity.Status != SubscriptionRequestStatus.Pending)
        {
            throw new ValidationException("Only a pending request can be cancelled.");
        }

        entity.Status = SubscriptionRequestStatus.Cancelled;
        entity.UpdatedById = request.UpdatedById;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CancelSubscriptionRequestResult { Data = entity };
    }
}
