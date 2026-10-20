using System.Text.RegularExpressions;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.CustomerManager.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record SearchCustomerDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Number { get; init; }
    public string? PhoneNumber { get; init; }

    /// <summary>True when <see cref="PhoneNumber"/> is the same mobile number as the search term.</summary>
    public bool IsExactMobileMatch { get; init; }
}

public class SearchCustomerResult
{
    public List<SearchCustomerDto>? Data { get; init; }
}

/// <summary>
/// Type-ahead lookup for the sales order customer field: matches the mobile number (digits only,
/// any of the 01… / 88 01… / +88 01… forms) or, when the term has letters, the customer name.
/// </summary>
public class SearchCustomerRequest : IRequest<SearchCustomerResult>
{
    public string? Term { get; init; }
    public int Take { get; init; } = 8;
}

public class SearchCustomerValidator : AbstractValidator<SearchCustomerRequest>
{
    public SearchCustomerValidator()
    {
        RuleFor(x => x.Term).MaximumLength(50);
        RuleFor(x => x.Take).InclusiveBetween(1, 20);
    }
}

public partial class SearchCustomerHandler : IRequestHandler<SearchCustomerRequest, SearchCustomerResult>
{
    private const int MinimumDigits = 3;

    private readonly IQueryContext _context;

    public SearchCustomerHandler(IQueryContext context)
    {
        _context = context;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();

    public async Task<SearchCustomerResult> Handle(SearchCustomerRequest request, CancellationToken cancellationToken)
    {
        var term = (request.Term ?? string.Empty).Trim();
        var digits = NonDigits().Replace(term, string.Empty);
        var hasLetters = term.Any(char.IsLetter);

        // "88017…" / "+88017…" → "017…", so the search matches however the number was stored.
        if (digits.StartsWith("880"))
            digits = digits[2..];

        if (digits.Length < MinimumDigits && !(hasLetters && term.Length >= 2))
            return new SearchCustomerResult { Data = new List<SearchCustomerDto>() };

        var nameTerm = term.ToLower();

        var query = _context
            .Customer
            .AsNoTracking()
            .ApplyIsDeletedFilter(false);

        query = hasLetters
            ? query.Where(x => x.Name != null && x.Name.ToLower().Contains(nameTerm))
            : query.Where(x => x.PhoneNumber != null && x.PhoneNumber.Contains(digits));

        var rows = await query
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Number, x.PhoneNumber })
            .Take(request.Take * 3)
            .ToListAsync(cancellationToken);

        var exactMobile = CustomerMobileNumber.TryNormalize(term, out var normalized) ? normalized : null;

        var data = rows
            .Select(x => new SearchCustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                Number = x.Number,
                PhoneNumber = x.PhoneNumber,
                IsExactMobileMatch = exactMobile != null
                    && CustomerMobileNumber.TryNormalize(x.PhoneNumber, out var stored)
                    && stored == exactMobile
            })
            .OrderByDescending(x => x.IsExactMobileMatch)
            .Take(request.Take)
            .ToList();

        return new SearchCustomerResult
        {
            Data = data
        };
    }
}
