# 09 — Reports

uStock has six reporting screens. All of them share the grid features from
[Chapter 13](13-everyday-tasks-and-conventions.md): search, filter, sort, group, choose
columns, and **Excel Export**.

| Report | Menu | Answers |
|--------|------|---------|
| Sales Report | `Sales ▸ Sales Report` | What did we sell, to whom, for how much? |
| Purchase Report | `Purchase ▸ Purchase Report` | What did we buy, from whom, for how much? |
| Stock Report | `Inventory ▸ Stock Report` | How much of each product is in each warehouse now? |
| Transaction Report | `Inventory ▸ Transaction Report` | Every stock movement line, with its source document. |
| Movement Report | `Inventory ▸ Movement Reports` | Net movement per product/warehouse, by movement type (pivot). |
| Price Report | `Pricing ▸ Price Report` | Price vs cost vs margin per product — see [Chapter 08](08-pricing.md). |

---

## 9.1 Sales Report

### Purpose
A line-by-line list of sales order items in a date range.

### How to use it
1. `Sales ▸ Sales Report`.
2. Set **From Date** and **To Date**.
3. The grid lists one row per sold line:

| Column | Meaning |
|--------|---------|
| **Customer** | Who bought it. |
| **Sales Order** | The order number. |
| **Order Date** | When it was ordered. |
| **Product Number / Product Name** | The item. |
| **Unit Price** | Price charged per unit. |
| **Quantity** | Units sold. |
| **Total** | Line value. |

4. **Group** by *Customer* or *Product Name* to see subtotals, or **Excel Export** for
   pivots in a spreadsheet.

### Rules & tips
- The report is based on sales orders. Whether it counts drafts or only confirmed orders
  depends on your configuration — check with your administrator if the totals look high.

---

## 9.2 Purchase Report

### Purpose
The buying equivalent of the Sales Report: one row per purchase order line in a date
range.

### How to use it
1. `Purchase ▸ Purchase Report`.
2. Set **From Date** and **To Date**.
3. Columns: **Vendor**, **Purchase Order**, **Order Date**, **Product Number / Name**,
   **Unit Price**, **Quantity**, **Total**.
4. Group by **Vendor** or **Product Name**; **Excel Export** as needed.

---

## 9.3 Stock Report

### Purpose
Current stock on hand, per product per warehouse.

### How to use it
1. `Inventory ▸ Stock Report`.
2. Columns: **Warehouse**, **Product Name**, **Product Number**, **Stock**, **Status**.
3. **Filter** by warehouse to see one location; **group** by warehouse or product.
4. **Excel Export** for a stock-take working list or a valuation.

### Rules & tips
- Only **confirmed** movements are reflected. A goods receipt still in Draft will not
  show here yet.
- A negative figure means more has gone out than came in for that product/warehouse —
  investigate with the Transaction Report.
- System warehouses may appear; focus on your real warehouses.

---

## 9.4 Transaction Report

### Purpose
The full audit trail: every ledger line, newest activity included, with a link back to
the document that created it.

### How to use it
1. `Inventory ▸ Transaction Report`.
2. Key columns:

| Column | Meaning |
|--------|---------|
| **Warehouse** | Where the movement hit. |
| **Product** | The item. |
| **Movement Date** | Effective date. |
| **Number** | The ledger line reference. |
| **Movement** | Quantity moved on this line. |
| **Trans Type** | **In** or **Out**. |
| **Stock** | The signed effect on stock (`+` in, `−` out). |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Module** / **Module Code** / **Module Number** | Which document type and which document (e.g. `DeliveryOrder` / `DO00021`). |
| **Warehouse From / Warehouse To** | The two ends of the movement (one is usually a system warehouse). |

3. **Filter** by product, warehouse, module or status to trace a discrepancy.

### Rules & tips
- This is the screen to open when a stock figure looks wrong: filter to the product and
  warehouse, sort by **Movement Date**, and read down the movements.
- Lines with status **Cancelled** are excluded from stock but kept for history.

---

## 9.5 Movement Report

### Purpose
A **pivot table** summarising net movement by product and warehouse, split across the
movement types (delivery, goods receive, transfer in/out, adjustments, scrapping, stock
count).

### How to use it
1. `Inventory ▸ Movement Reports`.
2. Read the pivot: **rows** are Product then Warehouse; **columns** are the movement
   type; **values** are the net quantity moved.
3. Use the pivot toolbar to expand/collapse or switch the layout.

### Rules & tips
- Good for spotting, at a glance, which products are churning through a warehouse and by
  what mechanism (e.g. lots of adjustments = a data-quality problem).

---

## 9.6 Exporting and sharing

- Every report has **Excel Export** in the toolbar. It exports what you currently see,
  including your filters and grouping.
- For a printable page of a single **document** (not a report), use its **Print / PDF**
  option instead (Chapter 13, section 13.7).
