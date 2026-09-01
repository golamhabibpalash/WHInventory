using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

public class CompanySeeder
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CompanySeeder(
        ICommandRepository<Company> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    /// <param name="companyName">
    /// Provisioning passes the new tenant's own name; startup seeding takes the placeholder.
    /// </param>
    public async Task GenerateDataAsync(string? companyName = null)
    {
        var entity = new Company
        {
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
            Name = string.IsNullOrWhiteSpace(companyName) ? "Acme Corp" : companyName.Trim(),
            Currency = "USD",
            Street = "123 Main St",
            City = "Metropolis",
            State = "New York",
            ZipCode = "10001",
            Country = "USA",
            PhoneNumber = "+1-212-555-1234",
            FaxNumber = "+1-212-555-5678",
            EmailAddress = "info@acmecorp.com",
            Website = "https://www.acmecorp.com"
        };

        await _repository.CreateAsync(entity);
        await _unitOfWork.SaveAsync();
    }

}
