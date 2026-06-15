using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SecurityManager.Queries;

public record GetLockScreenInfoDto
{
    public string? Name { get; init; }
    public string? LogoName { get; init; }
}

public class GetLockScreenInfoProfile : Profile
{
    public GetLockScreenInfoProfile()
    {
        CreateMap<Company, GetLockScreenInfoDto>();
    }
}

public class GetLockScreenInfoResult
{
    public GetLockScreenInfoDto? Data { get; init; }
}

public class GetLockScreenInfoRequest : IRequest<GetLockScreenInfoResult>
{
}

public class GetLockScreenInfoHandler : IRequestHandler<GetLockScreenInfoRequest, GetLockScreenInfoResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetLockScreenInfoHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetLockScreenInfoResult> Handle(GetLockScreenInfoRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context
            .Company
            .AsNoTracking()
            .ApplyIsDeletedFilter()
            .FirstOrDefaultAsync(cancellationToken);

        var dto = _mapper.Map<GetLockScreenInfoDto>(entity);

        return new GetLockScreenInfoResult
        {
            Data = dto
        };
    }
}
