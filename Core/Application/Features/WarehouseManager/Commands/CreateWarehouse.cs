using Application.Common.Repositories;
using Application.Common.Services.SubscriptionManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.WarehouseManager.Commands;

public class CreateWarehouseResult
{
    public Warehouse? Data { get; set; }
}

public class CreateWarehouseRequest : IRequest<CreateWarehouseResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseRequest, CreateWarehouseResult>
{
    private readonly ICommandRepository<Warehouse> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionQuotaService _quotaService;

    public CreateWarehouseHandler(
        ICommandRepository<Warehouse> repository,
        IUnitOfWork unitOfWork,
        ISubscriptionQuotaService quotaService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _quotaService = quotaService;
    }

    public async Task<CreateWarehouseResult> Handle(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await _quotaService.EnsureCanAddWarehouseAsync(cancellationToken);

        var entity = new Warehouse();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.SystemWarehouse = false;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateWarehouseResult
        {
            Data = entity
        };
    }
}
