using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentManager.Queries;

public record GetPaymentListByModuleDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
    public string? ModuleNumber { get; init; }
    public DateTime? PaymentDate { get; init; }
    public PaymentDirection Direction { get; init; }
    public double? Amount { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? PaymentMethodName { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentListByModuleResult
{
    public List<GetPaymentListByModuleDto>? Data { get; init; }
}

public class GetPaymentListByModuleRequest : IRequest<GetPaymentListByModuleResult>
{
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
}

public class GetPaymentListByModuleHandler : IRequestHandler<GetPaymentListByModuleRequest, GetPaymentListByModuleResult>
{
    private readonly IQueryContext _context;

    public GetPaymentListByModuleHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPaymentListByModuleResult> Handle(GetPaymentListByModuleRequest request, CancellationToken cancellationToken)
    {
        var data = await _context.Payment
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == request.ModuleName && x.ModuleId == request.ModuleId)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => new GetPaymentListByModuleDto
            {
                Id = x.Id,
                Number = x.Number,
                ModuleName = x.ModuleName,
                ModuleId = x.ModuleId,
                ModuleNumber = x.ModuleNumber,
                PaymentDate = x.PaymentDate,
                Direction = x.Direction,
                Amount = x.Amount,
                PaymentMethodId = x.PaymentMethodId,
                PaymentMethodName = x.PaymentMethod != null ? x.PaymentMethod.Name : string.Empty,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new GetPaymentListByModuleResult { Data = data };
    }
}
