using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductGroupManager.Commands;

public class DeleteProductGroupResult
{
    public ProductGroup? Data { get; set; }
}

public class DeleteProductGroupRequest : IRequest<DeleteProductGroupResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteProductGroupValidator : AbstractValidator<DeleteProductGroupRequest>
{
    public DeleteProductGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteProductGroupHandler : IRequestHandler<DeleteProductGroupRequest, DeleteProductGroupResult>
{
    private readonly ICommandRepository<ProductGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public DeleteProductGroupHandler(
        ICommandRepository<ProductGroup> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<DeleteProductGroupResult> Handle(DeleteProductGroupRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var hasChildren = await _queryContext.ProductGroup
            .AnyAsync(x => x.ParentId == entity.Id && x.IsDeleted == false, cancellationToken);

        if (hasChildren)
        {
            throw new Exception("Cannot delete a product group that has child groups. Remove or reassign child groups first.");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProductGroupResult
        {
            Data = entity
        };
    }
}

