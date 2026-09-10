# 13 — Everyday Tasks & Conventions

Almost every screen in uStock behaves the same way. Learn this once and every module
becomes familiar. The task chapters (03–12) refer back here instead of repeating it.

## 13.1 Working with a list (grid)

Every "…List" screen shows records in a grid with a toolbar above it.

| Action | How |
|--------|-----|
| **Search** | Type in the **Search** box in the toolbar. It matches as you type across the visible columns. |
| **Sort** | Click a column header. Click again to reverse. |
| **Filter** | Open the column menu (small icon in the header) or the filter bar and tick the values you want. |
| **Group** | Drag a column header to the "group by" area to cluster rows (e.g. group products by brand). |
| **Reorder / resize columns** | Drag a header to move it; drag its edge to resize. |
| **Show / hide columns** | Use the column menu ▸ **Columns**. |
| **Page size** | Choose 10 / 20 / 50 / 100 / 200 / All at the bottom. |
| **Export to Excel** | Click **Excel Export** in the toolbar. The current list (with your filters) is downloaded as a spreadsheet. |

Your column layout and sort choices are remembered per screen on the same browser.

## 13.2 Adding a record

1. Click **Add** in the toolbar.
2. Fill in the form. Fields marked as required must be completed.
3. Click **Save** (or **Create**).
4. The new record appears in the list, usually at the top.

## 13.3 Editing a record

1. Click the row to select it (or tick its checkbox).
2. Click **Edit**.
3. Change what you need.
4. Click **Save** (or **Update**).

Some documents can only be edited while they are in **Draft** status (see 13.6).

## 13.4 Deleting a record

1. Select the row.
2. Click **Delete**.
3. Confirm when asked.

⚠️ Deletion in uStock is a **soft delete**: the record is hidden from lists but kept in
the database for audit purposes. It is not shown again and cannot be un-deleted from the
screen. Do not delete documents that have already affected stock — **cancel** them
instead (13.6).

## 13.5 Attaching files to a record

Some records (for example Products) let you attach documents or images — invoices,
spec sheets, photos. Look for an **Attachments** or upload area on the form. Allowed
file types are typically PDF, Word, Excel, PowerPoint and common image formats.

## 13.6 Document status: Draft → Confirmed → Cancelled

Transaction documents (orders, deliveries, receipts, transfers, adjustments, scrapping,
stock counts, returns) carry a **Status**:

| Status | You can edit it? | Effect on stock |
|--------|------------------|-----------------|
| **Draft** | Yes, freely | None — it is only a plan |
| **Confirmed** | No (re-open by setting back to Draft where allowed) | The movement is applied to stock |
| **Cancelled** | No | Any stock effect is removed |
| **Archived** | No | Historical only; keeps lists tidy |

**To make a document take effect:** open it, set **Status** to **Confirmed**, and save.

**To undo a confirmed document:** set its **Status** to **Cancelled** and save. The
ledger lines it created are removed from the stock calculation.

Purchase Orders additionally show progress labels — **Open**, **Partially Received**,
**Fully Received**, **Closed** — driven by how much has been received against them.

## 13.7 Printing a document (PDF)

Documents such as Sales Orders, Purchase Orders, Delivery Orders, Goods Receives,
Transfers, Adjustments, Scrapping, Stock Counts and Returns have a **Print** / **PDF**
option. It opens a clean, printable page using your company name, logo and address from
**Settings ▸ My Company**. Use your browser's print dialog to print or save as PDF.

## 13.8 Dates

- Dates are shown and entered as **DD/MM/YYYY** (e.g. `05/09/2026` = 5 September 2026).
- Date pickers open a calendar; you can also type the date.
- Times and record history are stored in UTC internally but displayed in local format.

## 13.9 Numbers and currency

- The currency symbol/label comes from **Settings ▸ My Company ▸ Currency**.
- Document numbers (SO…, PO…, DO…, GR… etc.) are generated automatically — see
  Chapter 03, *Number Sequence*. You do not type them.

## 13.10 Required master data before you can transact

You will be blocked or slowed down if these do not exist yet:

| To create a… | You first need… |
|--------------|-----------------|
| Sales Order | at least one Customer, one Product, (a Tax rate if you charge tax) |
| Delivery Order | a confirmed/valid Sales Order and at least one real Warehouse with stock |
| Purchase Order | at least one Vendor and one Product |
| Goods Receive | a Purchase Order and a real Warehouse |
| Transfer Out | two real Warehouses and stock in the source |
| Any price rule | a Product (and usually a Price Policy) |

## 13.11 If something will not save

- Look for red validation text under the fields — a required field is empty or a value
  is out of range.
- Check you have the **role** for that area (Chapter 10). Without it the screen will
  not even appear in the menu.
- For stock-reducing documents, check there is enough stock (Chapter 07, 13.6).
- See Chapter 14 for more.
