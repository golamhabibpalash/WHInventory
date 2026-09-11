using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

/// <summary>
/// Seeds the default right-edge quick-access shortcuts (Purchase/Sales/Delivery Order, Goods
/// Receive) for a tenant. Admin can then edit, reorder, add, or remove entries from
/// Settings > Quick Shortcuts (see QuickShortcutController) — this only establishes the
/// starting set so the dock isn't empty on a fresh tenant.
/// </summary>
public class QuickShortcutSeeder
{
    private readonly ICommandRepository<QuickShortcut> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public QuickShortcutSeeder(
        ICommandRepository<QuickShortcut> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var defaults = new[]
        {
            new QuickShortcut { Name = "Purchase Order", Icon = "fas fa-file-invoice", Url = "/PurchaseOrders/PurchaseOrderList", SortOrder = 1 },
            new QuickShortcut { Name = "Sales Order", Icon = "fas fa-cash-register", Url = "/SalesOrders/SalesOrderList", SortOrder = 2 },
            new QuickShortcut { Name = "Delivery Order", Icon = "fas fa-truck", Url = "/DeliveryOrders/DeliveryOrderList", SortOrder = 3 },
            new QuickShortcut { Name = "Goods Receive", Icon = "fas fa-dolly-flatbed", Url = "/GoodsReceives/GoodsReceiveList", SortOrder = 4 },
        };

        foreach (var shortcut in defaults)
        {
            await _repository.CreateAsync(shortcut);
        }

        await _unitOfWork.SaveAsync();
    }
}
