using Application.Common.Repositories;
using Application.Common.Tenancy;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Services;

/// <summary>
/// Resolves the customer for a sales document from a mobile number: reuse the tenant's existing
/// customer with that number, otherwise stage a new one. The new customer is only added to the
/// command context — the caller's own <c>SaveAsync</c> persists it together with the document,
/// so a failed save leaves no orphaned customer.
/// </summary>
public class CustomerResolutionService
{
    // Striped locks: one in-process gate per (tenant, number) hash bucket, so two concurrent
    // submissions for the same new number cannot both miss the lookup and both insert. Bounded
    // memory; unrelated numbers only rarely share a stripe. This mirrors NumberSequenceService's
    // in-process locking and assumes a single app instance (see docker-compose).
    private static readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly ITenantContext _tenantContext;

    public CustomerResolutionService(
        ICommandRepository<Customer> customerRepository,
        NumberSequenceService numberSequenceService,
        ITenantContext tenantContext
        )
    {
        _customerRepository = customerRepository;
        _numberSequenceService = numberSequenceService;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Runs <paramref name="action"/> while holding the gate for <paramref name="normalizedMobile"/>
    /// (no gate when it is null). The action should cover both <see cref="ResolveByMobileAsync"/>
    /// and the caller's <c>SaveAsync</c>, so the lookup and the insert are one critical section.
    /// </summary>
    public async Task RunWithMobileLockAsync(string? normalizedMobile, Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(normalizedMobile))
        {
            await action();
            return;
        }

        var key = $"{_tenantContext.TenantId}|{normalizedMobile}";
        var stripe = _stripes[(int)((uint)StringComparer.Ordinal.GetHashCode(key) % (uint)_stripes.Length)];

        await stripe.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            stripe.Release();
        }
    }

    public async Task<Customer?> FindByMobileAsync(string normalizedMobile, CancellationToken cancellationToken = default)
    {
        // Stored numbers passed the same validator, so every equivalent form ends with the
        // canonical local number; EndsWith narrows in SQL, TryNormalize confirms exactly.
        var candidates = await _customerRepository
            .GetQuery()
            .Where(x => x.PhoneNumber != null && x.PhoneNumber.EndsWith(normalizedMobile))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(x =>
            CustomerMobileNumber.TryNormalize(x.PhoneNumber, out var stored) && stored == normalizedMobile);
    }

    /// <summary>
    /// Returns the existing customer for the number, or a new staged (unsaved) customer whose
    /// name defaults to the number. Call inside <see cref="RunWithMobileLockAsync"/>.
    /// </summary>
    public async Task<Customer> ResolveByMobileAsync(
        string normalizedMobile,
        string? name,
        string? createdById,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindByMobileAsync(normalizedMobile, cancellationToken);
        if (existing != null)
            return existing;

        var customer = new Customer
        {
            CreatedById = createdById,
            Name = string.IsNullOrWhiteSpace(name) ? normalizedMobile : name.Trim(),
            Number = _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
            PhoneNumber = normalizedMobile
        };

        await _customerRepository.CreateAsync(customer, cancellationToken);

        return customer;
    }

    /// <summary>Rejects a customer id that is unknown, deleted or outside the current tenant.</summary>
    public async Task EnsureCustomerExistsAsync(string? customerId, CancellationToken cancellationToken = default)
    {
        var exists = await _customerRepository
            .GetQuery()
            .AnyAsync(x => x.Id == customerId, cancellationToken);

        if (!exists)
            throw new Exception("The selected customer was not found. Re-enter the customer's mobile number.");
    }
}
