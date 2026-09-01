using Application.Common.Repositories;
using Application.Common.Tenancy;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TenantManager.Commands;

public class DeleteTenantResult
{
    public Tenant? Data { get; init; }
}

public class DeleteTenantRequest : IRequest<DeleteTenantResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteTenantValidator : AbstractValidator<DeleteTenantRequest>
{
    public DeleteTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteTenantHandler : IRequestHandler<DeleteTenantRequest, DeleteTenantResult>
{
    private readonly ICommandRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTenantHandler(
        ICommandRepository<Tenant> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteTenantResult> Handle(DeleteTenantRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        if (entity.Id == TenantDefaults.DefaultTenantId)
        {
            throw new ValidationException("The default tenant cannot be deleted.");
        }

        // Soft delete only. The tenant's rows stay in place and stay hidden behind the query
        // filter; nothing about a delete here destroys another organisation's data.
        entity.IsActive = false;
        entity.UpdatedById = request.DeletedById;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteTenantResult { Data = entity };
    }
}
