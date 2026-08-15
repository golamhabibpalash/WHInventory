using Domain.Common;

namespace Domain.Entities;

public class Tenant : BaseEntity
{
    public string? Name { get; set; }

    /// <summary>
    /// Lowercase host label used to resolve the tenant from the request, e.g. "acme" in acme.ustock.app.
    /// </summary>
    public string? Slug { get; set; }

    public bool IsActive { get; set; } = true;
}
