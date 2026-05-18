using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesOrderManager;

public class SalesOrderService
{
    private readonly ICommandRepository<SalesOrder> _salesOrderRepository;
    private readonly ICommandRepository<SalesOrderItem> _salesOrderItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SalesOrderService(
        ICommandRepository<SalesOrder> salesOrderRepository,
        ICommandRepository<SalesOrderItem> salesOrderItemRepository,
        IUnitOfWork unitOfWork
        )
    {
        _salesOrderRepository = salesOrderRepository;
        _salesOrderItemRepository = salesOrderItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Recalculate(string salesOrderId)
    {
        var salesOrder = await _salesOrderRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.Id == salesOrderId)
            .Include(x => x.Tax)
            .SingleOrDefaultAsync();

        if (salesOrder == null)
            return;

        var salesOrderItems = await _salesOrderItemRepository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.SalesOrderId == salesOrderId)
            .ToListAsync();

        salesOrder.BeforeTaxAmount = salesOrderItems.Sum(x => x.Total ?? 0);

        var taxPercentage = salesOrder.Tax?.Percentage ?? 0;
        salesOrder.TaxAmount = (salesOrder.BeforeTaxAmount ?? 0) * taxPercentage / 100;

        salesOrder.AfterTaxAmount = (salesOrder.BeforeTaxAmount ?? 0) + (salesOrder.TaxAmount ?? 0);

        _salesOrderRepository.Update(salesOrder);
        await _unitOfWork.SaveAsync();
    }
}
