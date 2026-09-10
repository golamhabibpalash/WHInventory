# 03 — Company & Settings

🔒 These screens are normally for administrators. Set them up before daily use begins.

Menu group: **Settings** (plus **Inventory** for warehouses and classifications, covered
in Chapter 04).

---

## 3.1 My Company

### Purpose
Holds your organisation's identity: the name, address, contact details, currency and
logo that appear on printed documents (Sales Orders, invoices, delivery notes, etc.) and
on the Dashboard.

### Where to find it
`Settings ▸ My Company`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Legal or trading name. Shown on every printed document. |
| **Description** | Free text (tagline, registration number, etc.). |
| **Currency** | The currency label/symbol used across the system (e.g. `USD`, `৳`, `€`). Set this once, early. |
| **Street, City, State, Zip Code, Country** | Postal address for documents. |
| **Phone, Fax, Email, Website** | Contact block on documents. |
| **Logo** | Upload an image; it appears on printed documents and the sidebar. |
| **Allow price outside band** | If ticked, salespeople may enter a selling price below the product's minimum or above its maximum (see Chapter 08, price band). Leave unticked to enforce the limits. |

### How to update company details
1. Open `Settings ▸ My Company`.
2. Click **Edit**.
3. Change the fields; upload a new **Logo** if needed.
4. Click **Save**.
5. Open any document's **Print** view to confirm the header looks right.

### Rules & tips
- Changing the **Currency** label does not convert existing figures; it only changes the
  label. Decide on it before entering data.
- Keep the logo modest in size (a wide, transparent PNG works best).

---

## 3.2 Tax

### Purpose
Defines the tax rates you apply to sales and purchase documents (e.g. VAT, GST, sales
tax). Each order can reference one tax rate, which is applied to the order's net amount.

### Where to find it
`Settings ▸ Tax`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Label shown when choosing tax on an order (e.g. `VAT 15%`). |
| **Percentage** | The rate as a number (e.g. `15` for 15%). |
| **Description** | Optional note (e.g. legal reference). |

### How to add a tax rate
1. Open `Settings ▸ Tax`.
2. Click **Add**.
3. Enter **Name**, **Percentage** and an optional **Description**.
4. Click **Save**.

### How tax is used on an order
- On a Sales Order or Purchase Order, choose a **Tax** in the header.
- The order shows **Before Tax Amount**, **Tax Amount** and **After Tax Amount**,
  recalculated as you add or change line items.
- Changing the tax rate later does not retroactively change confirmed orders unless you
  edit and re-save them.

### Rules & tips
- Create every rate you need up front. If a rate changes by law, add a **new** tax
  record rather than editing the old one, so historical orders keep their original rate.
- A `0%` rate is fine for tax-exempt sales.

---

## 3.3 Number Sequence

### Purpose
Controls the automatic reference numbers on documents (Sales Order `…SO`, Purchase Order
`…PO`, Delivery Order `…DO`, Goods Receive `…GR`, and so on). uStock generates the next
number for you every time you create a document.

### Where to find it
`Settings ▸ Number Sequence`

### Key fields
| Field | Notes |
|-------|-------|
| **Entity Name** | Which document type the sequence is for (e.g. `SalesOrder`). |
| **Prefix** | Text placed **before** the running number. |
| **Suffix** | Text placed **after** the running number (the defaults use suffixes like `SO`, `PO`, `DO`, `GR`). |
| **Last Used Count** | The last number issued. uStock increases this automatically. |

### How to change a document's numbering
1. Open `Settings ▸ Number Sequence`.
2. Find the row for the document type, or click **Add** to create one.
3. Set the **Prefix** and/or **Suffix** you want (e.g. prefix `2026-`).
4. Click **Save**. New documents of that type use the new pattern from now on.

### Rules & tips
- Sequence rows are created automatically the first time a document type is used, so the
  list may be short until you start transacting.
- ⚠️ Do not lower **Last Used Count** below a number already in use — you could create
  duplicate references.
- Numbers are issued in a way that is safe even when several people create documents at
  the same time.

---

## 3.4 Payment methods

Payment methods (Cash, Bank Transfer, Card, etc.) are seeded automatically for every new
organisation and are used to record payments against orders. If your build exposes a
screen to manage them it will appear under **Settings**; otherwise the seeded set is
used as-is.
