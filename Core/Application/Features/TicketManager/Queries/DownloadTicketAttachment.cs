using Application.Common.Repositories;
using Application.Common.Services.CurrentUserManager;
using Application.Common.Services.FileDocumentManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TicketManager.Queries;

public class DownloadTicketAttachmentResult
{
    public byte[]? Data { get; init; }
    public string? OriginalName { get; init; }
    public string? Extension { get; init; }
}

/// <summary>
/// Deliberately keyed by the FileDocument's own Id, not its generated storage filename — the
/// filename is an implementation detail this endpoint resolves internally after verifying the
/// caller may access the owning ticket, rather than trusting a client-supplied filename that
/// could be re-pointed at an unrelated ticket's attachment.
/// </summary>
public class DownloadTicketAttachmentRequest : IRequest<DownloadTicketAttachmentResult>
{
    public string? AttachmentId { get; init; }
}

public class DownloadTicketAttachmentValidator : AbstractValidator<DownloadTicketAttachmentRequest>
{
    public DownloadTicketAttachmentValidator()
    {
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}

public class DownloadTicketAttachmentHandler : IRequestHandler<DownloadTicketAttachmentRequest, DownloadTicketAttachmentResult>
{
    private readonly ICommandRepository<FileDocument> _documentRepository;
    private readonly ICommandRepository<Ticket> _ticketRepository;
    private readonly IFileDocumentService _fileDocumentService;
    private readonly ICurrentUserService _currentUser;

    public DownloadTicketAttachmentHandler(
        ICommandRepository<FileDocument> documentRepository,
        ICommandRepository<Ticket> ticketRepository,
        IFileDocumentService fileDocumentService,
        ICurrentUserService currentUser
        )
    {
        _documentRepository = documentRepository;
        _ticketRepository = ticketRepository;
        _fileDocumentService = fileDocumentService;
        _currentUser = currentUser;
    }

    public async Task<DownloadTicketAttachmentResult> Handle(DownloadTicketAttachmentRequest request, CancellationToken cancellationToken)
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
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser.UserId, isAgent);

        var data = await _fileDocumentService.GetFileAsync(document.GeneratedName ?? string.Empty, cancellationToken);

        return new DownloadTicketAttachmentResult
        {
            Data = data,
            OriginalName = document.OriginalName,
            Extension = document.Extension
        };
    }
}
