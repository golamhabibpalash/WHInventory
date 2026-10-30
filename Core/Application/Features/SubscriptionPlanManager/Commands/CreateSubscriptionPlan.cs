using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SubscriptionPlanManager.Commands;

public class CreateSubscriptionPlanResult
{
    public SubscriptionPlan? Data { get; set; }
}

public class CreateSubscriptionPlanRequest : IRequest<CreateSubscriptionPlanResult>
{
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
    public string? CreatedById { get; init; }
}

public class CreateSubscriptionPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TrialDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0).When(x => x.MaxUsers.HasValue);
        RuleFor(x => x.MaxWarehouses).GreaterThan(0).When(x => x.MaxWarehouses.HasValue);
        RuleFor(x => x.MaxProducts).GreaterThan(0).When(x => x.MaxProducts.HasValue);
    }
}

public class CreateSubscriptionPlanHandler : IRequestHandler<CreateSubscriptionPlanRequest, CreateSubscriptionPlanResult>
{
    private readonly ICommandRepository<SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public CreateSubscriptionPlanHandler(
        ICommandRepository<SubscriptionPlan> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<CreateSubscriptionPlanResult> Handle(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code!.Trim().ToUpperInvariant();

        var codeTaken = await _queryContext.SubscriptionPlan
            .AsNoTracking()
            .AnyAsync(x => x.Code == code && x.IsDeleted == false, cancellationToken);

        if (codeTaken)
        {
            throw new ValidationException($"The plan code '{code}' is already in use.");
        }

        var entity = new SubscriptionPlan
        {
            Name = request.Name!.Trim(),
            Code = code,
            Description = request.Description,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency!.Trim().ToUpperInvariant(),
            BillingInterval = request.BillingInterval,
            TrialDays = request.TrialDays,
            MaxUsers = request.MaxUsers,
            MaxWarehouses = request.MaxWarehouses,
            MaxProducts = request.MaxProducts,
            IsActive = request.IsActive,
            IsPublic = request.IsPublic,
            SortOrder = request.SortOrder,
            StripeProductId = string.IsNullOrWhiteSpace(request.StripeProductId) ? null : request.StripeProductId!.Trim(),
            StripePriceId = string.IsNullOrWhiteSpace(request.StripePriceId) ? null : request.StripePriceId!.Trim(),
            CreatedById = request.CreatedById
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateSubscriptionPlanResult { Data = entity };
    }
}
