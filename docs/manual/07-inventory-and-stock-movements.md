# 07 — Inventory & Stock Movements

This chapter covers internal stock movements: transfers between your warehouses, manual
corrections, write-offs and physical counts. Deliveries, sales returns, goods receipts
and purchase returns also move stock — they are documented in Chapters 05 and 06.

Menu group: **Inventory**.

Generic list / add / edit behaviour: [Chapter 13](13-everyday-tasks-and-conventions.md).

---

## 7.1 How stock on hand is calculated

- Every movement writes signed lines to the **inventory transaction ledger**: `+` where
  stock arrives, `−` where it leaves.
- **Stock on hand** for a product in a warehouse = the sum of all **Confirmed** ledger
  lines for that product and warehouse.
- Draft documents contribute nothing. Confirming a document adds its lines to the
  calculation; cancelling it removes them.
- Only **physical** products have stock. Non-physical (service) products are ignored
  everywhere in this chapter.
- Each movement has a *system warehouse* as its other end (Customer, Vendor, Transfer,
  Adjustment, Stock Count, Scrapping) so the ledger always balances.

To see the ledger and current balances, use the reports in Chapter 09
(**Transaction Report**, **Stock Report**, **Movement Report**).

---

## 7.2 Transfer Out

### Purpose
Sends stock from one of your warehouses towards another. The goods leave the source
warehouse immediately (on confirmation) and sit "in transit" until a matching
**Transfer In** receives them.

### Where to find it
`Inventory ▸ Transfer Out`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Transfer Release Date** | When goods leave the source. |
| **Warehouse From** | Source (real warehouse). Required. |
| **Warehouse To** | Intended destination (real warehouse). Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Notes (vehicle, reference). |
| **Line** — Product, Quantity | What is being moved. |

### How to send a transfer
1. `Inventory ▸ Transfer Out` ▸ **Add**.
2. Choose **Warehouse From** and **Warehouse To**, set the **Release Date**. Save.
3. Add lines: **Product** and **Quantity** for each item.
4. Set **Status** to **Confirmed**. Stock leaves *Warehouse From* into the *Transfer*
   (in-transit) system warehouse.

### Rules & tips
- ⚠️ The source warehouse must hold enough of each item.
- The destination does **not** receive the stock yet — do a **Transfer In** (7.3).
- **Cancelling** a confirmed Transfer Out returns the stock to the source.

---

## 7.3 Transfer In

### Purpose
Receives the goods from a **Transfer Out** into the destination warehouse, completing
the move.

### Where to find it
`Inventory ▸ Transfer In`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Transfer Receive Date** | When goods arrived at the destination. |
| **Transfer Out** | The outbound transfer being received. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Notes / discrepancies. |
| **Line** — Product, Quantity received | Defaults to the sent quantities; adjust for shortages/damage. |

### How to receive a transfer
1. `Inventory ▸ Transfer In` ▸ **Add**.
2. Choose the **Transfer Out** and set the **Receive Date**. Save.
3. Check the received **quantities** per line.
4. Set **Status** to **Confirmed**. Stock moves from the *Transfer* system warehouse
   into the destination warehouse.

### Rules & tips
- If you receive **less** than was sent, the difference stays "in transit". Resolve it
  with a **Negative Adjustment** (loss) or a corrective transfer.
- **Cancelling** a confirmed Transfer In puts the goods back "in transit".

---

## 7.4 Positive Adjustment

### Purpose
Increases stock for a reason that is not a purchase — opening balances, found stock, a
correction after an error, a supplier bonus not on any PO.

### Where to find it
`Inventory ▸ Positive Adjustment`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Adjustment Date** | Effective date. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | ⚠️ Always state the reason — this is your audit trail. |
| **Line** — Warehouse, Product, Quantity | Where and how much to add. |

### How to add stock
1. `Inventory ▸ Positive Adjustment` ▸ **Add**.
2. Set the **Adjustment Date** and write a clear **Description** (reason).
3. Add lines: **Warehouse**, **Product**, **Quantity** to add.
4. Set **Status** to **Confirmed**. Stock rises in the chosen warehouse (counterparty:
   the *Adjustment* system warehouse).

### Rules & tips
- This is the standard way to **load opening stock** for a new organisation.
- Prefer a **Stock Count** (7.7) when you are correcting to a physically counted figure.

---

## 7.5 Negative Adjustment

### Purpose
Decreases stock for a reason that is not a sale — shrinkage, theft, breakage found
during handling, a correction after an over-count.

### Where to find it
`Inventory ▸ Negative Adjustment`

### Key fields
Same shape as Positive Adjustment: **Number**, **Adjustment Date**, **Status**,
**Description**, and **Line** rows of **Warehouse**, **Product**, **Quantity** to remove.

### How to remove stock
1. `Inventory ▸ Negative Adjustment` ▸ **Add**.
2. Set the date and a clear **Description** (reason).
3. Add lines: **Warehouse**, **Product**, **Quantity** to remove.
4. Set **Status** to **Confirmed**. Stock falls in the chosen warehouse.

### Rules & tips
- ⚠️ You cannot remove more than is on hand.
- If the goods are physically destroyed, use **Scrapping** (7.6) instead so the reason
  is explicit.

---

## 7.6 Scrapping

### Purpose
Writes off stock that is damaged, expired or otherwise unsellable and is being disposed
of. Reduces stock and records it as scrapped (not lost, not sold).

### Where to find it
`Inventory ▸ Scrapping`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Scrapping Date** | Effective date. |
| **Warehouse** | The warehouse the goods are removed from. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Reason / authorisation reference. |
| **Line** — Product, Quantity | What is being scrapped. |

### How to scrap goods
1. `Inventory ▸ Scrapping` ▸ **Add**.
2. Choose the **Warehouse**, set the **Scrapping Date**, record the reason. Save.
3. Add lines: **Product** and **Quantity**.
4. Set **Status** to **Confirmed**. Stock moves from the warehouse to the *Scrapping*
   system warehouse.

### Rules & tips
- Scrapping keeps a permanent record for insurance, audit and waste reporting.
- **Cancelling** a confirmed scrapping returns the stock.
- **Print** produces a scrapping / disposal note.

---

## 7.7 Stock Count

### Purpose
Records a physical count of a warehouse and corrects the system figures to match what
you actually counted.

### Where to find it
`Inventory ▸ Stock Count`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Count Date** | The date of the count. |
| **Warehouse** | The warehouse being counted. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Notes (team, area, sheet reference). |
| **Line** — Product, System Stock, Counted, Adjustment | See below. |

For each product line:

- **System Stock** — what uStock currently believes is on hand (read-only).
- **Counted** — the quantity you actually counted (you type this).
- **Adjustment** — the difference (`Counted − System Stock`), calculated. Positive means
  stock will be added; negative means it will be removed.

### How to run a stock count
1. `Inventory ▸ Stock Count` ▸ **Add**.
2. Choose the **Warehouse** and **Count Date**. Save. Keep it **Draft** while counting.
3. Add a line per product counted and enter the **Counted** quantity. The
   **Adjustment** column shows the variance.
4. When counting is complete and checked, set **Status** to **Confirmed**. uStock posts
   the variance for every line so that stock on hand now equals your **Counted**
   figures (counterparty: the *Stock Count* system warehouse).

### Rules & tips
- Freeze movements in that warehouse while counting, or count outside working hours, so
  the **System Stock** shown does not drift mid-count.
- Only products you add lines for are affected. A product with no line is left
  unchanged.
- **Cancelling** a confirmed stock count reverses all its corrections.
- **Print** produces a count sheet.

---

## 7.8 Choosing the right movement

| Situation | Use |
|-----------|-----|
| First-time / opening stock | Positive Adjustment (7.4) |
| Bought from a supplier | Goods Receive (Chapter 06) |
| Sold and shipped to a customer | Delivery Order (Chapter 05) |
| Customer sent goods back | Sales Return (Chapter 05) |
| Sending goods back to a supplier | Purchase Return (Chapter 06) |
| Moving goods between your warehouses | Transfer Out + Transfer In (7.2, 7.3) |
| Damaged / expired goods being disposed of | Scrapping (7.6) |
| Shrinkage / theft / unexplained loss | Negative Adjustment (7.5) |
| Found extra stock, no paperwork | Positive Adjustment (7.4) |
| Correcting to a physical count | Stock Count (7.7) |
