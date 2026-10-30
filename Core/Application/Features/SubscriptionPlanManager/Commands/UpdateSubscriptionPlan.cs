using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SubscriptionPlanManager.Commands;

public class UpdateSubscriptionPlanResult
{
    public SubscriptionPlan? Data { get; set; }
}

public class UpdateSubscriptionPlanRequest : IRequest<UpdateSubscriptionPlanResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public double Price { get; init; }
    public string? Currency { get; init; } = "USD";
    public BillingInterval BillingInterval { get; init; } = BillingInterval.Monthly;
    public int TrialDays { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxWarehouses { get; init; }
    public int? MaxProducts { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsPublic { get; init; } = true;
    public int SortOrder { get; init; }
    public string? StripeProductId { get; init; }
    public string? StripePriceId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSubscriptionPlanValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TrialDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0).When(x => x.MaxUsers.HasValue);
        RuleFor(x => x.MaxWarehouses).GreaterThan(0).When(x => x.MaxWarehouses.HasValue);
        RuleFor(x => x.MaxProducts).GreaterThan(0).When(x => x.MaxProducts.HasValue);
    }
}

public class UpdateSubscriptionPlanHandler : IRequestHandler<UpdateSubscriptionPlanRequest, UpdateSubscriptionPlanResult>
{
    private readonly ICommandRepository<SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public UpdateSubscriptionPlanHandler(
        ICommandRepository<SubscriptionPlan> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<UpdateSubscriptionPlanResult> Handle(UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var code = request.Code!.Trim().ToUpperInvariant();

        var codeTaken = await _queryContext.SubscriptionPlan
            .AsNoTracking()
            .AnyAsync(x => x.Code == code && x.Id != entity.Id && x.IsDeleted == false, cancellationToken);

        if (codeTaken)
        {
            throw new ValidationException($"The plan code '{code}' is already in use.");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name!.Trim();
        entity.Code = code;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency!.Trim().ToUpperInvariant();
        entity.BillingInterval = request.BillingInterval;
        entity.TrialDays = request.TrialDays;
        entity.MaxUsers = request.MaxUsers;
        entity.MaxWarehouses = request.MaxWarehouses;
        entity.MaxProducts = request.MaxProducts;
        entity.IsActive = request.IsActive;
        entity.IsPublic = request.IsPublic;
        entity.SortOrder = request.SortOrder;
        entity.StripeProductId = string.IsNullOrWhiteSpace(request.StripeProductId) ? null : request.StripeProductId!.Trim();
        entity.StripePriceId = string.IsNullOrWhiteSpace(request.StripePriceId) ? null : request.StripePriceId!.Trim();

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateSubscriptionPlanResult { Data = entity };
    }
}
