using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BrandManager.Commands;

public class BulkCreateBrandResult
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkCreateBrandErrorRow>? Errors { get; init; }
}

public class BulkCreateBrandErrorRow
{
    public int RowNumber { get; init; }
    public string? Name { get; init; }
    public string? Message { get; init; }
}

public class BulkCreateBrandRequest : IRequest<BulkCreateBrandResult>
{
    public byte[] Data { get; init; } = null!;
    public string? CreatedById { get; init; }
}

public class BulkCreateBrandValidator : AbstractValidator<BulkCreateBrandRequest>
{
    public BulkCreateBrandValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class BulkCreateBrandHandler : IRequestHandler<BulkCreateBrandRequest, BulkCreateBrandResult>
{
    private static readonly string[] AllowedStatuses = { "Active", "Inactive" };

    private readonly ICommandRepository<Brand> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _context;

    public BulkCreateBrandHandler(
        ICommandRepository<Brand> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext context
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _context = context;
    }

    public async Task<BulkCreateBrandResult> Handle(BulkCreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        // Brand names are unique per tenant, so a clash has to be caught per row and reported
        // rather than thrown - one bad row must not cost the user the rest of the file.
        var existingNames = await _context.Brand
            .AsNoTracking()
            .Where(x => x.IsDeleted == false)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var takenNames = new HashSet<string>(
            existingNames.Where(x => !string.IsNullOrWhiteSpace(x))!,
            StringComparer.OrdinalIgnoreCase);

        var errors = new List<BulkCreateBrandErrorRow>();
        var successCount = 0;
        var totalRows = 0;

        using var stream = new MemoryStream(request.Data);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var range = sheet.RangeUsed();

        if (range == null)
        {
            return new BulkCreateBrandResult
            {
                TotalRows = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Errors = new List<BulkCreateBrandErrorRow>()
            };
        }

        var rows = range.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            totalRows++;
            var rowNumber = row.RowNumber();
            var name = row.Cell(1).GetString().Trim();
            var description = row.Cell(2).GetString().Trim();
            var status = row.Cell(3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new BulkCreateBrandErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = "Name is required."
                });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new BulkCreateBrandErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = $"Status '{status}' is not valid. Use Active or Inactive."
                });
                continue;
            }

            // Covers both a name already in the database and one repeated earlier in this file.
            if (!takenNames.Add(name))
            {
                errors.Add(new BulkCreateBrandErrorRow
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Message = $"A brand named '{name}' already exists."
                });
                continue;
            }

            var entity = new Brand();
            entity.CreatedById = request.CreatedById;
            entity.Number = _numberSequenceService.GenerateNumber(nameof(Brand), "", "BRD");
            entity.Name = name;
            entity.Description = string.IsNullOrWhiteSpace(description) ? null : description;
            entity.Status = string.IsNullOrWhiteSpace(status)
                ? "Active"
                : AllowedStatuses.First(x => x.Equals(status, StringComparison.OrdinalIgnoreCase));

            await _repository.CreateAsync(entity, cancellationToken);
            successCount++;
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new BulkCreateBrandResult
        {
            TotalRows = totalRows,
            SuccessCount = successCount,
            FailureCount = errors.Count,
            Errors = errors
        };
    }
}
