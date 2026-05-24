using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CompanyManager.Commands;

public class CreateCompanyResult
{
    public Company? Data { get; set; }
}

public class CreateCompanyRequest : IRequest<CreateCompanyResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Currency { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FaxNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Website { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateCompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
    }
}

public class CreateCompanyHandler : IRequestHandler<CreateCompanyRequest, CreateCompanyResult>
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCompanyHandler(
        ICommandRepository<Company> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCompanyResult> Handle(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Company();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Currency = request.Currency;
        entity.Street = request.Street;
        entity.City = request.City;
        entity.State = request.State;
        entity.ZipCode = request.ZipCode;
        entity.Country = request.Country;
        entity.PhoneNumber = request.PhoneNumber;
        entity.FaxNumber = request.FaxNumber;
        entity.EmailAddress = request.EmailAddress;
        entity.Website = request.Website;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCompanyResult
        {
            Data = entity
        };
    }
}
