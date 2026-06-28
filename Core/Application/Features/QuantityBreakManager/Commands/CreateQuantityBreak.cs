using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuantityBreakManager.Commands;

public class CreateQuantityBreakResult
{
    public QuantityBreak? Data { get; set; }
}

public class CreateQuantityBreakRequest : IRequest<CreateQuantityBreakResult>
{
    public string? ProductPriceId { get; init; }
    public double MinQuantity { get; init; } = 1;
    public double? MaxQuantity { get; init; }
    public double Price { get; init; } = 0;
    public string? CreatedById { get; init; }
}

public class CreateQuantityBreakValidator : AbstractValidator<CreateQuantityBreakRequest>
{
    public CreateQuantityBreakValidator()
    {
        RuleFor(x => x.ProductPriceId).NotEmpty();
        RuleFor(x => x.MinQuantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreateQuantityBreakHandler : IRequestHandler<CreateQuantityBreakRequest, CreateQuantityBreakResult>
{
    private readonly ICommandRepository<QuantityBreak> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuantityBreakHandler(ICommandRepository<QuantityBreak> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateQuantityBreakResult> Handle(CreateQuantityBreakRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new QuantityBreak();
        entity.CreatedById = request.CreatedById;
        entity.ProductPriceId = request.ProductPriceId;
        entity.MinQuantity = request.MinQuantity;
        entity.MaxQuantity = request.MaxQuantity;
        entity.Price = request.Price;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateQuantityBreakResult { Data = entity };
    }
}
