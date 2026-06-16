using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FileDocumentManager.Queries;

public record GetDocumentsByModuleDto
{
    public string? Id { get; init; }
    public string? OriginalName { get; init; }
    public string? GeneratedName { get; init; }
    public string? Extension { get; init; }
    public long? FileSize { get; init; }
    public string? Description { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetDocumentsByModuleResult
{
    public List<GetDocumentsByModuleDto>? Data { get; init; }
}

public class GetDocumentsByModuleRequest : IRequest<GetDocumentsByModuleResult>
{
    public string? ModuleName { get; init; }
    public string? ModuleId { get; init; }
}

public class GetDocumentsByModuleValidator : AbstractValidator<GetDocumentsByModuleRequest>
{
    public GetDocumentsByModuleValidator()
    {
        RuleFor(x => x.ModuleName).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
    }
}

public class GetDocumentsByModuleHandler : IRequestHandler<GetDocumentsByModuleRequest, GetDocumentsByModuleResult>
{
    private readonly IQueryContext _context;

    public GetDocumentsByModuleHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetDocumentsByModuleResult> Handle(GetDocumentsByModuleRequest request, CancellationToken cancellationToken)
    {
        var entities = await _context.FileDocument
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.ModuleName == request.ModuleName && x.ModuleId == request.ModuleId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new GetDocumentsByModuleDto
            {
                Id = x.Id,
                OriginalName = x.OriginalName,
                GeneratedName = x.GeneratedName,
                Extension = x.Extension,
                FileSize = x.FileSize,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new GetDocumentsByModuleResult { Data = entities };
    }
}
