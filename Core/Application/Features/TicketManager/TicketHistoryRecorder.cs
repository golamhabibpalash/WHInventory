using Application.Common.Repositories;
using Domain.Entities;

namespace Application.Features.TicketManager;

/// <summary>
/// Stages a TicketHistory row (does not save — the calling handler's own
/// _unitOfWork.SaveAsync() commits it together with whatever else that handler changed, so a
/// ticket update and its history entry are always one transaction, never two).
/// </summary>
public class TicketHistoryRecorder
{
    private readonly ICommandRepository<TicketHistory> _historyRepository;

    public TicketHistoryRecorder(ICommandRepository<TicketHistory> historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public void Record(string ticketId, string action, string? oldValue, string? newValue, string? performedById)
    {
        _historyRepository.Create(new TicketHistory
        {
            TicketId = ticketId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            PerformedById = performedById
        });
    }
}
