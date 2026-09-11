using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A single entry in the right-edge quick-access shortcut dock (see
/// FrontEnd/Pages/Shared/AdminLTE/__quick-shortcuts.cshtml). Admin-managed per tenant from
/// Settings > Quick Shortcuts, rendered in ascending SortOrder.
/// </summary>
public class QuickShortcut : BaseEntity
{
    public string? Name { get; set; }

    /// <summary>Font Awesome 5 solid class, e.g. "fas fa-file-invoice".</summary>
    public string? Icon { get; set; }

    /// <summary>App-relative page path the shortcut navigates to, e.g. "/PurchaseOrders/PurchaseOrderList".</summary>
    public string? Url { get; set; }

    public int SortOrder { get; set; }
}
