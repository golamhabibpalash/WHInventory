using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BrandManager.Commands;

public class UpdateBrandResult
{
    public Brand? Data { get; set; }
}

public class UpdateBrandRequest : IRequest<UpdateBrandResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ImageName { get; init; }
    public string? Status { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateBrandValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateBrandHandler : IRequestHandler<UpdateBrandRequest, UpdateBrandResult>
{
    private readonly ICommandRepository<Brand> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBrandHandler(
        ICommandRepository<Brand> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBrandResult> Handle(UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ImageName = string.IsNullOrWhiteSpace(request.ImageName) ? null : request.ImageName;
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateBrandResult
        {
            Data = entity
        };
    }
}
