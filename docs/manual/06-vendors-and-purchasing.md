# 06 — Vendors & Purchasing

This chapter follows the purchasing flow from supplier record to goods received and
returns.

Menu groups: **Purchase** (vendors, purchase orders), and **Inventory** for Goods
Receive and Purchase Return (they change stock).

Generic list / add / edit behaviour: [Chapter 13](13-everyday-tasks-and-conventions.md).

---

## 6.1 Vendor Group

### Purpose
Groups suppliers for organisation and reporting (e.g. *Local*, *Import*, *Consumables*).

### Where to find it
`Purchase ▸ Vendor Group`

### Key fields
**Name**, **Description**.

---

## 6.2 Vendor Category

### Purpose
A second, independent classification for suppliers (e.g. by material type or region).

### Where to find it
`Purchase ▸ Vendor Category`

### Key fields
**Name**, **Description**.

---

## 6.3 Vendor

### Purpose
The master record for each supplier you buy from.

### Where to find it
`Purchase ▸ Vendor`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Supplier name. Required. |
| **Number** | Internal code. Auto-generated if blank. |
| **Description** | Free text. |
| **Address** — Street, City, State, Zip Code, Country | Appears on purchase documents. |
| **Phone, Fax, Email, Website** | Contact details. |
| **Social** — WhatsApp, LinkedIn, Facebook, Instagram, X/Twitter, TikTok | Optional. |
| **Vendor Group** | See 6.1. |
| **Vendor Category** | See 6.2. |

### How to add a vendor
1. `Purchase ▸ Vendor` ▸ **Add**.
2. Enter **Name** and contact details, choose group/category, **Save**.

---

## 6.4 Vendor Contact

### Purpose
Named people at a supplier (sales rep, accounts, dispatch).

### Where to find it
`Purchase ▸ Vendor Contact`

### Key fields
Contact **Name**, **Position/Title**, **Phone**, **Email**, and the **Vendor** they
belong to.

### How to add
1. `Purchase ▸ Vendor Contact` ▸ **Add**, choose the **Vendor**, enter details, **Save**.

---

## 6.5 Purchase Order

### Purpose
Your formal order to a supplier: what you want to buy, how much, at what price and tax.
It drives goods receipt, payment tracking and purchase reporting.

### Where to find it
`Purchase ▸ Purchase Order`

### Key fields — header
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated (ends `PO`). |
| **Order Date** | Defaults to today. |
| **Vendor** | Required. |
| **Tax** | Tax rate for the order (Chapter 03). Optional. |
| **Description** | Notes, quotation reference, etc. |
| **Before Tax / Tax / After Tax Amount** | Calculated from the lines. |
| **Status** | Draft / Confirmed / Cancelled / Archived, plus progress labels **Open**, **Partially Received**, **Fully Received**, **Closed**. |

### Key fields — line item
| Field | Notes |
|-------|-------|
| **Product** | The item to buy. |
| **Unit Price** | The purchase (cost) price. |
| **Quantity** | How many units ordered. |
| **Total** | Unit Price × Quantity. |
| **Summary / Remark** | Optional line note. |

### How to create a purchase order
1. `Purchase ▸ Purchase Order` ▸ **Add**.
2. Choose the **Vendor**, set the **Order Date**, pick a **Tax** if applicable. Save.
3. Add each line: **Product**, **Unit Price** (cost), **Quantity**.
4. Review the totals.
5. Set **Status** to **Confirmed** to issue the order to the supplier.

### How to record a payment to the supplier
1. Open the purchase order.
2. Go to its **Payments** section.
3. Click **Add payment**: **Payment Date**, **Amount**, **Payment Method**, optional
   **Reference Number** / **Notes**. Save.
4. The summary shows **Document Total**, **Amount Paid**, **Amount Outstanding** and
   whether it is **Fully Settled**. You cannot pay more than the document value.

### Rules & tips
- A Purchase Order does **not** change stock. Stock rises only when you create and
  confirm a **Goods Receive** against it (6.6).
- The progress labels update automatically as you receive goods:
  - **Open** — confirmed, nothing received yet.
  - **Partially Received** — some quantity received.
  - **Fully Received** — every line fully received.
  - **Closed** — manually finished; no more receipts expected.
- **Print** produces a supplier-facing purchase order.

---

## 6.6 Goods Receive

### Purpose
Records goods physically arriving from a supplier against a purchase order.
**Confirming a goods receive increases stock.**

### Where to find it
`Inventory ▸ Goods Receive`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated (ends `GR`). |
| **Receive Date** | The date goods arrived. |
| **Purchase Order** | The order being received. Required. Only orders with quantity still outstanding are offered. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Notes (delivery note number, condition). |
| **Line** — Warehouse, Product, Quantity received | For each item: where it is put away and how many arrived. The remaining-to-receive quantity from the PO is shown to guide you. |

### How to receive goods
1. `Inventory ▸ Goods Receive` ▸ **Add**.
2. Choose the **Purchase Order** and set the **Receive Date**. Save.
3. In the line grid, for each item choose the destination **Warehouse**, the
   **Product** (from the PO's items) and the **quantity** actually received. For a
   part-delivery, enter only what arrived; you can create another Goods Receive later
   for the rest.
4. Set **Status** to **Confirmed**.

Stock-in ledger lines are created from the *Vendor* system warehouse into the chosen
warehouse. The linked purchase order's progress label updates.

### Rules & tips
- You can split one purchase order across several goods receives (partial deliveries).
- **Cancelling** a confirmed goods receive removes the stock it added.
- Received cost feeds the **weighted average cost** used by pricing and profit figures
  (Chapter 08).
- **Print** produces a goods received note (GRN).

---

## 6.7 Purchase Return

### Purpose
Records goods sent back to a supplier (wrong item, damaged, over-supplied).
**Confirming a purchase return decreases stock.**

### Where to find it
`Inventory ▸ Purchase Return`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Return Date** | When goods were sent back. |
| **Goods Receive** | The receipt being returned against. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Reason for return. |
| **Line** — Warehouse, Product, Quantity | Which stock leaves, from where. |

### How to record a purchase return
1. `Inventory ▸ Purchase Return` ▸ **Add**.
2. Choose the **Goods Receive**, set the **Return Date**, describe the reason. Save.
3. In the line grid, choose the **Warehouse** the goods leave from and the returned
   **quantity** per product.
4. Set **Status** to **Confirmed**.

Stock-out ledger lines are created from your warehouse to the *Vendor* system warehouse.

### Rules & tips
- ⚠️ You must still hold the stock you are returning. If it has already been sold or
  moved, the confirmation is blocked.
- **Cancelling** a confirmed purchase return puts the stock back.

---

## 6.8 Purchase Report

Covered in [Chapter 09 — Reports](09-reports.md). It summarises purchase value and
quantity by period, vendor and product, with export to Excel.
