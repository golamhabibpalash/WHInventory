using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.GoodsReceiveManager.Queries;

public record GetGoodsReceiveListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? ReceiveDate { get; init; }
    public GoodsReceiveStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderNumber { get; init; }
    public double TotalOrderedQty { get; set; }
    public double TotalReceivedQty { get; set; }
    public string? ReceivingStatus { get; set; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetGoodsReceiveListProfile : Profile
{
    public GetGoodsReceiveListProfile()
    {
        CreateMap<GoodsReceive, GetGoodsReceiveListDto>()
            .ForMember(
                dest => dest.PurchaseOrderNumber,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? src.PurchaseOrder.Number : string.Empty)
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetGoodsReceiveListResult
{
    public List<GetGoodsReceiveListDto>? Data { get; init; }
}

public class GetGoodsReceiveListRequest : IRequest<GetGoodsReceiveListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetGoodsReceiveListHandler : IRequestHandler<GetGoodsReceiveListRequest, GetGoodsReceiveListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetGoodsReceiveListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetGoodsReceiveListResult> Handle(GetGoodsReceiveListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Include(x => x.PurchaseOrder)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetGoodsReceiveListDto>>(entities);

        // Compute receiving status for each goods receive
        var grIds = dtos.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id!).ToList();
        if (grIds.Count > 0)
        {
            // Total ordered qty per PO
            var poIds = dtos.Where(x => !string.IsNullOrEmpty(x.PurchaseOrderId)).Select(x => x.PurchaseOrderId!).Distinct().ToList();
            var orderedDict = new Dictionary<string, double>();
            if (poIds.Count > 0)
            {
                var orderedItems = await _context.PurchaseOrderItem
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(x => x.PurchaseOrderId != null && poIds.Contains(x.PurchaseOrderId))
                    .GroupBy(x => x.PurchaseOrderId!)
                    .Select(g => new { POId = g.Key, Total = g.Sum(x => x.Quantity ?? 0.0) })
                    .ToListAsync(cancellationToken);
                orderedDict = orderedItems.ToDictionary(x => x.POId, x => x.Total);
            }

            // Total received qty per goods receive
            var receivedRows = await _context.InventoryTransaction
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.ModuleId != null && grIds.Contains(x.ModuleId)
                         && x.ModuleName == nameof(GoodsReceive))
                .GroupBy(x => x.ModuleId!)
                .Select(g => new { GRId = g.Key, Total = g.Sum(x => x.Movement ?? 0.0) })
                .ToListAsync(cancellationToken);
            var receivedDict = receivedRows.ToDictionary(x => x.GRId, x => x.Total);

            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.Id)) continue;

                var totalOrdered = !string.IsNullOrEmpty(dto.PurchaseOrderId) && orderedDict.TryGetValue(dto.PurchaseOrderId, out var oq) ? oq : 0.0;
                var totalReceived = receivedDict.TryGetValue(dto.Id, out var rq) ? rq : 0.0;

                dto.TotalOrderedQty = totalOrdered;
                dto.TotalReceivedQty = totalReceived;
                dto.ReceivingStatus = totalOrdered <= 0
                    ? "N/A"
                    : totalReceived >= totalOrdered
                        ? "Complete"
                        : totalReceived > 0
                            ? "Partial"
                            : "Not Started";
            }
        }

        return new GetGoodsReceiveListResult
        {
            Data = dtos
        };
    }


}



