using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.QuickShortcutManager.Queries;

public record GetQuickShortcutListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Icon { get; init; }
    public string? Url { get; init; }
    public int SortOrder { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetQuickShortcutListProfile : Profile
{
    public GetQuickShortcutListProfile()
    {
        CreateMap<QuickShortcut, GetQuickShortcutListDto>();
    }
}

public class GetQuickShortcutListResult
{
    public List<GetQuickShortcutListDto>? Data { get; init; }
}

public class GetQuickShortcutListRequest : IRequest<GetQuickShortcutListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetQuickShortcutListHandler : IRequestHandler<GetQuickShortcutListRequest, GetQuickShortcutListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetQuickShortcutListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetQuickShortcutListResult> Handle(GetQuickShortcutListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .QuickShortcut
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .AsQueryable();

        var entities = await query.Take(2000).ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetQuickShortcutListDto>>(entities);

        return new GetQuickShortcutListResult
        {
            Data = dtos
        };
    }
}
