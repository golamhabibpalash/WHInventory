using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

/// <summary>Default ticket categories for a fresh tenant — the brief's own example list
/// (section 10). Admin can rename/disable/reorder/add from Settings &gt; Ticketing &gt; Categories.</summary>
public class TicketCategorySeeder
{
    private readonly ICommandRepository<TicketCategory> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TicketCategorySeeder(ICommandRepository<TicketCategory> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var names = new[]
        {
            "Inventory", "Stock", "Product", "Warehouse", "Purchase", "Sales",
            "User Account", "Authentication", "Report", "Payment", "Integration",
            "Technical Issue", "Bug", "Feature Request", "Other"
        };

        var sortOrder = 1;
        foreach (var name in names)
        {
            await _repository.CreateAsync(new TicketCategory { Name = name, SortOrder = sortOrder++, IsActive = true });
        }

        await _unitOfWork.SaveAsync();
    }
}
