# 02 — Getting Started

## 2.1 What you need

- A web browser (Chrome, Edge, Firefox or Safari, kept reasonably up to date).
- The web address of your organisation's uStock, e.g. `https://acme.your-ustock-domain`.
- A user account (email and password). Either an administrator creates it for you, or —
  if your organisation allows open sign-up — you create the organisation yourself
  (section 2.3).

## 2.2 Signing in

1. Open the uStock web address in your browser.
2. On the **Login** page, type your **email** and **password**.
3. Click **Login**.
4. You arrive at the **Dashboard**.

If you stay signed in, the session is kept for a limited time and then you are asked to
log in again. Use **Logout** (top-right menu) on shared computers.

## 2.3 Creating a new organisation (open sign-up)

This is only available when your installation has open sign-up switched on. It creates a
brand-new, empty organisation with you as its first administrator.

1. On the Login page, click **Sign Up** (or open `/Accounts/SignUp`).
2. Fill in:
   - **Organisation Name** — your company or team name.
   - **Workspace address** — a short code using lowercase letters, digits and hyphens
     (e.g. `acme`). This becomes part of your web address. It cannot contain spaces or
     capitals.
   - **First name**, **Last name**.
   - **Email** — becomes your sign-in name and the administrator contact.
   - **Password** and **Confirm Password**.
3. Click **Sign Up**.
4. If email confirmation is required, open the message sent to your address and click
   the confirmation link before signing in.
5. Sign in at your new workspace address.

A new organisation is created pre-loaded with: your administrator account, a company
record, the six system warehouses, and default payment methods. Everything else
(products, real warehouses, customers, vendors) you add yourself — see Chapter 03.

## 2.4 Being invited to an existing organisation

If an administrator created your account, you will receive your email address and a
password (or a link to set one). Sign in as in section 2.2. If your account needs email
confirmation, use the **Email Confirm** link you were sent.

## 2.5 If you forget your password

1. On the Login page, click **Forgot Password**.
2. Enter your email and submit.
3. Open the reset email and follow the link to choose a new password.
4. Sign in with the new password.

Password reset emails only work if your organisation has email (SMTP) configured. If no
email arrives, ask an administrator to reset your password from the **Users** screen.

## 2.6 The workspace at a glance

After signing in you see three areas:

- **Left menu (sidebar)** — grouped links to every screen you have access to:
  *Dashboards, Sales, Purchase, Inventory, Pricing, Utilities, Membership, Profiles,
  Settings, Logs.* Click a group to expand it. Drag a link up or down within its group
  to set your own preferred order (this is remembered for you).
- **Top bar** — your organisation, quick links, and your account menu (**My Profile**,
  **Logout**).
- **Main area** — the screen you selected. Most screens are a grid (list) with a
  toolbar; adding or editing opens a form.

## 2.7 The Dashboard

`Dashboards ▸ Default`

The Dashboard is your home screen and a quick health check. It shows:

- **Summary cards** — totals and recent movement counts for deliveries, goods receipts,
  and transfers.
- **KPIs** — key figures such as number of products, stock value indicators and a
  **low-stock** count, each with the change versus the previous period.
- **Inventory status** — a breakdown of stock by state.
- **Movement trend** — a chart of stock movements over time.
- **Top categories** — which product groups move the most.
- **Recent activity** — the latest documents created across the system.

Use the **warehouse selector** at the top of the Dashboard to focus every figure on a
single warehouse, or leave it on *all warehouses*.

## 2.8 First-time checklist for a new organisation

Do these in order (details in Chapter 03 and 04):

1. **Settings ▸ My Company** — fill in name, address, currency and logo.
2. **Settings ▸ Tax** — add the tax rates you charge (e.g. VAT 15%).
3. **Settings ▸ Number Sequence** — optional; adjust document number prefixes if the
   defaults do not suit you.
4. **Inventory ▸ Warehouse** — add your real warehouse(s).
5. **Inventory ▸ Unit Measure** — add units (piece, box, kg, litre…).
6. **Inventory ▸ Product Group** and **Brand** — set up your classifications.
7. **Inventory ▸ Product** — add your products.
8. **Membership ▸ Users** and **Roles** — invite colleagues and grant them access.
9. Load opening stock with a **Positive Adjustment** (Chapter 07).

## 2.9 Signing out

Open your account menu (top-right) and click **Logout**, or go to `/Accounts/Logout`.
