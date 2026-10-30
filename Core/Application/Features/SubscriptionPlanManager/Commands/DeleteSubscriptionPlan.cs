using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SubscriptionPlanManager.Commands;

public class DeleteSubscriptionPlanResult
{
    public SubscriptionPlan? Data { get; set; }
}

public class DeleteSubscriptionPlanRequest : IRequest<DeleteSubscriptionPlanResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteSubscriptionPlanValidator : AbstractValidator<DeleteSubscriptionPlanRequest>
{
    public DeleteSubscriptionPlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteSubscriptionPlanHandler : IRequestHandler<DeleteSubscriptionPlanRequest, DeleteSubscriptionPlanResult>
{
    private readonly ICommandRepository<SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSubscriptionPlanHandler(ICommandRepository<SubscriptionPlan> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteSubscriptionPlanResult> Handle(DeleteSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        // Soft-delete only. Existing subscriptions snapshot their plan's name and limits, so a
        // retired plan keeps working for current subscribers; prefer setting it inactive to hide it
        // from new sign-ups while leaving it assignable by a platform admin.
        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteSubscriptionPlanResult { Data = entity };
    }
}
