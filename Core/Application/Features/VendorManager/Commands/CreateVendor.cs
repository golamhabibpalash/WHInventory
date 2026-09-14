using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.VendorManager.Commands;

public class CreateVendorResult
{
    public Vendor? Data { get; set; }
}

public class CreateVendorRequest : IRequest<CreateVendorResult>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
    public string? WhatsApp { get; set; }
    public string? LinkedIn { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TwitterX { get; set; }
    public string? TikTok { get; set; }
    public string? VendorGroupId { get; set; }
    public string? VendorCategoryId { get; set; }
    public string? CreatedById { get; init; }
}

public class CreateVendorValidator : AbstractValidator<CreateVendorRequest>
{
    public CreateVendorValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone Number is required.")
            .Matches(@"^(?:\+?88)?01[3-9]\d{8}$").WithMessage("Enter a valid phone number (e.g. 01711234567 or +8801711234567).");
        RuleFor(x => x.EmailAddress)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress));
    }
}

public class CreateVendorHandler : IRequestHandler<CreateVendorRequest, CreateVendorResult>
{
    private readonly ICommandRepository<Vendor> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateVendorHandler(
        ICommandRepository<Vendor> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateVendorResult> Handle(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Vendor();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Number = _numberSequenceService.GenerateNumber(nameof(Vendor), "", "CST");
        entity.Description = request.Description;
        entity.Street = request.Street;
        entity.City = request.City;
        entity.State = request.State;
        entity.ZipCode = request.ZipCode;
        entity.Country = request.Country;
        entity.PhoneNumber = request.PhoneNumber;
        entity.FaxNumber = request.FaxNumber;
        entity.EmailAddress = request.EmailAddress;
        entity.Website = request.Website;
        entity.WhatsApp = request.WhatsApp;
        entity.LinkedIn = request.LinkedIn;
        entity.Facebook = request.Facebook;
        entity.Instagram = request.Instagram;
        entity.TwitterX = request.TwitterX;
        entity.TikTok = request.TikTok;
        entity.VendorGroupId = request.VendorGroupId;
        entity.VendorCategoryId = request.VendorCategoryId;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateVendorResult
        {
            Data = entity
        };
    }
}
