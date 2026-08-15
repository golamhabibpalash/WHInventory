namespace Application.Common.Services.SecurityManager;

public record GetUserListResultDto
{
    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool? EmailConfirmed { get; init; }
    public bool? IsBlocked { get; init; }
    public bool? IsDeleted { get; init; }
    public DateTime? CreatedAt { get; init; }

    /// <summary>Null means the user falls back to the system default.</summary>
    public int? SessionTimeoutMinutes { get; init; }
}

