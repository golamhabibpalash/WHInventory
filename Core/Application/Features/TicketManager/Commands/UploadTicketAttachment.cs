using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.FileDocumentManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Commands;

public class UploadTicketAttachmentResult
{
    public string? DocumentName { get; set; }
}

public class UploadTicketAttachmentRequest : IRequest<UploadTicketAttachmentResult>
{
    public string? TicketId { get; init; }
    public string? OriginalFileName { get; init; }
    public string? Extension { get; init; }
    public byte[]? Data { get; init; }
    public long? Size { get; init; }
    public string? CreatedById { get; init; }
}

public class UploadTicketAttachmentValidator : AbstractValidator<UploadTicketAttachmentRequest>
{
    public UploadTicketAttachmentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.OriginalFileName).NotEmpty();
        RuleFor(x => x.Extension).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.Size)
            .NotEmpty()
            .LessThanOrEqualTo(TicketAttachmentHelper.MaxFileSizeBytes)
            .WithMessage($"File exceeds the {TicketAttachmentHelper.MaxFileSizeBytes / (1024 * 1024)} MB limit.");
    }
}

public class UploadTicketAttachmentHandler : IRequestHandler<UploadTicketAttachmentRequest, UploadTicketAttachmentResult>
{
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly IFileDocumentService _fileDocumentService;
    private readonly TicketHistoryRecorder _historyRecorder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadTicketAttachmentHandler(
        ICommandRepository<Ticket> ticketRepository,
        IFileDocumentService fileDocumentService,
        TicketHistoryRecorder historyRecorder,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser
        )
    {
        _ticketRepository = ticketRepository;
        _fileDocumentService = fileDocumentService;
        _historyRecorder = historyRecorder;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UploadTicketAttachmentResult> Handle(UploadTicketAttachmentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetAsync(request.TicketId ?? string.Empty, cancellationToken);
        if (ticket == null)
        {
            throw new Exception($"Entity not found: {request.TicketId}");
        }

        var isAgent = _currentUser.IsInRole(TicketAccessGuard.AgentRole);
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);

        // Extension is derived server-side from the filename (same as FileDocumentController
        // already does) — never trust a client-declared Content-Type alone.
        if (!TicketAttachmentHelper.IsExtensionAllowed(request.Extension))
        {
            throw new Exception($"File type '.{request.Extension}' is not allowed for ticket attachments.");
        }

        var documentName = await _fileDocumentService.UploadAsync(
            request.OriginalFileName,
            request.Extension,
            request.Data,
            request.Size,
            description: null,
            createdById: request.CreatedById,
            moduleName: "Ticket",
            moduleId: request.TicketId,
            cancellationToken);

        _historyRecorder.Record(ticket.Id, "AttachmentUploaded", null, request.OriginalFileName, request.CreatedById);
        ticket.LastActivityAtUtc = DateTime.UtcNow;
        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadTicketAttachmentResult { DocumentName = documentName };
    }
}
