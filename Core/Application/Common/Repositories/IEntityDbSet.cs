using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Repositories;


public interface IEntityDbSet
{
    DbSet<Token> Token { get; set; }
    DbSet<Todo> Todo { get; set; }
    DbSet<TodoItem> TodoItem { get; set; }
    DbSet<Company> Company { get; set; }
    DbSet<FileImage> FileImage { get; set; }
    DbSet<FileDocument> FileDocument { get; set; }

    DbSet<NumberSequence> NumberSequence { get; set; }
    DbSet<CustomerGroup> CustomerGroup { get; set; }
    DbSet<CustomerCategory> CustomerCategory { get; set; }
    DbSet<VendorGroup> VendorGroup { get; set; }
    DbSet<VendorCategory> VendorCategory { get; set; }
    DbSet<Warehouse> Warehouse { get; set; }
    DbSet<Customer> Customer { get; set; }
    DbSet<Vendor> Vendor { get; set; }
    DbSet<UnitMeasure> UnitMeasure { get; set; }
    DbSet<ProductGroup> ProductGroup { get; set; }
    DbSet<Product> Product { get; set; }
    DbSet<CustomerContact> CustomerContact { get; set; }
    DbSet<VendorContact> VendorContact { get; set; }
    DbSet<Tax> Tax { get; set; }
    DbSet<SalesOrder> SalesOrder { get; set; }
    DbSet<SalesOrderItem> SalesOrderItem { get; set; }
    DbSet<PurchaseOrder> PurchaseOrder { get; set; }
    DbSet<PurchaseOrderItem> PurchaseOrderItem { get; set; }
    DbSet<InventoryTransaction> InventoryTransaction { get; set; }
    DbSet<DeliveryOrder> DeliveryOrder { get; set; }
    DbSet<GoodsReceive> GoodsReceive { get; set; }
    DbSet<SalesReturn> SalesReturn { get; set; }
    DbSet<PurchaseReturn> PurchaseReturn { get; set; }
    DbSet<TransferIn> TransferIn { get; set; }
    DbSet<TransferOut> TransferOut { get; set; }
    DbSet<StockCount> StockCount { get; set; }
    DbSet<NegativeAdjustment> NegativeAdjustment { get; set; }
    DbSet<PositiveAdjustment> PositiveAdjustment { get; set; }
    DbSet<Scrapping> Scrapping { get; set; }
    DbSet<NavigationMenuSortOrder> NavigationMenuSortOrder { get; set; }
    DbSet<AuditLog> AuditLog { get; set; }
    DbSet<UserActivityLog> UserActivityLog { get; set; }
}

