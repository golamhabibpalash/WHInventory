# uStock — User Manual

This folder contains the complete end-user manual for **uStock**, an inventory and
warehouse management system. It is written for the people who use the software every
day — sales staff, purchasing staff, storekeepers, accountants and administrators —
not for developers.

The manual is deliberately split into short, self-contained chapters (one file each)
so that any team can update the part they own without touching the rest.

---

## How to read this manual

Start with these three chapters in order:

1. [01 — Introduction](01-introduction.md) — what the system is and the ideas behind it.
2. [02 — Getting Started](02-getting-started.md) — signing in and finding your way around.
3. [13 — Everyday Tasks & Conventions](13-everyday-tasks-and-conventions.md) — the buttons,
   lists, filters and document statuses that work the same way on every screen.

After that, jump to whichever chapter matches your job.

## Table of contents

| # | Chapter | For whom |
|---|---------|----------|
| 00 | [About This Manual](00-about-this-manual.md) | Everyone (document control, how to maintain this manual) |
| 01 | [Introduction](01-introduction.md) | Everyone |
| 02 | [Getting Started](02-getting-started.md) | Everyone |
| 03 | [Company & Settings](03-company-and-settings.md) | Administrators |
| 04 | [Products & Catalogue](04-products-and-catalogue.md) | Catalogue / inventory staff |
| 05 | [Customers & Sales](05-customers-and-sales.md) | Sales staff |
| 06 | [Vendors & Purchasing](06-vendors-and-purchasing.md) | Purchasing staff |
| 07 | [Inventory & Stock Movements](07-inventory-and-stock-movements.md) | Warehouse / store staff |
| 08 | [Pricing](08-pricing.md) | Pricing / sales managers |
| 09 | [Reports](09-reports.md) | Managers, accountants |
| 10 | [Users, Roles & Security](10-users-roles-and-security.md) | Administrators |
| 11 | [Administration & Logs](11-administration-and-logs.md) | Administrators |
| 12 | [Utilities — To‑do Lists](12-utilities-todo.md) | Everyone |
| 13 | [Everyday Tasks & Conventions](13-everyday-tasks-and-conventions.md) | Everyone |
| 14 | [Troubleshooting & FAQ](14-troubleshooting-and-faq.md) | Everyone |
| 15 | [Glossary](15-glossary.md) | Everyone |

---

## Editions of this manual

| File | What it is |
|------|------------|
| `00`–`15` chapter files | The **source**. Edit these. |
| `uStock-User-Manual.md` | All chapters joined into one Markdown document (cross-links converted to in-page anchors). Generated — do not hand-edit unless you are not using the chapter files. |
| `uStock-User-Manual.docx` | **Microsoft Word** edition: title page, auto table of contents, page numbers, formatted tables. Generated. |
| `build.py` | Script that regenerates the two combined editions from the chapter files. |

### Regenerating the Word / combined files

After editing any chapter, rebuild the combined editions:

```bash
pip install python-docx      # once
python docs/manual/build.py
```

Then open `uStock-User-Manual.docx` in Word and, if prompted, right-click the table of
contents and choose **Update Field** to refresh page numbers.

To produce a **PDF**, open the `.docx` in Word (or LibreOffice / Google Docs) and
"Save as / Export as PDF".

## How to maintain this manual

These files are plain **Markdown** (`.md`) text files. You can edit them in any text
editor, in Microsoft Word (choose "Keep text only" when pasting), or directly on your
code hosting site.

**When you change the software, update the manual in the same week.** Suggested routine:

1. Edit the relevant chapter file(s).
2. Add one row to the **Revision history** table in
   [00 — About This Manual](00-about-this-manual.md).
3. Increase the version number using the rule in Chapter 00.
4. If you added or removed a chapter, update the table of contents above **and** in
   Chapter 00.

**Style rules for editors** (so the manual stays consistent):

- Plain language. Avoid technical jargon; if a technical term is unavoidable, add it to
  the [Glossary](15-glossary.md).
- Each feature section uses the same five headings:
  **Purpose**, **Where to find it**, **Key fields**, **How to…**, **Rules & tips**.
- Write dates in examples as `DD/MM/YYYY` (the format the screen uses).
- One instruction per numbered step. Start each step with a verb ("Click…", "Choose…").
- Keep screenshots, if you add any, in an `images/` sub‑folder and link to them
  relatively.

---

## Document identification

| Field | Value |
|-------|-------|
| Document title | uStock User Manual |
| Product | uStock — Inventory & Warehouse Management |
| Document type | End-user operating manual |
| Format | Markdown (UTF‑8), one file per chapter |
| Current version | See [00 — About This Manual](00-about-this-manual.md) |
| Language | English |
