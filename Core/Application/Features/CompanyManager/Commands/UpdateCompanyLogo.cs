using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CompanyManager.Commands;

public class UpdateCompanyLogoResult
{
    public string? LogoName { get; set; }
}

public class UpdateCompanyLogoRequest : IRequest<UpdateCompanyLogoResult>
{
    public string? Id { get; init; }
    public string? LogoName { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateCompanyLogoValidator : AbstractValidator<UpdateCompanyLogoRequest>
{
    public UpdateCompanyLogoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LogoName).NotEmpty();
    }
}

public class UpdateCompanyLogoHandler : IRequestHandler<UpdateCompanyLogoRequest, UpdateCompanyLogoResult>
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCompanyLogoHandler(ICommandRepository<Company> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCompanyLogoResult> Handle(UpdateCompanyLogoRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
            throw new Exception($"Entity not found: {request.Id}");

        entity.UpdatedById = request.UpdatedById;
        entity.LogoName = request.LogoName;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCompanyLogoResult { LogoName = entity.LogoName };
    }
}
