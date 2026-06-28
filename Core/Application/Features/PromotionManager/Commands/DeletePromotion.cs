using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PromotionManager.Commands;

public class DeletePromotionResult
{
    public Promotion? Data { get; set; }
}

public class DeletePromotionRequest : IRequest<DeletePromotionResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePromotionValidator : AbstractValidator<DeletePromotionRequest>
{
    public DeletePromotionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePromotionHandler : IRequestHandler<DeletePromotionRequest, DeletePromotionResult>
{
    private readonly ICommandRepository<Promotion> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePromotionHandler(ICommandRepository<Promotion> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePromotionResult> Handle(DeletePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePromotionResult { Data = entity };
    }
}
