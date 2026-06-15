using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BrandManager.Commands;

public class ToggleBrandStatusResult
{
    public Brand? Data { get; set; }
}

public class ToggleBrandStatusRequest : IRequest<ToggleBrandStatusResult>
{
    public string? Id { get; init; }
    public string? UpdatedById { get; init; }
}

public class ToggleBrandStatusValidator : AbstractValidator<ToggleBrandStatusRequest>
{
    public ToggleBrandStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ToggleBrandStatusHandler : IRequestHandler<ToggleBrandStatusRequest, ToggleBrandStatusResult>
{
    private readonly ICommandRepository<Brand> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleBrandStatusHandler(
        ICommandRepository<Brand> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ToggleBrandStatusResult> Handle(ToggleBrandStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Status = entity.Status == "Active" ? "Inactive" : "Active";

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ToggleBrandStatusResult
        {
            Data = entity
        };
    }
}
