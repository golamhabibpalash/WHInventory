# 01 — Introduction

## 1.1 What uStock is

uStock is a web-based system for tracking goods from the moment you buy them until the
moment you deliver them to a customer. It keeps a running, auditable record of how much
of each product is in each warehouse, and why the number changed.

It brings the following areas into one place:

- **Sales** — customers, sales orders, deliveries, returns, warranty look-up.
- **Purchasing** — vendors (suppliers), purchase orders, goods receipts, returns.
- **Inventory** — products, warehouses, transfers between warehouses, stock adjustments,
  scrapping of damaged goods, and physical stock counts.
- **Pricing** — price lists, quantity discounts, promotions and margin rules.
- **Reporting** — stock on hand, movements, and sales / purchase / price analysis.
- **Administration** — company details, taxes, document numbering, users, roles and
  activity logs.

## 1.2 Key ideas you need to know

### Products

A *product* is anything you buy, hold or sell. Products can be:

- **Physical** — real goods that occupy stock and are counted (the default).
- **Non-physical** — services or charges that are sold but never held in stock.
  These are ignored by all stock calculations.

### Warehouses

A *warehouse* is a place stock is held. You create one warehouse per real location
(main store, shop floor, van, etc.).

The system also keeps six **system warehouses** that you never edit or delete. They
stand in for the "outside world" so that every movement has two ends:

| System warehouse | Represents |
|------------------|------------|
| Customer | Goods that have left you towards a customer |
| Vendor | Goods coming in from a supplier |
| Transfer | Goods in transit between two of your warehouses |
| Adjustment | The counterparty for manual stock corrections |
| Stock Count | The counterparty for stock-count corrections |
| Scrapping | The destination for written-off / destroyed goods |

### Stock is a ledger, not a single number

uStock never simply "sets" a stock figure. Instead, **every** change writes one or more
lines to an *inventory transaction* ledger — a positive line where stock arrives and a
negative line where it leaves. The quantity on hand for a product in a warehouse is the
sum of all **confirmed** ledger lines for that product and warehouse.

This is why:

- You can always see *why* a number is what it is (Chapter 09, Transaction Report).
- Nothing is ever silently overwritten.
- A document only affects stock once it is **Confirmed** (see 1.4).

### Documents and their line items

Most day-to-day work is done through *documents*: a Sales Order, a Purchase Order, a
Transfer, a Stock Count, and so on. A document has a **header** (dates, the customer or
vendor, a description) and one or more **line items** (a product and a quantity).

## 1.3 The typical flow of goods

```
        PURCHASING                         SALES
   ┌──────────────────┐            ┌──────────────────────┐
   │  Purchase Order  │            │     Sales Order      │
   └────────┬─────────┘            └──────────┬───────────┘
            │ receive                         │ deliver
            ▼                                 ▼
   ┌──────────────────┐            ┌──────────────────────┐
   │  Goods Receive   │  stock +   │   Delivery Order     │  stock −
   └────────┬─────────┘            └──────────┬───────────┘
            │ (if faulty)                     │ (if returned)
            ▼                                 ▼
   ┌──────────────────┐            ┌──────────────────────┐
   │ Purchase Return  │  stock −   │    Sales Return      │  stock +
   └──────────────────┘            └──────────────────────┘

   INTERNAL MOVEMENTS: Transfer Out → Transfer In, Positive/Negative
   Adjustment, Scrapping, Stock Count
```

## 1.4 Document status lifecycle

Almost every document moves through the same set of statuses:

| Status | Meaning | Effect on stock |
|--------|---------|-----------------|
| **Draft** | Being prepared. Can be freely edited. | None. |
| **Confirmed** | Approved and final. | The stock movement takes effect. |
| **Cancelled** | Voided. | Any stock effect is reversed / removed. |
| **Archived** | Kept for history, hidden from active lists. | None (movement already applied while it was Confirmed, unless later cancelled). |

Purchase Orders have a few extra progress labels (**Open**, **Partially Received**,
**Fully Received**, **Closed**) that describe how much of the order has been received.

⚠️ **Nothing you enter changes stock until the document is Confirmed.** A Draft order is
just a plan.

## 1.5 Multi-organisation (tenancy)

One uStock installation can host several independent organisations ("tenants" or
"workspaces"). Each organisation has its own web address (for example
`acme.your-ustock-domain`), its own users, and completely separate data. You only ever
see your own organisation's records. See Chapter 11 for the platform-level Tenants
screen.

## 1.6 Access is by area

What you can see and do is controlled by **roles**. Each role corresponds to one area of
the menu (for example "Products", "SalesOrders", "Users"). An administrator grants you
the roles for the areas you need. Menu items you have no role for simply do not appear.
See Chapter 10.
