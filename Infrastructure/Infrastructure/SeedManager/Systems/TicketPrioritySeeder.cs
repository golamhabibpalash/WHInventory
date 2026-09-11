using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

/// <summary>Default ticket priorities with the brief's own example SLA hours (section 17) — the
/// starting point for a fresh tenant's SLA foundation, editable from
/// Settings &gt; Ticketing &gt; Priorities.</summary>
public class TicketPrioritySeeder
{
    private readonly ICommandRepository<TicketPriority> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TicketPrioritySeeder(ICommandRepository<TicketPriority> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var defaults = new[]
        {
            new TicketPriority { Name = "Critical", Level = 0, ColorHex = "#dc3545", SlaResponseHours = 1, SlaResolutionHours = 4, IsActive = true },
            new TicketPriority { Name = "High", Level = 1, ColorHex = "#fd7e14", SlaResponseHours = 4, SlaResolutionHours = 24, IsActive = true },
            new TicketPriority { Name = "Medium", Level = 2, ColorHex = "#1b84ff", SlaResponseHours = 24, SlaResolutionHours = 72, IsActive = true },
            new TicketPriority { Name = "Low", Level = 3, ColorHex = "#6c757d", SlaResponseHours = 48, SlaResolutionHours = 168, IsActive = true },
        };

        foreach (var priority in defaults)
        {
            await _repository.CreateAsync(priority);
        }

        await _unitOfWork.SaveAsync();
    }
}
