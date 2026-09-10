# 11 — Administration & Logs

🔒 Administrator screens. Menu group: **Logs**.

---

## 11.1 Tenants (organisations)

### Purpose
The registry of organisations on a multi-organisation uStock installation. Each *tenant*
is a completely separate workspace with its own users, data and web address. This screen
is only meaningful to a **platform administrator**; ordinary organisations do not use it.

### Where to find it
`Logs ▸ Tenants`

### Key fields
| Field | Notes |
|-------|-------|
| **Organisation** (Name) | The organisation's display name. |
| **Address** (Slug) | The short code in its web address (e.g. `acme` → `acme.your-ustock-domain`). Lowercase letters, digits and hyphens only. |
| **Users** | How many user accounts the organisation has. |
| **Status** (Active) | Whether the organisation is enabled. Inactive organisations cannot be used. |
| **Created** | When it was set up. |

### How to add an organisation
1. `Logs ▸ Tenants` ▸ **Add**.
2. Enter the **Organisation** name and a unique **Address** (slug).
3. Leave **Status** active. **Save**.

The new organisation is created with its own administrator account, company record, the
six system warehouses and default payment methods (the same as open sign-up in
Chapter 02).

### How to disable an organisation
1. Select it, **Edit**, set **Status** to inactive, **Save**.
2. Its users can no longer sign in. Set it active again to restore access.

### Rules & tips
- ⚠️ Changing a slug changes the organisation's web address — anyone with the old
  address will not reach it. Communicate the change first.
- Deleting an organisation is a soft delete and hides it; it does not erase its data.
- You only see this screen if you hold the platform **Tenants** role.

---

## 11.2 Audit Log

### Purpose
A tamper-evident record of **data changes** — who created, updated or deleted which
record, and when. Used for investigations, compliance and dispute resolution.

### Where to find it
`Logs ▸ Audit Log`

### Columns
| Column | Meaning |
|--------|---------|
| **Entity Type** | The kind of record changed (e.g. `Product`, `SalesOrder`). |
| **Entity Id** | The specific record's identifier. |
| **Operation** | Created / Updated / Deleted. |
| **User Id** | Who made the change. |
| **IP Address** | Where the request came from. |
| **Timestamp (UTC)** | When it happened (in UTC). |

### How to use it
1. `Logs ▸ Audit Log`.
2. Set the **From** / **To** dates to narrow the period.
3. Filter by **Entity Type**, **Operation** or **User Id**.
4. Select a row and click **View Detail** to see exactly which fields changed, with
   their old and new values.

### Rules & tips
- Timestamps are UTC — allow for your local time zone when correlating with events.
- The audit log is written automatically for every change; it cannot be edited from the
  application.

---

## 11.3 User Activity Log

### Purpose
A record of **user sessions and navigation** — sign-ins, sign-outs, and pages visited.
Complements the Audit Log (which records data changes) by showing what people were doing.

### Where to find it
`Logs ▸ User Activity Log`

### Columns
| Column | Meaning |
|--------|---------|
| **User Email** | Who was active. |
| **Activity Type** | The kind of event (e.g. login, logout, page view). |
| **Description** | A short description of the event. |
| **Page URL** | The screen involved. |
| **IP Address** | Origin of the request. |
| **User Agent** | The browser / device used. |
| **Timestamp (UTC)** | When it happened. |

### How to use it
1. `Logs ▸ User Activity Log`.
2. Set **From** / **To** dates.
3. Filter by **User Email** or **Activity Type** to follow one person or one kind of
   event.

### Rules & tips
- Useful when a user reports "I didn't do that" — check both this log and the Audit Log
  for the same time window.
- Repeated failed sign-ins from an unfamiliar IP address are worth investigating.

---

## 11.4 Housekeeping recommendations

| Task | Suggested frequency |
|------|---------------------|
| Review blocked/departed users (Chapter 10) | Monthly |
| Review role assignments against job needs | Quarterly |
| Skim the Audit Log for unexpected deletions | Monthly |
| Confirm **My Company** details and logo are current | Each accounting period |
| Check number sequences are not near a limit or duplicated | Yearly |
| Confirm a data backup exists (with whoever hosts uStock) | Ongoing |
