# 10 — Users, Roles & Security

🔒 Mostly for administrators. Every user can manage their own profile (10.4).

Menu group: **Membership** (Users, Roles) and **Profiles** (My Profile).

---

## 10.1 How access works

- What a person can see and do is decided by the **roles** granted to their account.
- Each role corresponds to **one area of the menu** — for example `Products`,
  `SalesOrders`, `Customers`, `Users`, `Tenants`. If you do not hold the role for an
  area, that menu item does not appear and its screens are blocked.
- Roles are **not** editable — the list is fixed and matches the menu structure. You
  assign existing roles to users; you do not create new ones.
- The `Profiles` area (My Profile) is available to everyone by default.
- `Tenants` is a **platform** role, only relevant on multi-organisation installations
  (Chapter 11).

There is one level of access per area — holding a role lets the user use that area's
screens (view and change). There is no separate "read-only" grant.

---

## 10.2 Users

### Purpose
Create and manage the people who can sign in to your organisation.

### Where to find it
`Membership ▸ Users`

### Key fields
| Field | Notes |
|-------|-------|
| **First Name**, **Last Name** | The person's name. |
| **Email** | Their sign-in name. Cannot be changed after creation. |
| **Password** / **Confirm Password** | Set only when creating (or via **Change Password** later). |
| **Email Confirmed** | Tick to mark the address as verified so the user can sign in without clicking a confirmation email. |
| **Is Blocked** | Tick to stop the user signing in without deleting the account. |
| **Session timeout (minutes)** | How long this user stays signed in while inactive. |

### How to create a user
1. `Membership ▸ Users` ▸ **Add**.
2. Enter **First Name**, **Last Name**, **Email**.
3. Set a **Password** and **Confirm Password**.
4. Tick **Email Confirmed** so they can log in straight away (otherwise they must
   confirm by email, which needs SMTP configured).
5. Click **Save**.
6. Select the new user and click **Change Role** to grant access (10.3).

### How to reset a user's password
1. Select the user.
2. Click **Change Password**.
3. Enter and confirm the new password. **Save**.
4. Tell the user their new password by a secure channel.

### How to suspend or remove a user
- **Suspend** — select the user, **Edit**, tick **Is Blocked**, **Save**. They can no
  longer sign in; their data and history stay intact. Untick to restore access.
- **Remove** — select the user, **Delete**. This is a soft delete; the account is
  disabled and hidden. Prefer **Is Blocked** for people who might return.

### Rules & tips
- Give each person their own account. Do not share logins — the activity log (11.3)
  attributes actions to the signed-in user.
- The built-in administrator account created with the organisation should be kept safe
  and its password changed from the default immediately.

---

## 10.3 Roles (granting access)

### Purpose
View the fixed list of access areas and assign them to users.

### Where to find it
- The list itself: `Membership ▸ Roles` (view only).
- Assigning: `Membership ▸ Users` ▸ select a user ▸ **Change Role**.

### How to grant or revoke access
1. `Membership ▸ Users`, select the user, click **Change Role**.
2. A list of every area appears with a grant/revoke control per area.
3. Tick the areas the person needs (e.g. a salesperson: `Customers`, `SalesOrders`,
   `SalesReports`, `Products` view, `Warranties`).
4. Use **Grant All** / **Revoke All** for administrators or to start from a clean slate.
5. Save. The user sees the change next time they sign in (or reload).

### Suggested role sets

| Job | Typical areas to grant |
|-----|------------------------|
| Salesperson | Customers, CustomerContacts, SalesOrders, DeliveryOrders, SalesReturns, SalesReports, Warranties, Products, StockReports |
| Buyer | Vendors, VendorContacts, PurchaseOrders, GoodsReceives, PurchaseReturns, PurchaseReports, Products |
| Storekeeper | Warehouses, TransferOuts, TransferIns, PositiveAdjustments, NegativeAdjustments, Scrappings, StockCounts, StockReports, TransactionReports, MovementReports |
| Pricing manager | PricePolicies, ProductPrices, Promotions, PriceReports, Products |
| Administrator | Grant All |

---

## 10.4 My Profile

### Purpose
Every user's own account page.

### Where to find it
`Profiles ▸ My Profile` (also the link on your name, top-left of the sidebar).

### What you can do
| Action | How |
|--------|-----|
| Update your name | **Edit**, change **First Name** / **Last Name**, **Save**. |
| Change your password | **Change Password**, enter your **current password**, then the **new password** twice, **Save**. |
| Change your picture | **Change Avatar**, upload an image, **Save**. It appears in the sidebar. |

### Rules & tips
- You cannot change your own email address or your own roles — ask an administrator.
- If you are locked out, an administrator can reset your password (10.2) or you can use
  **Forgot Password** on the login page (Chapter 02).

---

## 10.5 Sign-in security notes

- Sessions expire after a period of inactivity (per-user, see **Session timeout**). You
  will be asked to sign in again.
- Always **Logout** on shared or public computers.
- Failed sign-ins, sign-outs and key actions are recorded (Chapter 11).
