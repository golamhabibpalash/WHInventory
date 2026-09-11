using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DeliveryOrderManager.Queries;

public record GetDeliveryOrderListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public DeliveryOrderStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public string? SalesOrderNumber { get; init; }
    public double TotalOrderedQty { get; set; }
    public double TotalDeliveredQty { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetDeliveryOrderListProfile : Profile
{
    public GetDeliveryOrderListProfile()
    {
        CreateMap<DeliveryOrder, GetDeliveryOrderListDto>()
            .ForMember(
                dest => dest.SalesOrderNumber,
                opt => opt.MapFrom(src => src.SalesOrder != null ? src.SalesOrder.Number : string.Empty)
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetDeliveryOrderListResult
{
    public List<GetDeliveryOrderListDto>? Data { get; init; }
}

public class GetDeliveryOrderListRequest : IRequest<GetDeliveryOrderListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetDeliveryOrderListHandler : IRequestHandler<GetDeliveryOrderListRequest, GetDeliveryOrderListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetDeliveryOrderListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetDeliveryOrderListResult> Handle(GetDeliveryOrderListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .DeliveryOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Include(x => x.SalesOrder)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetDeliveryOrderListDto>>(entities);

        var doIds = dtos.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id!).ToList();
        if (doIds.Count > 0)
        {
            var soIds = dtos.Where(x => !string.IsNullOrEmpty(x.SalesOrderId)).Select(x => x.SalesOrderId!).Distinct().ToList();
            var orderedDict = new Dictionary<string, double>();
            if (soIds.Count > 0)
            {
                var orderedItems = await _context.SalesOrderItem
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(x => x.SalesOrderId != null && soIds.Contains(x.SalesOrderId))
                    .GroupBy(x => x.SalesOrderId!)
                    .Select(g => new { SOId = g.Key, Total = g.Sum(x => x.Quantity ?? 0.0) })
                    .ToListAsync(cancellationToken);
                orderedDict = orderedItems.ToDictionary(x => x.SOId, x => x.Total);
            }

            var deliveredRows = await _context.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId != null && doIds.Contains(x.ModuleId)
                         && x.ModuleName == nameof(DeliveryOrder))
                .GroupBy(x => x.ModuleId!)
                .Select(g => new { DOId = g.Key, Total = g.Sum(x => x.Movement ?? 0.0) })
                .ToListAsync(cancellationToken);
            var deliveredDict = deliveredRows.ToDictionary(x => x.DOId, x => x.Total);

            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.Id)) continue;

                var totalOrdered = !string.IsNullOrEmpty(dto.SalesOrderId) && orderedDict.TryGetValue(dto.SalesOrderId, out var oq) ? oq : 0.0;
                var totalDelivered = deliveredDict.TryGetValue(dto.Id, out var dq) ? dq : 0.0;

                dto.TotalOrderedQty = totalOrdered;
                dto.TotalDeliveredQty = totalDelivered;
                dto.DeliveryStatus = totalOrdered <= 0
                    ? "N/A"
                    : totalDelivered >= totalOrdered
                        ? "Complete"
                        : totalDelivered > 0
                            ? "Partial"
                            : "Not Started";
            }
        }

        return new GetDeliveryOrderListResult
        {
            Data = dtos
        };
    }


}



