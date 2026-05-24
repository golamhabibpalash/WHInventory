namespace Domain.Entities;

public class UserActivityLog
{
    public string Id { get; set; } = null!;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? ActivityType { get; set; }
    public string? Description { get; set; }
    public string? PageUrl { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
}
