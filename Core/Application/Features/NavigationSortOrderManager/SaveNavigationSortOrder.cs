using Application.Common.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.NavigationSortOrderManager;

public class SaveNavigationSortOrderResult
{
    public string? UserId { get; init; }
}

public class SaveNavigationSortOrderRequest : IRequest<SaveNavigationSortOrderResult>
{
    public string? UserId { get; init; }
    public string? SortOrderJson { get; init; }
}

public class SaveNavigationSortOrderHandler : IRequestHandler<SaveNavigationSortOrderRequest, SaveNavigationSortOrderResult>
{
    private readonly ICommandRepository<NavigationMenuSortOrder> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveNavigationSortOrderHandler(
        ICommandRepository<NavigationMenuSortOrder> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveNavigationSortOrderResult> Handle(SaveNavigationSortOrderRequest request, CancellationToken cancellationToken)
    {
        var existing = _repository.GetQuery()
            .FirstOrDefault(x => x.UserId == request.UserId && !x.IsDeleted);

        if (existing != null)
        {
            existing.SortOrderJson = request.SortOrderJson;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedById = request.UserId;
            _repository.Update(existing);
        }
        else
        {
            var entity = new NavigationMenuSortOrder
            {
                UserId = request.UserId,
                SortOrderJson = request.SortOrderJson,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedById = request.UserId
            };
            await _repository.CreateAsync(entity, cancellationToken);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
        return new SaveNavigationSortOrderResult { UserId = request.UserId };
    }
}
