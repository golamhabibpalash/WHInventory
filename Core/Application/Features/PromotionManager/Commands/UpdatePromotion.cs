using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PromotionManager.Commands;

public class UpdatePromotionResult
{
    public Promotion? Data { get; set; }
}

public class UpdatePromotionRequest : IRequest<UpdatePromotionResult>
{
    public string? Id { get; init; }
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
    public string? UpdatedById { get; init; }
}

public class UpdatePromotionValidator : AbstractValidator<UpdatePromotionRequest>
{
    public UpdatePromotionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class UpdatePromotionHandler : IRequestHandler<UpdatePromotionRequest, UpdatePromotionResult>
{
    private readonly ICommandRepository<Promotion> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePromotionHandler(ICommandRepository<Promotion> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePromotionResult> Handle(UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
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

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePromotionResult { Data = entity };
    }
}
