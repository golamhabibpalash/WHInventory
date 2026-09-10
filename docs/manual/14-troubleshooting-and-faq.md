# 14 — Troubleshooting & FAQ

## 14.1 Signing in

**I get "invalid email or password".**
Check for typos and Caps Lock. If it still fails, your account may be **blocked** or
your email **not confirmed** — ask an administrator (Chapter 10). Use **Forgot
Password** to be sure of the password.

**I never receive the password-reset or confirmation email.**
Email only works if your organisation has an email (SMTP) server configured. Check spam.
If nothing arrives, an administrator can set your password directly and tick **Email
Confirmed** (Chapter 10, 10.2).

**I'm signed in but a menu group is missing.**
You don't have the **role** for that area. Ask an administrator to grant it
(Chapter 10, 10.3).

**I keep getting logged out.**
Your **Session timeout** is short, or you were inactive. An administrator can raise the
timeout for your account.

**"Sign Up" is not on the login page.**
Open sign-up is switched off for this installation. An administrator must create your
account, or create the organisation from the Tenants screen (Chapter 11).

## 14.2 Saving records

**The form won't save and shows red text.**
A required field is empty or a value is out of range. Fix the highlighted fields.

**I can't edit a document.**
It is probably **Confirmed** or **Cancelled**. Only **Draft** documents are freely
editable. Where your process allows, set it back to **Draft**, edit, then re-confirm
(Chapter 13, 13.6).

**"Not enough stock" when confirming a delivery / transfer / scrapping / negative
adjustment.**
The chosen warehouse does not hold enough of that product **in confirmed stock**.
Options: receive stock (Goods Receive), transfer stock in, reduce the quantity, or
choose the correct warehouse. Check the **Stock Report** (Chapter 09).

**I deleted something by mistake.**
Deletion is a *soft delete* — the record is hidden, not destroyed, but there is no
"undo" button on screen. For master data, re-create it. For a document that had already
affected stock, contact your administrator; the record still exists in the database.

## 14.3 Stock figures look wrong

1. Open `Inventory ▸ Transaction Report` (Chapter 09, 9.4).
2. Filter to the **product** and **warehouse** in question.
3. Sort by **Movement Date** and read down every line.
4. Look for: documents still in **Draft** (no effect yet), **Cancelled** documents
   (effect removed), movements to/from the wrong warehouse, or a missing Goods Receive.
5. Correct with the right movement type (Chapter 07, 7.8) — usually a **Stock Count** to
   snap the figure to a physical count.

**A non-physical (service) product shows no stock.**
That is correct — service products are deliberately excluded from all stock logic
(Chapter 04).

## 14.4 Pricing questions

**The Sales Order line didn't use my price policy / promotion.**
Sales Order lines start from the product's **Unit Price** and are edited manually within
the Min/Max band. The pricing rules feed the **Price Report** (Chapter 08, 8.6), which
tells you what to charge; the order does not apply them automatically.

**A cost-plus or margin price shows as 0.**
Those methods use the **weighted average cost**, which is 0 until the product has been
received via a Goods Receive with a cost (Chapter 08, 8.3).

**A price change was rejected as "outside the band".**
The price is below the product's **Min Selling Price** or above its **Max Selling
Price**. Either change the product's band (Chapter 04) or, if policy allows, an
administrator can tick **Allow price outside band** in **My Company** (Chapter 03).

## 14.5 Printing

**The printed document has the wrong company name / logo / address.**
Update **Settings ▸ My Company** (Chapter 03). The print view reads from there.

**Dates on screen look like month/day.**
uStock uses **DD/MM/YYYY** everywhere. `05/09/2026` means 5 September 2026.

## 14.6 Numbers and references

**Two documents got the same number.**
The **Last Used Count** on that Number Sequence was lowered or reset. An administrator
should set it above the highest number already issued (Chapter 03, 3.3).

**I want my document numbers to include the year.**
Add a **Prefix** like `2026-` to the relevant Number Sequence (Chapter 03, 3.3).

## 14.7 Who to ask

| Problem | Contact |
|---------|---------|
| Access / roles / password | Your organisation's uStock administrator |
| Wrong stock, needs correcting beyond your role | Warehouse supervisor / administrator |
| Pricing rules and bands | Pricing manager / administrator |
| The site is down or very slow, errors on every page | Whoever hosts/operates uStock for you |
| A feature seems broken (not just misunderstood) | Raise it with your administrator to pass to the software team |
