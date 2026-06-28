using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.QuantityBreakManager.Commands;

public class DeleteQuantityBreakResult
{
    public QuantityBreak? Data { get; set; }
}

public class DeleteQuantityBreakRequest : IRequest<DeleteQuantityBreakResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteQuantityBreakValidator : AbstractValidator<DeleteQuantityBreakRequest>
{
    public DeleteQuantityBreakValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteQuantityBreakHandler : IRequestHandler<DeleteQuantityBreakRequest, DeleteQuantityBreakResult>
{
    private readonly ICommandRepository<QuantityBreak> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQuantityBreakHandler(ICommandRepository<QuantityBreak> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteQuantityBreakResult> Handle(DeleteQuantityBreakRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteQuantityBreakResult { Data = entity };
    }
}
