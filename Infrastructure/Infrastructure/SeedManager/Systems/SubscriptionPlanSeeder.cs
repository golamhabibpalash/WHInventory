using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.SeedManager.Systems;

/// <summary>
/// Seeds the platform-wide subscription plan catalogue. Plans are global (no tenant), so this runs
/// once at root scope. Idempotent per plan code: an existing plan is left untouched, so a platform
/// admin's later price/limit edits survive restarts.
/// </summary>
public class SubscriptionPlanSeeder
{
    private readonly ICommandRepository<SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionPlanSeeder(ICommandRepository<SubscriptionPlan> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var defaults = new List<SubscriptionPlan>
        {
            new SubscriptionPlan
            {
                Name = "Free", Code = "FREE", Description = "Get started at no cost.",
                Price = 0, Currency = "USD", BillingInterval = BillingInterval.Monthly, TrialDays = 0,
                MaxUsers = 2, MaxWarehouses = 1, MaxProducts = 50,
                IsActive = true, IsPublic = true, SortOrder = 1
            },
            new SubscriptionPlan
            {
                Name = "Pro", Code = "PRO", Description = "For growing teams.",
                Price = 29, Currency = "USD", BillingInterval = BillingInterval.Monthly, TrialDays = 14,
                MaxUsers = 10, MaxWarehouses = 5, MaxProducts = 2000,
                IsActive = true, IsPublic = true, SortOrder = 2
            },
            new SubscriptionPlan
            {
                Name = "Enterprise", Code = "ENTERPRISE", Description = "Unlimited scale.",
                Price = 99, Currency = "USD", BillingInterval = BillingInterval.Monthly, TrialDays = 14,
                MaxUsers = null, MaxWarehouses = null, MaxProducts = null,
                IsActive = true, IsPublic = true, SortOrder = 3
            }
        };

        var existingCodes = _repository.GetQuery()
            .Where(x => x.Code != null)
            .Select(x => x.Code!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var plan in defaults.Where(p => !existingCodes.Contains(p.Code!)))
        {
            await _repository.CreateAsync(plan);
            added = true;
        }

        if (added)
        {
            await _unitOfWork.SaveAsync();
        }
    }
}
