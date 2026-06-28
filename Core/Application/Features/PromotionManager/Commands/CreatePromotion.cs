using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PromotionManager.Commands;

public class CreatePromotionResult
{
    public Promotion? Data { get; set; }
}

public class CreatePromotionRequest : IRequest<CreatePromotionResult>
{
    public string? Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? ProductId { get; init; }
    public string? PricePolicyId { get; init; }
    public double? PromotionalPrice { get; init; }
    public double? DiscountPercent { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsActive { get; init; } = true;
    public string? CreatedById { get; init; }
}

public class CreatePromotionValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class CreatePromotionHandler : IRequestHandler<CreatePromotionRequest, CreatePromotionResult>
{
    private readonly ICommandRepository<Promotion> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePromotionHandler(ICommandRepository<Promotion> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePromotionResult> Handle(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Promotion();
        entity.CreatedById = request.CreatedById;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Description = request.Description;
        entity.ProductId = request.ProductId;
        entity.PricePolicyId = request.PricePolicyId;
        entity.PromotionalPrice = request.PromotionalPrice;
        entity.DiscountPercent = request.DiscountPercent;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Priority = request.Priority;
        entity.IsActive = request.IsActive;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePromotionResult { Data = entity };
    }
}
