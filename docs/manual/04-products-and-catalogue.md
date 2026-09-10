# 04 — Products & Catalogue

This chapter covers everything you need to describe *what* you trade and *where* you
keep it: units of measure, product groups, brands, products, and warehouses.

Menu group: **Inventory**.

The general list / add / edit / delete behaviour is in [Chapter 13](13-everyday-tasks-and-conventions.md).

---

## 4.1 Unit Measure

### Purpose
The units you buy, hold and sell in — piece, box, carton, kilogram, litre, metre, hour,
and so on. Every product is assigned one unit.

### Where to find it
`Inventory ▸ Unit Measure`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | The unit label shown on products and documents (e.g. `PCS`, `BOX`, `KG`). |
| **Description** | Optional explanation. |

### How to add a unit
1. Open `Inventory ▸ Unit Measure`.
2. Click **Add**, type a **Name**, click **Save**.

### Rules & tips
- Keep the list short and standardised. Decide whether you sell in `PCS` or `EA` and
  stick to it.
- uStock does not convert between units automatically (e.g. box → pieces). If you need
  both, create separate products or handle the conversion in your process.

---

## 4.2 Product Group

### Purpose
A category tree for classifying products (e.g. *Electronics*, *Cables*, *Stationery*).
Used for filtering, grouping and the Dashboard's "top categories".

### Where to find it
`Inventory ▸ Product Group`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Category name. |
| **Description** | Optional. |

### How to add a group
1. Open `Inventory ▸ Product Group`.
2. Click **Add**, enter a **Name**, click **Save**.

---

## 4.3 Brand

### Purpose
The manufacturer or brand of a product. Used for filtering and reporting.

### Where to find it
`Inventory ▸ Brand`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Brand name. |
| **Description** | Optional. |

---

## 4.4 Product

### Purpose
The master record for each item you trade. Everything else — orders, stock, prices,
warranty — refers back to a product.

### Where to find it
`Inventory ▸ Product`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Product name. Required. |
| **Number** | Your internal code / SKU. Auto-generated if left blank, or type your own. |
| **Description** | Free text. |
| **Unit Measure** | The unit this product is counted in (see 4.1). |
| **Product Group** | Category (see 4.2). |
| **Brand** | Brand (see 4.3). |
| **Barcode** | Scannable code, if you use one. |
| **Image** | A product photo (shown in lists and on some documents). |
| **Unit Price** | The default selling price. Used when no pricing rule applies (see Chapter 08). |
| **Min Selling Price** / **Max Selling Price** | The allowed price band. Salespeople cannot go outside it unless **My Company ▸ Allow price outside band** is on. Leave blank for no limit. |
| **Physical** | **On** = real stock, counted and controlled. **Off** = a service/charge, ignored by all stock logic. |
| **Warranty applicable** | Turn on if this product carries a warranty. |
| **Warranty days** | Length of the warranty in days from the delivery date. Used by *Warranty Check* (Chapter 05). |

### How to add a product
1. Open `Inventory ▸ Product`.
2. Click **Add**.
3. Enter the **Name** and pick a **Unit Measure**, **Product Group** and **Brand**.
4. Set **Unit Price** and, if you enforce limits, **Min/Max Selling Price**.
5. Leave **Physical** on for stocked goods; turn it off for services.
6. If it has a warranty, turn on **Warranty applicable** and set **Warranty days**.
7. Optionally add a **Barcode** and upload an **Image**.
8. Click **Save**.

### How to give a product opening stock
Products start with zero stock. To load what you already hold, create a **Positive
Adjustment** (see [Chapter 07](07-inventory-and-stock-movements.md), section 7.5).

### Rules & tips
- A **non-physical** product will never appear in stock reports, transfers, counts or
  deliveries' stock effect — it is only a billing line.
- The price band (**Min/Max**) is a guard rail, not a price. The actual price comes from
  Chapter 08 rules or the **Unit Price**.
- Deleting a product that has history is discouraged; it is soft-deleted and disappears
  from pick-lists. Prefer to stop using it.

---

## 4.5 Warehouse

### Purpose
A physical location where stock is held. You need at least one real warehouse before you
can receive, deliver, transfer or count stock.

### Where to find it
`Inventory ▸ Warehouse`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Location name (e.g. `Main Store`, `Shop Floor`, `Van 1`). |
| **Description** | Optional (address, responsible person). |

### How to add a warehouse
1. Open `Inventory ▸ Warehouse`.
2. Click **Add**, enter a **Name**, click **Save**.

### Rules & tips
- The six **system warehouses** (Customer, Vendor, Transfer, Adjustment, Stock Count,
  Scrapping) are listed here but marked as system. ⚠️ **Never edit or delete them** —
  they are used internally as the "other side" of every movement.
- Only real (non-system) warehouses can be chosen as the source or destination on your
  documents.
- There is no separate "close warehouse" action; simply stop using one and move its
  stock out with a Transfer.
