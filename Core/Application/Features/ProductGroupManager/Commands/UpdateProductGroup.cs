using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductGroupManager.Commands;

public class UpdateProductGroupResult
{
    public ProductGroup? Data { get; set; }
}

public class UpdateProductGroupRequest : IRequest<UpdateProductGroupResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? UpdatedById { get; init; }
    public string? ParentId { get; init; }
}

public class UpdateProductGroupValidator : AbstractValidator<UpdateProductGroupRequest>
{
    public UpdateProductGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateProductGroupHandler : IRequestHandler<UpdateProductGroupRequest, UpdateProductGroupResult>
{
    private readonly ICommandRepository<ProductGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _queryContext;

    public UpdateProductGroupHandler(
        ICommandRepository<ProductGroup> repository,
        IUnitOfWork unitOfWork,
        IQueryContext queryContext
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _queryContext = queryContext;
    }

    public async Task<UpdateProductGroupResult> Handle(UpdateProductGroupRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId;

        if (parentId != null)
        {
            if (parentId == entity.Id)
            {
                throw new Exception("A product group cannot be its own parent.");
            }

            var allGroups = await _queryContext.ProductGroup
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var descendantIds = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(entity.Id);
            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                foreach (var child in allGroups.Where(x => x.ParentId == currentId))
                {
                    if (descendantIds.Add(child.Id))
                    {
                        stack.Push(child.Id);
                    }
                }
            }

            if (descendantIds.Contains(parentId))
            {
                throw new Exception("Circular reference detected: the selected parent is a descendant of this product group.");
            }
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ParentId = parentId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateProductGroupResult
        {
            Data = entity
        };
    }
}

