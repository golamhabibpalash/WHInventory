using Domain.Common;

namespace Domain.Entities;

public class NavigationMenuSortOrder : BaseEntity
{
    public string? UserId { get; set; }
    public string? SortOrderJson { get; set; }
}
