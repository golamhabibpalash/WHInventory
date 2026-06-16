using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BrandManager.Commands;

public class CreateBrandResult
{
    public Brand? Data { get; set; }
}

public class CreateBrandRequest : IRequest<CreateBrandResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ImageName { get; init; }
    public string? Status { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateBrandValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateBrandHandler : IRequestHandler<CreateBrandRequest, CreateBrandResult>
{
    private readonly ICommandRepository<Brand> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _queryContext;

    public CreateBrandHandler(
        ICommandRepository<Brand> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext queryContext
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _queryContext = queryContext;
    }

    public async Task<CreateBrandResult> Handle(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var duplicate = await _queryContext.Brand
            .AnyAsync(x => x.Name == request.Name && x.IsDeleted == false, cancellationToken);

        if (duplicate)
        {
            throw new ValidationException($"A brand with the name '{request.Name}' already exists.");
        }

        var entity = new Brand();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(Brand), "", "BRD");
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ImageName = string.IsNullOrWhiteSpace(request.ImageName) ? null : request.ImageName;
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBrandResult
        {
            Data = entity
        };
    }
}
