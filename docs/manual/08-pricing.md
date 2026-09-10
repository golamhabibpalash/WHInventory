# 08 — Pricing

uStock can hold more than one price for a product: a default price, policy-based price
lists (optionally per customer group), quantity discounts and time-limited promotions.
This chapter explains how to set them up and how uStock decides which price applies.

Menu group: **Pricing**.

Generic list / add / edit behaviour: [Chapter 13](13-everyday-tasks-and-conventions.md).

---

## 8.1 The pieces

| Piece | What it is |
|-------|------------|
| **Default price** | The **Unit Price** on the product record (Chapter 04). The fallback when nothing else matches. |
| **Price Policy** | A named price list ("Wholesale 2026", "Staff", "Clearance") with an active period and a priority. |
| **Product Price** | One product's price **within** a policy, defined by a calculation method (fixed, cost-plus, margin, formula). |
| **Quantity Break** | A stepped price inside a Product Price: cheaper per unit above a quantity threshold. |
| **Promotion** | A short-term override for one product: a promo price or a % discount, between two dates. |
| **Customer Group ▸ Price Policy** | Links a group of customers to a policy so they automatically get that price list. |
| **Weighted Average Cost (WAC)** | The running average cost of a product from goods receipts. Used for margin/profit figures and cost-plus pricing. |

---

## 8.2 Price Policy

### Purpose
A container for a set of product prices that applies for a period and can be switched on
or off as a whole.

### Where to find it
`Pricing ▸ Price Policy`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** | Policy name shown in pick-lists. |
| **Code** | Short reference code. |
| **Description** | Optional. |
| **Priority** | Higher number wins when more than one policy could apply. |
| **Active** | Turn the whole policy on/off. |
| **Effective From / Effective To** | The date window the policy is valid. Blank = no limit. |

### How to create a policy
1. `Pricing ▸ Price Policy` ▸ **Add**.
2. Enter **Name**, **Code**, a **Priority**, and the **Effective From/To** dates.
3. Leave **Active** on. **Save**.
4. Add product prices to it (8.3).
5. Optionally link it to a **Customer Group** (Chapter 05, section 5.1).

---

## 8.3 Product Price (and Quantity Breaks)

### Purpose
Sets how one product is priced inside one policy.

### Where to find it
`Pricing ▸ Product Price`

### Key fields
| Field | Notes |
|-------|-------|
| **Product** | The product being priced. |
| **Price Policy** | The policy this price belongs to. |
| **Calculation Method** | How the price is worked out — see the table below. |
| **Fixed Price** | Used when the method is *Fixed Price*. |
| **Markup %** / **Markup Amount** | Used with the cost-plus methods (applied to WAC). |
| **Margin %** | Used with the *Gross Margin %* method. |
| **Formula Multiplier** | Used with the *Formula Based* method (price = WAC × multiplier). |
| **Minimum Selling Price** | A floor — the calculated price will not go below this. |
| **Maximum Discount %** | The most a salesperson may discount from this price. |
| **Priority** | Higher wins if several product prices in the same policy match. |
| **Active** | On/off. |
| **Effective From / Effective To** | Date window for this specific price. |

**Calculation methods:**

| Method | Price is… |
|--------|-----------|
| **Fixed Price** | exactly the **Fixed Price** you enter. |
| **Cost + Markup %** | WAC increased by **Markup %**. |
| **Cost + Markup Amount** | WAC plus a flat **Markup Amount**. |
| **Gross Margin %** | set so that **Margin %** of the selling price is profit. |
| **Formula Based** | WAC multiplied by **Formula Multiplier**. |

### Quantity Breaks
Inside a Product Price you can add **Quantity Break** rows:

| Field | Notes |
|-------|-------|
| **Min Qty** | The break applies at this quantity and above. |
| **Max Qty** | Optional upper bound for this step. |
| **Price** | The per-unit price at this quantity step. |

### How to set a policy price for a product
1. `Pricing ▸ Product Price` ▸ **Add**.
2. Choose the **Product** and the **Price Policy**.
3. Pick a **Calculation Method** and fill in the field it needs.
4. Set a **Minimum Selling Price** and **Maximum Discount %** if you want guard rails.
5. Set the **Effective From/To** and **Priority**. **Save**.
6. To add volume pricing, open the record and add **Quantity Break** rows.

### Rules & tips
- Cost-plus, margin and formula methods depend on **WAC**. A product with no goods
  receipts yet has WAC 0, so those methods give 0 until stock is received.
- Changes to a product's price are recorded in a **price history** log automatically.

---

## 8.4 Promotion

### Purpose
A temporary price for one product, overriding everything else while it runs.

### Where to find it
`Pricing ▸ Promotion`

### Key fields
| Field | Notes |
|-------|-------|
| **Name** / **Code** | Identify the promotion. |
| **Product** | The product on offer. |
| **Price Policy** | Optional — restricts the promotion to one policy. |
| **Promotional Price** | A fixed promo price, **or**… |
| **Discount %** | …a percentage off the default price (use one or the other). |
| **Start Date / End Date** | The promotion's active window. |
| **Priority** | Higher wins if two promotions overlap. |
| **Active** | On/off. |

### How to run a promotion
1. `Pricing ▸ Promotion` ▸ **Add**.
2. Choose the **Product**, set either a **Promotional Price** or a **Discount %**.
3. Set **Start Date** and **End Date**. Leave **Active** on. **Save**.

---

## 8.5 How uStock chooses a price

For a given product, customer, quantity and date, the pricing engine checks in this
order and stops at the first match:

1. **Promotion** — an active promotion for the product on that date
   (highest priority wins). A promo price is used directly; a discount % is taken off
   the product's default price.
2. **Customer Price Group policy** — if the customer belongs to a Customer Group that is
   linked to a Price Policy, and that policy has a Product Price for this product:
   - if a **Quantity Break** matches the quantity, its price is used;
   - otherwise the Product Price's calculated price is used.
3. **Any active Product Price policy** — the highest-priority active Product Price for
   the product on that date (again, Quantity Break first, then calculated price).
4. **Default price** — the product's **Unit Price**.

Each result also reports the **cost (WAC)**, the **profit** and the **profit %**.

⚠️ **On a Sales Order line, the price is *not* filled in from this engine.** The line
starts from the product's **Unit Price** and you may edit it within the product's
Min/Max Selling Price band (Chapter 04 / Chapter 05). Use the **Price Report** to see
what the rules produce, then enter that figure if you want it.

---

## 8.6 Price Report

### Purpose
A one-screen overview of pricing for your products: default price versus cost, the
active policy price, the resulting margin, and whether a promotion is running.

### Where to find it
`Pricing ▸ Price Report`

### How to use it
1. `Pricing ▸ Price Report`.
2. Filter by **Product Group** and/or **Product**.
3. Read the table:

| Column | Meaning |
|--------|---------|
| **Default Price** | The product's Unit Price. |
| **WAC** | Weighted average cost from goods receipts. |
| **Active Policy** / **Policy Price** | The policy currently in effect and its price. |
| **Margin** / **Margin %** | Profit versus cost at the policy price. |
| **Promotion** / **Promo Price** | Whether a promotion is live and at what price. |

4. **Excel Export** to share or analyse.

### Rules & tips
- Use this before a price review, or when a customer queries a quote, to confirm you are
  selling above cost.
- If **WAC** looks wrong, check your goods receipts and their costs (Chapter 06).
