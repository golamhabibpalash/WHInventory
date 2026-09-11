using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class DeleteTicketAttachmentResult
{
    public bool Success { get; set; }
}

public class DeleteTicketAttachmentRequest : IRequest<DeleteTicketAttachmentResult>
{
    public string? AttachmentId { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteTicketAttachmentValidator : AbstractValidator<DeleteTicketAttachmentRequest>
{
    public DeleteTicketAttachmentValidator()
    {
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}

public class DeleteTicketAttachmentHandler : IRequestHandler<DeleteTicketAttachmentRequest, DeleteTicketAttachmentResult>
{
    private readonly ICommandRepository<FileDocument> _documentRepository;
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly ICurrentUserService _currentUser;

    public DeleteTicketAttachmentHandler(
        ICommandRepository<FileDocument> documentRepository,
        ICommandRepository<Ticket> ticketRepository,
        IUnitOfWork unitOfWork,
        TicketHistoryRecorder historyRecorder,
        ICurrentUserService currentUser
        )
    {
        _documentRepository = documentRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _historyRecorder = historyRecorder;
        _currentUser = currentUser;
    }

    public async Task<DeleteTicketAttachmentResult> Handle(DeleteTicketAttachmentRequest request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetAsync(request.AttachmentId ?? string.Empty, cancellationToken);
        if (document == null || document.ModuleName != "Ticket" || string.IsNullOrEmpty(document.ModuleId))
        {
            throw new Exception("Attachment not found.");
        }

        var ticket = await _ticketRepository.GetAsync(document.ModuleId, cancellationToken);
        if (ticket == null)
        {
            throw new Exception("Attachment not found.");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        // Only the uploader themselves or an agent may remove an attachment — not just anyone
        // who can view the ticket.
        if (!isAgent && document.CreatedById != _currentUser.UserId)
        {
            throw new Exception("You do not have access to remove this attachment.");
        }

        document.UpdatedById = request.DeletedById;
        _documentRepository.Delete(document);

        _historyRecorder.Record(ticket.Id, "AttachmentRemoved", document.OriginalName, null, request.DeletedById);
        ticket.LastActivityAtUtc = DateTime.UtcNow;
        _ticketRepository.Update(ticket);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteTicketAttachmentResult { Success = true };
    }
}
