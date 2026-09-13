using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;


public class GetCustomerSingleProfile : Profile
{
    public GetCustomerSingleProfile()
    {
    }
}

public class GetCustomerSingleResult
{
    public Customer? Data { get; init; }
}

public class GetCustomerSingleRequest : IRequest<GetCustomerSingleResult>
{
    public string? Id { get; init; }
}

public class GetCustomerSingleValidator : AbstractValidator<GetCustomerSingleRequest>
{
    public GetCustomerSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetCustomerSingleHandler : IRequestHandler<GetCustomerSingleRequest, GetCustomerSingleResult>
{
    private readonly IQueryContext _context;

    public GetCustomerSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetCustomerSingleResult> Handle(GetCustomerSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Customer
            .AsNoTracking()
            .ApplyIsDeletedFilter()
            .Include(x => x.CustomerGroup)
            .Include(x => x.CustomerCategory)
            .Include(x => x.CustomerContactList.Where(item => !item.IsDeleted))
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetCustomerSingleResult
        {
            Data = entity
        };
    }
}
