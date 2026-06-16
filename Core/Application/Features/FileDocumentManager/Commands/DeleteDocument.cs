using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.FileDocumentManager.Commands;

public class DeleteDocumentResult
{
    public FileDocument? Data { get; init; }
}

public class DeleteDocumentRequest : IRequest<DeleteDocumentResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteDocumentValidator : AbstractValidator<DeleteDocumentRequest>
{
    public DeleteDocumentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteDocumentHandler : IRequestHandler<DeleteDocumentRequest, DeleteDocumentResult>
{
    private readonly ICommandRepository<FileDocument> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDocumentHandler(ICommandRepository<FileDocument> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteDocumentResult> Handle(DeleteDocumentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Document not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteDocumentResult { Data = entity };
    }
}
