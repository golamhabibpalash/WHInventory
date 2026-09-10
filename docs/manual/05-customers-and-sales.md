# 05 — Customers & Sales

This chapter follows the sales flow from customer record to delivery, return and
warranty check.

Menu groups: **Sales** (customers, sales orders, warranty), and **Inventory** for
Delivery Order and Sales Return (they change stock, so they live in the Inventory menu —
but they belong to the sales story, so they are documented here).

Generic list / add / edit behaviour: [Chapter 13](13-everyday-tasks-and-conventions.md).

---

## 5.1 Customer Group

### Purpose
Groups customers for pricing and reporting (e.g. *Wholesale*, *Retail*, *Staff*). A
group can be linked to a **Price Policy** so that everyone in the group gets that price
list (Chapter 08).

### Where to find it
`Sales ▸ Customer Group`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Group name. |
| **Description** | Optional. |
| **Price Policy** | Optional. The price list applied to customers in this group. |

### How to add
1. `Sales ▸ Customer Group` ▸ **Add**.
2. Enter **Name**, optionally choose a **Price Policy**, **Save**.

---

## 5.2 Customer Category

### Purpose
A second, independent way to classify customers (e.g. by industry or region). Used for
filtering and reporting. Does not affect pricing.

### Where to find it
`Sales ▸ Customer Category`

### Key fields
**Name**, **Description**.

---

## 5.3 Customer

### Purpose
The master record for each buyer.

### Where to find it
`Sales ▸ Customer`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Customer / company name. Required. |
| **Number** | Internal code. Auto-generated if left blank. |
| **Description** | Free text. |
| **Address** — Street, City, State, Zip Code, Country | Appears on sales documents. |
| **Phone, Fax, Email, Website** | Contact details. |
| **Social** — WhatsApp, LinkedIn, Facebook, Instagram, X/Twitter, TikTok | Optional handles. |
| **Customer Group** | See 5.1. Drives group pricing. |
| **Customer Category** | See 5.2. |

### How to add a customer
1. `Sales ▸ Customer` ▸ **Add**.
2. Fill in **Name** and contact details.
3. Choose a **Customer Group** and **Customer Category** if you use them.
4. **Save**.

### Rules & tips
- Put the customer in the right **Customer Group** if you want them on a special price
  list — the Price Report and pricing engine use it (Chapter 08).

---

## 5.4 Customer Contact

### Purpose
Named people at a customer (buyer, accounts payable, site contact) with their own phone
and email.

### Where to find it
`Sales ▸ Customer Contact`

### Key fields
The contact's **Name**, **Position/Title**, **Phone**, **Email**, and the **Customer**
they belong to.

### How to add
1. `Sales ▸ Customer Contact` ▸ **Add**.
2. Choose the **Customer**, enter the person's details, **Save**.

You can also manage contacts from within the Customer record.

---

## 5.5 Sales Order

### Purpose
Records a customer's order: what they want, how much, at what price and tax. It is the
basis for delivery, payment tracking and sales reporting.

### Where to find it
`Sales ▸ Sales Order`

### Key fields — header
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated (ends `SO`). |
| **Order Date** | Defaults to today. |
| **Customer** | Required. |
| **Tax** | The tax rate for the whole order (Chapter 03). Optional. |
| **Description** | Notes, PO reference, etc. |
| **Before Tax / Tax / After Tax Amount** | Calculated automatically from the lines and the chosen tax. |

### Key fields — line item
| Field | Notes |
|-------|-------|
| **Product** | The item ordered. |
| **Unit Price** | Pre-filled from the product's default price. You can change it, within the product's **Min/Max Selling Price** band (Chapter 04). Outside the band is rejected unless **My Company ▸ Allow price outside band** is on. |
| **Quantity** | How many units. |
| **Total** | Unit Price × Quantity, calculated. |
| **Summary / Remark** | Optional line note. |

### How to create a sales order
1. `Sales ▸ Sales Order` ▸ **Add**.
2. Choose the **Customer**, set the **Order Date**, pick a **Tax** if applicable.
3. **Save** the header. The order opens for line entry.
4. Add each line: choose a **Product**, check/adjust the **Unit Price**, enter the
   **Quantity**. Repeat for every item.
5. Review **Before Tax / Tax / After Tax** totals.
6. Set **Status** to **Confirmed** when the order is agreed.

### How to record a payment against a sales order
1. Open the sales order.
2. Go to its **Payments** section.
3. Click **Add payment**: enter the **Payment Date**, **Amount**, **Payment Method**,
   and an optional **Reference Number** / **Notes**.
4. **Save**. The summary updates: **Document Total**, **Amount Paid**,
   **Amount Outstanding**, and whether it is **Fully Settled**.

You cannot record payments totalling more than the document value. Delete a payment line
to correct a mistake.

### How to check what the customer *should* be charged
Use the **Price Report** (`Pricing ▸ Price Report`, Chapter 08). Enter the customer,
product, quantity and date and it shows the price the rules produce, the cost and the
profit. The Sales Order itself does not apply pricing rules automatically — it starts
from the product's default price and lets you edit within the band.

### Rules & tips
- A Sales Order on its own does **not** move stock. Stock leaves only when you create
  and confirm a **Delivery Order** (5.6).
- Edit lines while the order is **Draft**. After **Confirmed**, re-open to **Draft** to
  change it (if your process allows), or cancel and reissue.
- **Print** produces a customer-facing order document.

---

## 5.6 Delivery Order

### Purpose
Records goods physically leaving your warehouse to fulfil a sales order. **Confirming a
delivery order reduces stock.**

### Where to find it
`Inventory ▸ Delivery Order`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated (ends `DO`). |
| **Delivery Date** | The date goods leave. |
| **Sales Order** | The order being fulfilled. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Notes (driver, vehicle, tracking). |

### How to create a delivery
1. `Inventory ▸ Delivery Order` ▸ **Add**.
2. Choose the **Sales Order** to fulfil and set the **Delivery Date**. Save.
3. The delivery opens with a **line grid** pre-filled from the sales order's physical
   items. For each line, choose the **Warehouse** the goods ship from and check the
   **quantity** (movement). Add or remove lines as needed for a partial delivery.
4. Leave **Status** as **Draft** while you review.
5. Set **Status** to **Confirmed** to release the goods.

On confirmation, uStock creates stock-out ledger lines for each line: from the chosen
warehouse to the *Customer* system warehouse. Non-physical products are ignored.

### Rules & tips
- ⚠️ You must have enough stock of each physical product in the chosen warehouse. If
  not, the confirmation is blocked — receive or transfer stock in first, or reduce the
  quantities.
- Only real (non-system) warehouses can be chosen.
- **Cancelling** a confirmed delivery returns the stock.
- The delivery date also starts the **warranty clock** for warranty products (5.8).
- **Print** produces a delivery note / packing slip.

---

## 5.7 Sales Return

### Purpose
Records goods a customer sends back against a delivery. **Confirming a sales return puts
stock back in.**

### Where to find it
`Inventory ▸ Sales Return`

### Key fields
| Field | Notes |
|-------|-------|
| **Number** | Auto-generated. |
| **Return Date** | When goods came back. |
| **Delivery Order** | The delivery being returned against. Required. |
| **Status** | Draft / Confirmed / Cancelled / Archived. |
| **Description** | Reason for return, condition, etc. |

### How to record a return
1. `Inventory ▸ Sales Return` ▸ **Add**.
2. Choose the **Delivery Order**, set the **Return Date**, describe the reason. Save.
3. In the line grid, for each returned item choose the **Warehouse** the goods come
   back into and enter the returned **quantity**. Remove lines for items not returned.
4. Set **Status** to **Confirmed**.

Stock-in ledger lines are created from the *Customer* system warehouse back into the
chosen warehouse.

### Rules & tips
- Returning damaged goods you cannot resell: bring them in with a Sales Return, then
  write them off with a **Scrapping** (Chapter 07) so the numbers stay honest.
- **Cancelling** a confirmed return reverses the stock-in.

---

## 5.8 Warranty Check

### Purpose
A look-up tool for after-sales / support staff to see whether a delivered item is still
under warranty on a given claim date.

### Where to find it
`Sales ▸ Warranty Check`

### How to use it
1. `Sales ▸ Warranty Check`.
2. Optionally choose a **Customer** and/or a **Product** to narrow the list.
3. Optionally set a **Claim Date** (defaults to today).
4. The results table shows every delivered warranty item with:
   - the **Delivery Order** number and **Delivery Date**,
   - the **Customer** and **Product**,
   - **Warranty Days**, the **Warranty Expiry Date** (delivery date + warranty days),
   - **Days Remaining** and a **Warranty Status** (valid / expired) as of the claim
     date.

### Rules & tips
- Only products with **Warranty applicable** on and **Warranty days** greater than zero
  appear (Chapter 04).
- Only items on **Confirmed** delivery orders are considered.
- The warranty period is counted from the **delivery date**, not the order date.

---

## 5.9 Sales Report

Covered in [Chapter 09 — Reports](09-reports.md). It summarises sales value and quantity
by period, customer and product, with export to Excel.
