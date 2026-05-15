#!/usr/bin/env python3
"""Generate WHInventory User Manual PDF using ReportLab."""

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, HRFlowable, ListFlowable, ListItem, KeepTogether
)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
from reportlab.pdfgen import canvas
from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate
import datetime

OUTPUT = "/Users/golamhabibpalash/Documents/Dev/Projects/Inventory-Order-Management-System/WHInventory_User_Manual.pdf"

# ── Colour palette ────────────────────────────────────────────────────────────
PRIMARY    = colors.HexColor("#2c3e50")
ACCENT     = colors.HexColor("#2980b9")
LIGHT_BG   = colors.HexColor("#eaf3fb")
HEADER_BG  = colors.HexColor("#2c3e50")
ALT_ROW    = colors.HexColor("#f2f6fb")
WHITE      = colors.white
BORDER     = colors.HexColor("#bdc3c7")
NOTE_BG    = colors.HexColor("#fef9e7")
NOTE_BORDER= colors.HexColor("#f39c12")
TIP_BG     = colors.HexColor("#eafaf1")
TIP_BORDER = colors.HexColor("#27ae60")


# ── Page layout ───────────────────────────────────────────────────────────────
PAGE_W, PAGE_H = A4
MARGIN = 2*cm

def header_footer(canvas_obj, doc):
    canvas_obj.saveState()
    # Top bar
    canvas_obj.setFillColor(PRIMARY)
    canvas_obj.rect(0, PAGE_H - 1.1*cm, PAGE_W, 1.1*cm, fill=1, stroke=0)
    canvas_obj.setFillColor(WHITE)
    canvas_obj.setFont("Helvetica-Bold", 9)
    canvas_obj.drawString(MARGIN, PAGE_H - 0.75*cm, "WHInventory – Warehouse & Inventory Management System")
    canvas_obj.setFont("Helvetica", 9)
    canvas_obj.drawRightString(PAGE_W - MARGIN, PAGE_H - 0.75*cm, "User Manual")
    # Bottom bar
    canvas_obj.setFillColor(PRIMARY)
    canvas_obj.rect(0, 0, PAGE_W, 0.85*cm, fill=1, stroke=0)
    canvas_obj.setFillColor(WHITE)
    canvas_obj.setFont("Helvetica", 8)
    canvas_obj.drawString(MARGIN, 0.28*cm, f"© {datetime.date.today().year} WHInventory  |  Confidential")
    canvas_obj.drawRightString(PAGE_W - MARGIN, 0.28*cm, f"Page {doc.page}")
    canvas_obj.restoreState()


# ── Styles ────────────────────────────────────────────────────────────────────
base = getSampleStyleSheet()

def S(name, **kw):
    """Create a named ParagraphStyle."""
    return ParagraphStyle(name, **kw)

styles = {
    "title": S("Title2",
        fontName="Helvetica-Bold", fontSize=28, textColor=WHITE,
        alignment=TA_CENTER, spaceAfter=6),
    "subtitle": S("Subtitle2",
        fontName="Helvetica", fontSize=14, textColor=colors.HexColor("#bdc3c7"),
        alignment=TA_CENTER, spaceAfter=4),
    "h1": S("H1",
        fontName="Helvetica-Bold", fontSize=18, textColor=PRIMARY,
        spaceBefore=18, spaceAfter=8,
        borderPad=4, borderColor=ACCENT, borderWidth=0,
        leftIndent=0),
    "h2": S("H2",
        fontName="Helvetica-Bold", fontSize=13, textColor=ACCENT,
        spaceBefore=14, spaceAfter=6),
    "h3": S("H3",
        fontName="Helvetica-Bold", fontSize=11, textColor=PRIMARY,
        spaceBefore=10, spaceAfter=4),
    "body": S("Body2",
        fontName="Helvetica", fontSize=10, leading=15,
        textColor=colors.HexColor("#2c3e50"),
        alignment=TA_JUSTIFY, spaceAfter=6),
    "bullet": S("Bullet2",
        fontName="Helvetica", fontSize=10, leading=14,
        textColor=colors.HexColor("#2c3e50"),
        leftIndent=14, bulletIndent=4, spaceAfter=3),
    "note": S("Note2",
        fontName="Helvetica-Oblique", fontSize=9.5, leading=13,
        textColor=colors.HexColor("#7d6608"),
        leftIndent=10, spaceAfter=6),
    "tip": S("Tip2",
        fontName="Helvetica-Oblique", fontSize=9.5, leading=13,
        textColor=colors.HexColor("#1e8449"),
        leftIndent=10, spaceAfter=6),
    "code": S("Code2",
        fontName="Courier", fontSize=9, leading=12,
        textColor=colors.HexColor("#c0392b"),
        leftIndent=10, spaceAfter=4),
    "toc": S("TOC",
        fontName="Helvetica", fontSize=10, leading=16,
        textColor=PRIMARY, spaceAfter=2),
    "toc_h1": S("TOCH1",
        fontName="Helvetica-Bold", fontSize=11, leading=18,
        textColor=PRIMARY, spaceAfter=2),
    "caption": S("Caption",
        fontName="Helvetica-Oblique", fontSize=8.5, leading=12,
        textColor=colors.grey, alignment=TA_CENTER, spaceAfter=8),
}


# ── Helper builders ───────────────────────────────────────────────────────────
def HR():
    return HRFlowable(width="100%", thickness=1, color=BORDER, spaceAfter=8, spaceBefore=4)

def SP(h=6):
    return Spacer(1, h)

def P(text, style="body"):
    return Paragraph(text, styles[style])

def H1(text):
    return Paragraph(f'<a name="{text}"/>{text}', styles["h1"])

def H2(text):
    return Paragraph(text, styles["h2"])

def H3(text):
    return Paragraph(text, styles["h3"])

def NOTE(text):
    data = [[Paragraph("⚠ Note", ParagraphStyle("nh", fontName="Helvetica-Bold", fontSize=9.5, textColor=colors.HexColor("#7d6608"))),
             Paragraph(text, styles["note"])]]
    t = Table(data, colWidths=[1.6*cm, 14.4*cm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0),(-1,-1), NOTE_BG),
        ("BOX",        (0,0),(-1,-1), 1, NOTE_BORDER),
        ("LEFTPADDING",(0,0),(-1,-1), 8),
        ("RIGHTPADDING",(0,0),(-1,-1),8),
        ("TOPPADDING", (0,0),(-1,-1), 6),
        ("BOTTOMPADDING",(0,0),(-1,-1),6),
        ("VALIGN",     (0,0),(-1,-1),"TOP"),
    ]))
    return t

def TIP(text):
    data = [[Paragraph("✓ Tip", ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9.5, textColor=colors.HexColor("#1e8449"))),
             Paragraph(text, styles["tip"])]]
    t = Table(data, colWidths=[1.3*cm, 14.7*cm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0,0),(-1,-1), TIP_BG),
        ("BOX",        (0,0),(-1,-1), 1, TIP_BORDER),
        ("LEFTPADDING",(0,0),(-1,-1), 8),
        ("RIGHTPADDING",(0,0),(-1,-1),8),
        ("TOPPADDING", (0,0),(-1,-1), 6),
        ("BOTTOMPADDING",(0,0),(-1,-1),6),
        ("VALIGN",     (0,0),(-1,-1),"TOP"),
    ]))
    return t

def field_table(rows):
    """Render a two-column Field / Description table."""
    header = [
        Paragraph("<b>Field</b>", ParagraphStyle("fh", fontName="Helvetica-Bold", fontSize=9.5, textColor=WHITE)),
        Paragraph("<b>Description</b>", ParagraphStyle("dh", fontName="Helvetica-Bold", fontSize=9.5, textColor=WHITE)),
    ]
    data = [header]
    for i, (f, d) in enumerate(rows):
        bg = ALT_ROW if i % 2 == 0 else WHITE
        data.append([
            Paragraph(f"<b>{f}</b>", ParagraphStyle("fc", fontName="Helvetica-Bold", fontSize=9.5, textColor=PRIMARY)),
            Paragraph(d, ParagraphStyle("dc", fontName="Helvetica", fontSize=9.5, leading=13, textColor=colors.HexColor("#2c3e50"))),
        ])
    t = Table(data, colWidths=[4.5*cm, 11.5*cm])
    ts = TableStyle([
        ("BACKGROUND",   (0,0), (-1,0),  HEADER_BG),
        ("ALIGN",        (0,0), (-1,-1), "LEFT"),
        ("VALIGN",       (0,0), (-1,-1), "TOP"),
        ("LEFTPADDING",  (0,0), (-1,-1), 8),
        ("RIGHTPADDING", (0,0), (-1,-1), 8),
        ("TOPPADDING",   (0,0), (-1,-1), 6),
        ("BOTTOMPADDING",(0,0), (-1,-1), 6),
        ("ROWBACKGROUNDS",(0,1),(-1,-1), [ALT_ROW, WHITE]),
        ("BOX",          (0,0), (-1,-1), 0.5, BORDER),
        ("INNERGRID",    (0,0), (-1,-1), 0.3, BORDER),
    ])
    t.setStyle(ts)
    return t

def status_table(rows):
    """Render a status list table."""
    header = [
        Paragraph("<b>Status</b>", ParagraphStyle("sh", fontName="Helvetica-Bold", fontSize=9.5, textColor=WHITE)),
        Paragraph("<b>Meaning</b>", ParagraphStyle("mh", fontName="Helvetica-Bold", fontSize=9.5, textColor=WHITE)),
    ]
    data = [header] + [[Paragraph(f"<b>{s}</b>", ParagraphStyle("sc", fontName="Helvetica-Bold", fontSize=9.5, textColor=ACCENT)),
                         Paragraph(m, ParagraphStyle("mc", fontName="Helvetica", fontSize=9.5, leading=13))]
                        for s, m in rows]
    t = Table(data, colWidths=[4*cm, 12*cm])
    t.setStyle(TableStyle([
        ("BACKGROUND",   (0,0),(-1,0),  HEADER_BG),
        ("ALIGN",        (0,0),(-1,-1),"LEFT"),
        ("VALIGN",       (0,0),(-1,-1),"TOP"),
        ("LEFTPADDING",  (0,0),(-1,-1),8),
        ("RIGHTPADDING", (0,0),(-1,-1),8),
        ("TOPPADDING",   (0,0),(-1,-1),6),
        ("BOTTOMPADDING",(0,0),(-1,-1),6),
        ("ROWBACKGROUNDS",(0,1),(-1,-1),[ALT_ROW,WHITE]),
        ("BOX",          (0,0),(-1,-1),0.5,BORDER),
        ("INNERGRID",    (0,0),(-1,-1),0.3,BORDER),
    ]))
    return t

def bullets(items):
    return ListFlowable(
        [ListItem(P(i, "bullet"), leftIndent=14, bulletColor=ACCENT) for i in items],
        bulletType="bullet", bulletColor=ACCENT, leftIndent=14, spaceAfter=4
    )

def cover_table(app_name, version, date_str):
    data = [[Paragraph(app_name, styles["title"])],
            [Paragraph("Warehouse &amp; Inventory Management System", styles["subtitle"])],
            [Spacer(1,12)],
            [Paragraph(f"User Manual  ·  Version {version}  ·  {date_str}", styles["subtitle"])]]
    t = Table(data, colWidths=[PAGE_W - 2*MARGIN])
    t.setStyle(TableStyle([
        ("BACKGROUND",(0,0),(-1,-1), PRIMARY),
        ("LEFTPADDING",(0,0),(-1,-1),24),
        ("RIGHTPADDING",(0,0),(-1,-1),24),
        ("TOPPADDING",(0,0),(-1,-1),32),
        ("BOTTOMPADDING",(0,0),(-1,-1),32),
        ("BOX",(0,0),(-1,-1),3,ACCENT),
    ]))
    return t


# ── Document assembly ─────────────────────────────────────────────────────────
def build():
    doc = SimpleDocTemplate(
        OUTPUT,
        pagesize=A4,
        leftMargin=MARGIN, rightMargin=MARGIN,
        topMargin=MARGIN + 0.6*cm, bottomMargin=MARGIN + 0.4*cm,
        title="WHInventory User Manual",
        author="WHInventory",
        subject="Warehouse & Inventory Management System – User Manual",
    )

    story = []
    today = datetime.date.today().strftime("%B %Y")

    # ═══════════════════════════════════════════════════════════════
    # COVER PAGE
    # ═══════════════════════════════════════════════════════════════
    story += [SP(80), cover_table("WHInventory", "1.0", today), SP(30)]
    story += [P("This document is the official User Manual for the WHInventory platform. "
                "It covers all modules, workflows, and configuration options available to "
                "system users and administrators.", "body")]
    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # TABLE OF CONTENTS (manual)
    # ═══════════════════════════════════════════════════════════════
    story += [H1("Table of Contents"), HR()]

    toc_entries = [
        ("1.", "Getting Started", "3"),
        ("  1.1", "System Requirements", "3"),
        ("  1.2", "Accessing the Application", "3"),
        ("  1.3", "Login & Logout", "3"),
        ("  1.4", "Forgot Password", "4"),
        ("  1.5", "User Registration", "4"),
        ("2.", "Interface Overview", "4"),
        ("  2.1", "Navigation Sidebar", "4"),
        ("  2.2", "Dashboard", "5"),
        ("  2.3", "Drag-and-Drop Navigation Reordering", "5"),
        ("3.", "Sales Module", "6"),
        ("  3.1", "Customer Group", "6"),
        ("  3.2", "Customer Category", "6"),
        ("  3.3", "Customer", "7"),
        ("  3.4", "Customer Contact", "7"),
        ("  3.5", "Sales Order", "7"),
        ("  3.6", "Sales Report", "9"),
        ("4.", "Purchase Module", "9"),
        ("  4.1", "Vendor Group", "9"),
        ("  4.2", "Vendor Category", "9"),
        ("  4.3", "Vendor", "10"),
        ("  4.4", "Vendor Contact", "10"),
        ("  4.5", "Purchase Order", "10"),
        ("  4.6", "Purchase Report", "11"),
        ("5.", "Inventory Module", "12"),
        ("  5.1", "Unit of Measure", "12"),
        ("  5.2", "Product Group", "12"),
        ("  5.3", "Product", "12"),
        ("  5.4", "Warehouse", "13"),
        ("  5.5", "Delivery Order", "13"),
        ("  5.6", "Goods Receive", "14"),
        ("  5.7", "Sales Return", "15"),
        ("  5.8", "Purchase Return", "15"),
        ("  5.9", "Transfer Out", "16"),
        ("  5.10", "Transfer In", "16"),
        ("  5.11", "Positive Adjustment", "17"),
        ("  5.12", "Negative Adjustment", "17"),
        ("  5.13", "Scrapping", "18"),
        ("  5.14", "Stock Count", "18"),
        ("  5.15", "Transaction Report", "19"),
        ("  5.16", "Stock Report", "19"),
        ("  5.17", "Movement Report", "20"),
        ("6.", "Utilities Module", "20"),
        ("  6.1", "Todo", "20"),
        ("  6.2", "Todo Item", "20"),
        ("7.", "Membership Module", "21"),
        ("  7.1", "Users", "21"),
        ("  7.2", "Roles", "21"),
        ("8.", "Settings Module", "22"),
        ("  8.1", "My Company", "22"),
        ("  8.2", "Tax", "22"),
        ("  8.3", "Number Sequence", "22"),
        ("9.", "My Profile", "23"),
        ("10.", "System Administration Notes", "23"),
        ("11.", "Keyboard & Grid Tips", "24"),
        ("12.", "Glossary", "24"),
    ]

    toc_data = []
    for num, title, pg in toc_entries:
        is_main = not num.startswith("  ")
        style = "toc_h1" if is_main else "toc"
        row = [
            Paragraph(f"<b>{num}</b>" if is_main else num, ParagraphStyle(
                "tn", fontName="Helvetica-Bold" if is_main else "Helvetica",
                fontSize=10 if is_main else 9.5, textColor=PRIMARY if is_main else colors.HexColor("#5d6d7e"))),
            Paragraph(f"<b>{title}</b>" if is_main else title, ParagraphStyle(
                "tt", fontName="Helvetica-Bold" if is_main else "Helvetica",
                fontSize=10 if is_main else 9.5, textColor=PRIMARY if is_main else colors.HexColor("#5d6d7e"))),
            Paragraph(pg, ParagraphStyle(
                "tp", fontName="Helvetica", fontSize=9.5,
                textColor=ACCENT, alignment=TA_CENTER)),
        ]
        toc_data.append(row)

    toc_t = Table(toc_data, colWidths=[1.2*cm, 13.3*cm, 1.5*cm])
    toc_t.setStyle(TableStyle([
        ("VALIGN",       (0,0),(-1,-1),"MIDDLE"),
        ("LEFTPADDING",  (0,0),(-1,-1),4),
        ("RIGHTPADDING", (0,0),(-1,-1),4),
        ("TOPPADDING",   (0,0),(-1,-1),3),
        ("BOTTOMPADDING",(0,0),(-1,-1),3),
        ("LINEBELOW",    (0,0),(-1,-1),0.2,colors.HexColor("#d5d8dc")),
    ]))
    story += [toc_t, PageBreak()]

    # ═══════════════════════════════════════════════════════════════
    # 1. GETTING STARTED
    # ═══════════════════════════════════════════════════════════════
    story += [H1("1. Getting Started"), HR()]

    story += [H2("1.1 System Requirements")]
    story += [P("WHInventory is a web-based application. No software installation is required on the user's device. The following are recommended:")]
    story += [field_table([
        ("Browser",   "Google Chrome 110+, Microsoft Edge 110+, or Firefox 110+ (latest version recommended)"),
        ("Display",   "Minimum 1280 × 768 pixels; 1920 × 1080 recommended for full grid visibility"),
        ("Network",   "Stable internet or intranet connection to the application server"),
        ("JavaScript","Must be enabled in the browser"),
    ]), SP()]

    story += [H2("1.2 Accessing the Application")]
    story += [P("Open your web browser and navigate to the server URL provided by your administrator "
                "(e.g., <font color='#2980b9'>http://your-server:5000</font>). "
                "The application serves both the API and the user interface from the same address.")]

    story += [H2("1.3 Login & Logout")]
    story += [P("Navigate to <b>/Accounts/Login</b>. Enter your registered email address and password, then click <b>Login</b>.")]
    story += [field_table([
        ("Email",    "Your registered email address (used as your username)"),
        ("Password", "Case-sensitive password (minimum 6 characters)"),
    ]), SP()]
    story += [NOTE("After 5 failed login attempts your account may be locked for 30 minutes. Contact your administrator if you are locked out.")]
    story += [SP(), P("To <b>log out</b>, click <b>Logout</b> from the <b>Profiles</b> menu or navigate to <b>/Accounts/Logout</b>. "
                       "Your session token is immediately revoked server-side.")]

    story += [H2("1.4 Forgot Password")]
    story += [P("On the Login page click <b>Forgot Password</b>. Enter your email address and submit. "
                "A temporary password will be emailed to you. Use it to log in, then immediately change your password from My Profile.")]
    story += [NOTE("Forgot-password email delivery requires correct SMTP settings configured by your administrator.")]

    story += [H2("1.5 User Registration")]
    story += [P("If self-registration is enabled, click <b>Register</b> on the Login page. "
                "Fill in your first name, last name, email address, and password. "
                "A confirmation email will be sent — click the link in the email to activate your account before logging in.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 2. INTERFACE OVERVIEW
    # ═══════════════════════════════════════════════════════════════
    story += [H1("2. Interface Overview"), HR()]

    story += [H2("2.1 Navigation Sidebar")]
    story += [P("The left sidebar is the primary navigation element. It is divided into <b>modules</b> (collapsible groups). "
                "Click a module header to expand or collapse it. Click any item inside a module to open that page.")]
    story += [P("The modules available depend on the roles assigned to your account by the administrator. "
                "Only pages you have access to are visible.")]
    story += [field_table([
        ("Dashboards",  "Summary charts and KPI cards"),
        ("Sales",       "Customer management and sales order processing"),
        ("Purchase",    "Vendor management and purchase order processing"),
        ("Inventory",   "Products, warehouses, and all stock movement transactions"),
        ("Utilities",   "Task management (Todo list)"),
        ("Membership",  "User and role administration"),
        ("Profiles",    "Personal profile settings"),
        ("Settings",    "Company profile, tax rates, number sequences"),
    ]), SP()]

    story += [H2("2.2 Dashboard")]
    story += [P("The <b>Default Dashboard</b> provides an at-a-glance business overview:")]
    story += [bullets([
        "<b>KPI Cards</b> – Total Sales Orders, Total Purchase Orders, Total Customers, Total Vendors, Total Products, and Current Total Stock.",
        "<b>Sales by Customer Group</b> – Bar chart showing sales quantity broken down by customer group.",
        "<b>Purchase by Vendor Group</b> – Bar chart showing purchase quantity by vendor group.",
        "<b>Sales by Customer Category</b> – Bar chart of sales by customer category.",
        "<b>Purchase by Vendor Category</b> – Bar chart of purchases by vendor category.",
        "<b>Stock by Warehouse</b> – Bar chart of confirmed stock levels per warehouse.",
        "<b>Recent Sales Orders</b> – Grid listing the most recent sales order lines with date, order number, product, and total.",
        "<b>Recent Inventory Transactions</b> – Live ledger of recent stock movements showing warehouse, product, number, and movement quantity.",
    ])]
    story += [SP(), TIP("Click the chart bars or grid rows to drill down. Use the browser back button to return to the dashboard.")]

    story += [H2("2.3 Drag-and-Drop Navigation Reordering")]
    story += [P("You can personalise the order of menu items <b>within each module</b> using drag and drop:")]
    story += [bullets([
        "Hover over a menu item — a drag handle appears.",
        "Click and drag the item up or down within the same module.",
        "Release to drop it in the new position.",
        "The order is saved automatically to the server and restored every time you log in, on any device or browser.",
        "Module headers (Dashboards, Sales, Purchase, etc.) cannot be reordered — only items inside modules.",
    ])]
    story += [SP(), NOTE("If you clear your browser's local storage, the sidebar will temporarily show the default order until the page is refreshed and the saved order is fetched from the server.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # GRID COMMON CONTROLS  (reusable blurb)
    # ═══════════════════════════════════════════════════════════════
    GRID_BLURB = ("Every list page uses an interactive data grid. Common controls include: "
                  "<b>Search</b> (top-right toolbar), column-header sorting (click), "
                  "multi-column <b>filtering</b> (funnel icon on column header), "
                  "<b>grouping</b> (drag a column header into the group area), "
                  "<b>Excel Export</b> (toolbar button), and column resizing (drag column border). "
                  "Use the pager at the bottom to navigate pages.")

    def module_section(number, title, description, subsections):
        """Build a full module section."""
        elems = [H1(f"{number}. {title}"), HR(), P(description)]
        for ss in subsections:
            elems += [H2(ss["title"]), P(ss.get("intro",""))]
            if "fields" in ss:
                elems += [SP(4), field_table(ss["fields"]), SP(4)]
            if "statuses" in ss:
                elems += [H3("Status Values"), status_table(ss["statuses"]), SP(4)]
            if "notes" in ss:
                for n in ss["notes"]:
                    elems += [NOTE(n) if n[0]=="!" else TIP(n[1:]), SP(4)]
            if "bullets" in ss:
                elems += [bullets(ss["bullets"]), SP(4)]
            if ss.get("page_break"):
                elems.append(PageBreak())
        return elems

    # ═══════════════════════════════════════════════════════════════
    # 3. SALES MODULE
    # ═══════════════════════════════════════════════════════════════
    story += [H1("3. Sales Module"), HR()]
    story += [P("The Sales module manages the complete customer-facing order cycle: "
                "from customer master data through to sales order creation and reporting.")]

    story += [H2("3.1 Customer Group")]
    story += [P("Customer Groups allow high-level classification of customers (e.g., Retail, Wholesale, Corporate). "
                "They are used for dashboard analytics and reporting segmentation.")]
    story += [field_table([
        ("Name",        "Unique descriptive name for the group"),
        ("Description", "Optional notes about this group"),
    ]), SP()]
    story += [bullets([
        "Click <b>Add New</b> in the toolbar to open the form.",
        "Fill in the Name and optional Description, then click <b>Save</b>.",
        "To edit an existing group, select its row in the grid and click <b>Edit</b>.",
        "To delete, select the row and click <b>Delete</b>, then confirm.",
    ])]

    story += [H2("3.2 Customer Category")]
    story += [P("Customer Categories provide a second classification dimension (e.g., Regular, VIP, Government). "
                "Used alongside groups for multi-dimensional reporting.")]
    story += [field_table([
        ("Name",        "Unique name for the category"),
        ("Description", "Optional notes"),
    ]), SP()]

    story += [H2("3.3 Customer")]
    story += [P("The Customer master stores all customer details. Each customer must belong to a Customer Group and a Customer Category.")]
    story += [field_table([
        ("Name",              "Full legal name of the customer"),
        ("Number",            "System-generated unique customer number"),
        ("Customer Group",    "Select from the Customer Group list"),
        ("Customer Category", "Select from the Customer Category list"),
        ("Street",            "Street address"),
        ("City",              "City"),
        ("State",             "State / Province"),
        ("Zip Code",          "Postal code"),
        ("Country",           "Country"),
        ("Phone",             "Primary phone number"),
        ("Email",             "Contact email address"),
        ("Website",           "Customer website URL"),
        ("Description",       "Additional notes"),
    ]), SP()]
    story += [TIP("Use the Customer Contacts sub-module to add multiple individual contacts for the same customer.")]

    story += [H2("3.4 Customer Contact")]
    story += [P("Store individual people associated with each customer (e.g., purchasing manager, accounts payable contact).")]
    story += [field_table([
        ("Customer",    "Select the parent customer"),
        ("Name",        "Contact person's full name"),
        ("Phone Number","Direct phone number"),
        ("Email",       "Contact email address"),
        ("Description", "Role or notes"),
    ]), SP()]

    story += [H2("3.5 Sales Order")]
    story += [P("A Sales Order records a customer's request to purchase specific products. "
                "Sales Orders drive downstream Delivery Orders and Sales Returns. "
                "The page shows a master/detail layout: the order header at the top and order lines below.")]
    story += [H3("Header Fields")]
    story += [field_table([
        ("Number",     "Auto-generated order number (e.g., 0001SO20260515). Read-only."),
        ("Order Date", "Date of the sales order"),
        ("Customer",   "Select from the customer list"),
        ("Tax",        "Select the applicable tax rate"),
        ("Status",     "Current processing state of the order"),
        ("Description","Optional notes or reference information"),
    ]), SP()]
    story += [H3("Order Line Fields")]
    story += [field_table([
        ("Product",    "Select from the product catalogue"),
        ("Summary",    "Optional line-level description"),
        ("Unit Price", "Price per unit"),
        ("Quantity",   "Quantity ordered"),
        ("Total",      "Calculated: Unit Price × Quantity (read-only)"),
    ]), SP()]
    story += [H3("Totals")]
    story += [field_table([
        ("Sub Total",   "Sum of all line totals before tax"),
        ("Tax Amount",  "Tax applied (Sub Total × Tax Rate)"),
        ("Total Amount","Sub Total + Tax Amount"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Initial state — order is being prepared. Lines can be added or edited freely."),
        ("Confirmed", "Order confirmed with the customer. Delivery Orders can be created against this order."),
        ("Archived",  "Order is closed and read-only. No further changes allowed."),
    ]), SP()]
    story += [bullets([
        "Click <b>Add New</b> to create a new Sales Order. The form opens with a blank header.",
        "Save the header first — the order lines section appears after the first save.",
        "Add lines by clicking <b>Add</b> in the order lines grid, selecting a product, and entering price and quantity.",
        "Change status to <b>Confirmed</b> when the order is approved.",
        "Click the <b>PDF</b> icon to download a printable Sales Order document.",
        "Excel Export from the toolbar exports the visible grid data.",
    ])]
    story += [SP(), NOTE("!Deleting a Sales Order also removes all associated inventory transaction records and delivery order links. Only delete Draft orders.")]

    story += [H2("3.6 Sales Report")]
    story += [P("The Sales Report presents a line-item view of all sales order items, grouped by order number. "
                "It supports date-range filtering on the Order Date.")]
    story += [field_table([
        ("Order Date From", "Filter start date — only items with Order Date ≥ this value are shown"),
        ("Order Date To",   "Filter end date — only items with Order Date ≤ this value are shown"),
    ]), SP()]
    story += [bullets([
        "Select date range using the date pickers, then click <b>Apply Filter</b>.",
        "Click <b>Clear Filter</b> to remove the date filter and show all records.",
        "The grid shows Customer, Sales Order Number, Order Date, Product Number, Product Name, Unit Price, Quantity, and Total.",
        "A <b>Total</b> aggregate is shown at the bottom of each order group.",
        "Export to Excel via the toolbar for further analysis.",
    ])]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 4. PURCHASE MODULE
    # ═══════════════════════════════════════════════════════════════
    story += [H1("4. Purchase Module"), HR()]
    story += [P("The Purchase module handles the full vendor procurement cycle: "
                "vendor master data, purchase orders, and purchase reporting.")]

    story += [H2("4.1 Vendor Group")]
    story += [P("Classifies vendors at the highest level (e.g., Supplier, Distributor, Service Provider).")]
    story += [field_table([
        ("Name",        "Unique group name"),
        ("Description", "Optional description"),
    ]), SP()]

    story += [H2("4.2 Vendor Category")]
    story += [P("Provides a secondary classification for vendors (e.g., Local, International, Preferred).")]
    story += [field_table([
        ("Name",        "Unique category name"),
        ("Description", "Optional description"),
    ]), SP()]

    story += [H2("4.3 Vendor")]
    story += [P("The Vendor master records all supplier information.")]
    story += [field_table([
        ("Name",            "Full legal name of the vendor"),
        ("Number",          "System-generated vendor number"),
        ("Vendor Group",    "Classification group"),
        ("Vendor Category", "Classification category"),
        ("Street / City / State / Zip / Country", "Full mailing address"),
        ("Phone",           "Contact phone number"),
        ("Email",           "Contact email"),
        ("Website",         "Vendor website URL"),
        ("Description",     "Notes or remarks"),
    ]), SP()]
    story += [TIP("Use Vendor Contacts to add the specific people you deal with at each vendor.")]

    story += [H2("4.4 Vendor Contact")]
    story += [P("Individual contacts at each vendor (e.g., account manager, technical support).")]
    story += [field_table([
        ("Vendor",      "Parent vendor"),
        ("Name",        "Contact person's name"),
        ("Phone Number","Direct phone"),
        ("Email",       "Contact email"),
        ("Description", "Role or notes"),
    ]), SP()]

    story += [H2("4.5 Purchase Order")]
    story += [P("A Purchase Order is issued to a vendor to procure goods. "
                "Once confirmed, a Goods Receive document is created against it when goods arrive.")]
    story += [H3("Header Fields")]
    story += [field_table([
        ("Number",     "Auto-generated (e.g., 0001PO20260515). Read-only."),
        ("Order Date", "Date of the purchase order"),
        ("Vendor",     "Select from the vendor list"),
        ("Tax",        "Applicable tax rate"),
        ("Status",     "Processing state"),
        ("Description","Optional notes"),
    ]), SP()]
    story += [H3("Order Line Fields")]
    story += [field_table([
        ("Product",    "Select product from catalogue"),
        ("Summary",    "Line-level description"),
        ("Unit Price", "Price negotiated with vendor"),
        ("Quantity",   "Quantity to purchase"),
        ("Total",      "Unit Price × Quantity (read-only)"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Order being prepared — editable"),
        ("Confirmed", "Sent to vendor — Goods Receive documents can now be created"),
        ("Archived",  "Closed and read-only"),
    ]), SP()]
    story += [bullets([
        "Create a new PO with <b>Add New</b>; save the header; then add order lines.",
        "Set status to <b>Confirmed</b> when the order is sent to the vendor.",
        "Download a PDF purchase order document using the <b>PDF</b> button.",
        "When goods arrive, create a <b>Goods Receive</b> document referencing this PO.",
    ])]

    story += [H2("4.6 Purchase Report")]
    story += [P("Line-item view of all purchase order items, filterable by Order Date range.")]
    story += [field_table([
        ("Order Date From", "Filter start date"),
        ("Order Date To",   "Filter end date"),
    ]), SP()]
    story += [bullets([
        "Apply/clear the date filter using the buttons above the grid.",
        "Grid shows Vendor, Purchase Order Number, Order Date, Product Number, Product Name, Unit Price, Quantity, and Total.",
        "Group-level totals appear for each order. Export to Excel via toolbar.",
    ])]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 5. INVENTORY MODULE
    # ═══════════════════════════════════════════════════════════════
    story += [H1("5. Inventory Module"), HR()]
    story += [P("The Inventory module is the core of WHInventory. It manages products, warehouses, "
                "and all stock movements. Every transaction posts entries to the central "
                "<b>Inventory Transaction Ledger</b>, which is the single source of truth for stock on hand.")]

    story += [H2("5.1 Unit of Measure")]
    story += [P("Define units used to measure products (e.g., PCS, KG, LTR, BOX, PALLET).")]
    story += [field_table([
        ("Name",        "Unit name (e.g., Pieces, Kilograms)"),
        ("Description", "Optional description"),
    ]), SP()]

    story += [H2("5.2 Product Group")]
    story += [P("High-level product classification used for filtering and reporting (e.g., Electronics, Consumables, Raw Materials).")]
    story += [field_table([
        ("Name",        "Group name"),
        ("Description", "Optional description"),
    ]), SP()]

    story += [H2("5.3 Product")]
    story += [P("The Product catalogue is the master list of all items that can be bought, sold, stored, or transferred.")]
    story += [field_table([
        ("Name",          "Product name"),
        ("Number",        "Auto-generated product number"),
        ("Product Group", "Select from the product group list"),
        ("Unit Measure",  "Select the unit of measure"),
        ("Physical",      "Check if this is a physical item tracked in stock. Un-check for services."),
        ("Description",   "Additional product details"),
    ]), SP()]
    story += [NOTE("!Only products marked as <b>Physical = Yes</b> are included in stock calculations. Service items (Physical = No) can appear on orders but do not affect inventory levels.")]

    story += [H2("5.4 Warehouse")]
    story += [P("Warehouses are physical or virtual locations where stock is stored. "
                "Each inventory transaction moves stock between two warehouses.")]
    story += [field_table([
        ("Name",            "Warehouse name"),
        ("Description",     "Location details or notes"),
        ("System Warehouse","Reserved flag (auto-set). System warehouses (Customer, Vendor, Transfer, Adjustment, StockCount, Scrapping) are used as virtual counterparties in ledger entries and should not be deleted or edited."),
    ]), SP()]
    story += [NOTE("!Do not delete or rename the six system warehouses: Customer, Vendor, Transfer, Adjustment, StockCount, Scrapping. These are required for the inventory transaction engine to work correctly.")]

    story += [H2("5.5 Delivery Order")]
    story += [P("A Delivery Order records the physical shipment of goods from your warehouse to a customer. "
                "It is linked to a Sales Order and posts an <b>Out</b> transaction to the Inventory Ledger.")]
    story += [H3("Header Fields")]
    story += [field_table([
        ("Number",        "Auto-generated (e.g., 0001DO20260515). Read-only."),
        ("Delivery Date", "Date goods were dispatched"),
        ("Sales Order",   "Linked Sales Order (must be Confirmed)"),
        ("Status",        "Processing state"),
        ("Description",   "Optional notes"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Source warehouse — stock is deducted from here"),
        ("Product",   "Product being shipped"),
        ("Movement",  "Quantity dispatched"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared — lines can be edited"),
        ("Confirmed", "Shipment dispatched — stock is deducted from inventory"),
        ("Archived",  "Closed"),
    ]), SP()]
    story += [bullets([
        "A Delivery Order can only be created against a <b>Confirmed</b> Sales Order.",
        "When status is set to <b>Confirmed</b>, the selected warehouse's stock is reduced.",
        "Download a PDF delivery note using the <b>PDF</b> button.",
    ])]

    story += [H2("5.6 Goods Receive")]
    story += [P("A Goods Receive document records goods arriving from a vendor against a Purchase Order. "
                "It posts an <b>In</b> transaction to the Inventory Ledger.")]
    story += [H3("Header Fields")]
    story += [field_table([
        ("Number",         "Auto-generated (e.g., 0001GR20260515). Read-only."),
        ("Receive Date",   "Date goods were received"),
        ("Purchase Order", "Linked Purchase Order (must be Confirmed)"),
        ("Status",         "Processing state"),
        ("Description",    "Notes or delivery reference"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Destination warehouse — stock is added here"),
        ("Product",   "Product received"),
        ("Movement",  "Quantity received"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being verified — lines can be edited"),
        ("Confirmed", "Goods accepted — stock is added to inventory"),
        ("Archived",  "Closed"),
    ]), SP()]
    story += [TIP("You can receive goods partially — create multiple Goods Receive documents against the same Purchase Order for partial deliveries.")]

    story.append(PageBreak())

    story += [H2("5.7 Sales Return")]
    story += [P("Records goods returned by a customer. Linked to a Delivery Order. Posts an <b>In</b> transaction — "
                "stock comes back into the warehouse.")]
    story += [field_table([
        ("Number",          "Auto-generated (e.g., 0001SR20260515). Read-only."),
        ("Return Date",     "Date goods were returned"),
        ("Delivery Order",  "The original Delivery Order being returned against"),
        ("Status",          "Processing state"),
        ("Description",     "Reason for return or notes"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Warehouse where returned goods are received"),
        ("Product",   "Returned product"),
        ("Movement",  "Quantity returned"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Return accepted — stock re-added to warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]

    story += [H2("5.8 Purchase Return")]
    story += [P("Records goods returned to a vendor. Linked to a Goods Receive. Posts an <b>Out</b> transaction — "
                "stock leaves the warehouse back to the vendor.")]
    story += [field_table([
        ("Number",          "Auto-generated (e.g., 0001PR20260515). Read-only."),
        ("Return Date",     "Date goods were returned to vendor"),
        ("Goods Receive",   "The original Goods Receive document"),
        ("Status",          "Processing state"),
        ("Description",     "Reason for return"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Source warehouse — stock is deducted from here"),
        ("Product",   "Product being returned"),
        ("Movement",  "Quantity returned"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Return dispatched — stock removed from warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]

    story += [H2("5.9 Transfer Out")]
    story += [P("Moves stock out of a source warehouse to an intermediate Transfer warehouse. "
                "Must be paired with a corresponding <b>Transfer In</b> at the destination warehouse.")]
    story += [field_table([
        ("Number",        "Auto-generated (e.g., 0001TO20260515). Read-only."),
        ("Release Date",  "Date goods are released from source warehouse"),
        ("Warehouse From","Source warehouse"),
        ("Warehouse To",  "Destination warehouse (Transfer system warehouse used internally)"),
        ("Status",        "Processing state"),
        ("Description",   "Transfer notes or reference"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Product",  "Product being transferred"),
        ("Movement", "Quantity"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Stock removed from source warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]

    story += [H2("5.10 Transfer In")]
    story += [P("Records goods arriving at the destination warehouse. Linked to a Transfer Out. "
                "Posts an <b>In</b> transaction — stock is added to the receiving warehouse.")]
    story += [field_table([
        ("Number",       "Auto-generated (e.g., 0001TI20260515). Read-only."),
        ("Receive Date", "Date goods arrive at destination"),
        ("Transfer Out", "Linked Transfer Out document"),
        ("Status",       "Processing state"),
        ("Description",  "Notes"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Stock added to destination warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]
    story += [NOTE("!Always create a Transfer In after a Transfer Out. Without a Transfer In the goods remain in the virtual Transfer warehouse and will not appear in the destination warehouse's stock.")]

    story.append(PageBreak())

    story += [H2("5.11 Positive Adjustment")]
    story += [P("Increases stock in a warehouse by a specified quantity. Used for corrections, "
                "found stock, or opening balance entries. Posts an <b>In</b> transaction.")]
    story += [field_table([
        ("Number",          "Auto-generated (e.g., 0001PA20260515). Read-only."),
        ("Adjustment Date", "Date of adjustment"),
        ("Status",          "Processing state"),
        ("Description",     "Reason for adjustment"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Warehouse receiving the stock increase"),
        ("Product",   "Product being adjusted"),
        ("Movement",  "Quantity to add — must be positive"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Stock added to warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]

    story += [H2("5.12 Negative Adjustment")]
    story += [P("Decreases stock in a warehouse. Used for write-offs, damages, or correction of excess stock. Posts an <b>Out</b> transaction.")]
    story += [field_table([
        ("Number",          "Auto-generated (e.g., 0001NA20260515). Read-only."),
        ("Adjustment Date", "Date of adjustment"),
        ("Status",          "Processing state"),
        ("Description",     "Reason for adjustment"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Warehouse", "Warehouse losing stock"),
        ("Product",   "Product being adjusted"),
        ("Movement",  "Quantity to subtract — must be positive"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Stock removed from warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]
    story += [NOTE("!The system validates that the movement quantity is greater than zero. Negative adjustment cannot make stock go below zero at the transaction level, but the ledger sum may become negative if multiple transactions are not reconciled.")]

    story += [H2("5.13 Scrapping")]
    story += [P("Records the permanent disposal of defective or end-of-life goods. "
                "Posts an <b>Out</b> transaction from the selected warehouse to the virtual Scrapping warehouse.")]
    story += [field_table([
        ("Number",         "Auto-generated (e.g., 0001SC20260515). Read-only."),
        ("Scrapping Date", "Date of disposal"),
        ("Warehouse",      "Source warehouse"),
        ("Status",         "Processing state"),
        ("Description",    "Reason for scrapping"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Product",  "Product to be scrapped"),
        ("Movement", "Quantity to scrap"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Being prepared"),
        ("Confirmed", "Goods scrapped — stock removed from warehouse"),
        ("Archived",  "Closed"),
    ]), SP()]

    story += [H2("5.14 Stock Count")]
    story += [P("A Stock Count compares the physical count of goods against the system's recorded stock. "
                "The system automatically calculates the delta and posts a correcting In or Out transaction.")]
    story += [field_table([
        ("Number",     "Auto-generated (e.g., 0001CN20260515). Read-only."),
        ("Count Date", "Date of physical count"),
        ("Warehouse",  "Warehouse being counted"),
        ("Status",     "Processing state"),
        ("Description","Notes"),
    ]), SP()]
    story += [H3("Line Fields")]
    story += [field_table([
        ("Product",        "Product being counted"),
        ("System Stock",   "Current stock per the ledger (read-only, auto-calculated)"),
        ("Counted",        "Physical quantity counted"),
        ("Adjustment",     "System Stock − Counted (read-only). Positive = surplus; Negative = shortage"),
    ]), SP()]
    story += [status_table([
        ("Draft",     "Count being entered"),
        ("Confirmed", "Differences posted to ledger — corrections applied to stock"),
        ("Archived",  "Closed"),
    ]), SP()]
    story += [TIP("Run stock counts regularly (monthly or quarterly) to detect discrepancies early. Confirm the count only after all line quantities have been verified.")]

    story.append(PageBreak())

    story += [H2("5.15 Transaction Report")]
    story += [P("A detailed, scrollable view of every entry in the Inventory Transaction Ledger. "
                "Shows the full history of stock movements across all modules.")]
    story += [field_table([
        ("Warehouse",     "Warehouse where movement occurred"),
        ("Product",       "Product moved"),
        ("Movement Date", "Date of the transaction"),
        ("Number",        "Auto-generated transaction number"),
        ("Movement",      "Quantity (always positive)"),
        ("Trans Type",    "In (stock arriving) or Out (stock leaving)"),
        ("Stock",         "Signed net effect: +In / −Out"),
        ("Status",        "Confirmed or Draft"),
        ("Module",        "Source module (e.g., SalesOrder, GoodsReceive)"),
        ("Module Code",   "Internal module identifier"),
        ("Module Number", "Reference number from the source document"),
        ("Warehouse From","Stock origin warehouse"),
        ("Warehouse To",  "Stock destination warehouse"),
    ]), SP()]
    story += [bullets([
        "Use column filters to narrow by product, warehouse, date range, or module.",
        "Group by Module or Warehouse using the grouping panel (drag a column header up).",
        "Export to Excel for external reconciliation.",
    ])]

    story += [H2("5.16 Stock Report")]
    story += [P("Shows the <b>current confirmed stock level</b> per warehouse/product combination. "
                "This is a summary view — only Confirmed transactions are included.")]
    story += [field_table([
        ("Warehouse",      "Warehouse location"),
        ("Product Name",   "Product name"),
        ("Product Number", "Product code"),
        ("Stock",          "Net stock on hand — positive means stock available, negative means oversold"),
        ("Status",         "Always Confirmed (draft transactions excluded)"),
    ]), SP()]
    story += [TIP("Export the Stock Report to Excel before doing a physical count for comparison.")]

    story += [H2("5.17 Movement Report")]
    story += [P("Combines the Transaction Ledger with product and warehouse grouping to produce a "
                "chronological movement log — useful for audits and reconciliation. "
                "Includes all the same columns as the Transaction Report plus a running stock balance.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 6. UTILITIES
    # ═══════════════════════════════════════════════════════════════
    story += [H1("6. Utilities Module"), HR()]

    story += [H2("6.1 Todo")]
    story += [P("The Todo list manages high-level tasks or projects. Each Todo can contain multiple Todo Items.")]
    story += [field_table([
        ("Title",       "Name of the todo or project"),
        ("Description", "Details about the task"),
        ("Status",      "Tracks progress (e.g., Open, Completed)"),
    ]), SP()]

    story += [H2("6.2 Todo Item")]
    story += [P("Individual action items within a Todo. Provides granular task tracking.")]
    story += [field_table([
        ("Todo",        "Parent Todo"),
        ("Title",       "Name of the specific task"),
        ("Description", "Details"),
        ("Status",      "Item progress status"),
    ]), SP()]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 7. MEMBERSHIP
    # ═══════════════════════════════════════════════════════════════
    story += [H1("7. Membership Module"), HR()]
    story += [P("The Membership module is administrator-only. It controls who can access the system and what they can see.")]

    story += [H2("7.1 Users")]
    story += [P("Create and manage application user accounts.")]
    story += [field_table([
        ("First Name",       "User's first name"),
        ("Last Name",        "User's last name"),
        ("Email",            "Login email address — must be unique"),
        ("Password",         "Initial password (minimum 6 characters)"),
        ("Email Confirmed",  "Whether the account can be used without email confirmation"),
        ("Is Blocked",       "Block a user from logging in without deleting their account"),
        ("Is Deleted",       "Soft-delete a user (data is retained)"),
    ]), SP()]
    story += [bullets([
        "Click <b>Add New</b> to create a user. Set Email Confirmed to Yes to skip email verification.",
        "Expand a user row and click <b>Manage Roles</b> to assign module access.",
        "Use <b>Change Password</b> to reset a user's password without knowing the old one.",
        "Blocking a user prevents login without removing their history.",
    ])]
    story += [NOTE("!The default administrator account (admin@root.com) is seeded at startup. Change its password immediately in a production environment.")]

    story += [H2("7.2 Roles")]
    story += [P("Roles in WHInventory correspond directly to navigation modules. "
                "Assigning a role to a user grants them access to all pages within that module.")]
    story += [field_table([
        ("Dashboards",   "Access to the dashboard"),
        ("Sales",        "Customer groups, categories, customers, contacts, sales orders, sales reports"),
        ("Purchase",     "Vendor groups, categories, vendors, contacts, purchase orders, purchase reports"),
        ("Inventory",    "Products, warehouses, all transaction types, all inventory reports"),
        ("Utilities",    "Todo and Todo Item management"),
        ("Membership",   "User and role administration — restrict to administrators only"),
        ("Profiles",     "Own profile management — automatically assigned to all users"),
        ("Settings",     "Company settings, tax rates, number sequences"),
    ]), SP()]
    story += [TIP("A newly registered user receives only the Profiles role by default. An administrator must assign additional roles from the Users page.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 8. SETTINGS
    # ═══════════════════════════════════════════════════════════════
    story += [H1("8. Settings Module"), HR()]

    story += [H2("8.1 My Company")]
    story += [P("Configure your organisation's profile. This information appears on PDF documents.")]
    story += [field_table([
        ("Company Name", "Legal name of the company"),
        ("Currency",     "Currency symbol or code used in reports"),
        ("Address",      "Street, city, state, zip, country"),
        ("Phone",        "Company phone"),
        ("Email",        "Company email"),
        ("Website",      "Company website URL"),
        ("Description",  "Additional company information"),
    ]), SP()]
    story += [NOTE("!There is always exactly one company record. You can update it but not create additional records.")]

    story += [H2("8.2 Tax")]
    story += [P("Define tax rates that can be applied to Sales Orders and Purchase Orders.")]
    story += [field_table([
        ("Name",        "Tax name (e.g., VAT 10%, GST 8%)"),
        ("Rate (%)",    "Percentage rate (e.g., 10 for 10%)"),
        ("Description", "Notes about applicability"),
    ]), SP()]
    story += [TIP("Create a 0% tax rate for orders that are tax-exempt. A tax must always be selected on orders.")]

    story += [H2("8.3 Number Sequence")]
    story += [P("Number Sequences define the auto-generation rules for document numbers. "
                "They are created automatically on first use and can be viewed here.")]
    story += [field_table([
        ("Entity Name",     "The document type (e.g., SalesOrder, GoodsReceive)"),
        ("Prefix",          "Text added at the start of the number"),
        ("Suffix",          "Text added at the end (e.g., SO, PO, DO)"),
        ("Last Used Count", "The last sequence number issued — view-only in normal use"),
    ]), SP()]
    story += [NOTE("!Changing number sequence values manually can cause duplicate document numbers. Edit only under guidance from your system administrator.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 9. MY PROFILE
    # ═══════════════════════════════════════════════════════════════
    story += [H1("9. My Profile"), HR()]
    story += [P("Each user manages their own profile. Access it from the sidebar under <b>Profiles → My Profile</b>.")]
    story += [field_table([
        ("First Name",   "Update your first name"),
        ("Last Name",    "Update your last name"),
        ("Company Name", "Your company affiliation"),
        ("Email",        "Your login email address (read-only)"),
    ]), SP()]
    story += [H2("Change Password")]
    story += [P("From the My Profile page, click <b>Change Password</b>. Enter your current password, then your new password and confirmation.")]
    story += [field_table([
        ("Old Password",     "Your current password"),
        ("New Password",     "Desired new password (minimum 6 characters)"),
        ("Confirm Password", "Repeat the new password to confirm"),
    ]), SP()]
    story += [H2("Profile Avatar")]
    story += [P("Upload a profile picture by clicking on your avatar image. "
                "Supported formats: JPEG, PNG. Maximum file size: 5 MB. "
                "Your avatar appears in the sidebar header on every page.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 10. SYSTEM ADMINISTRATION NOTES
    # ═══════════════════════════════════════════════════════════════
    story += [H1("10. System Administration Notes"), HR()]
    story += [P("The following information is relevant for system administrators managing the WHInventory installation.")]

    story += [H2("Database")]
    story += [bullets([
        "WHInventory uses <b>PostgreSQL</b> by default (also supports SQL Server — configure via <font color='#c0392b'>appsettings.json → DatabaseProvider</font>).",
        "The database schema is auto-created on first startup using <font color='#c0392b'>EnsureCreated()</font>. No migration scripts are required.",
        "The default connection string is in <font color='#c0392b'>appsettings.json → ConnectionStrings → DefaultConnection</font>.",
        "Demo data is seeded on startup when <font color='#c0392b'>IsDemoVersion: true</font> in <font color='#c0392b'>appsettings.json</font>. Set to <b>false</b> in production.",
    ])]

    story += [H2("JWT Authentication")]
    story += [bullets([
        "Tokens expire after 30 minutes by default (configurable in <font color='#c0392b'>appsettings.json → Jwt → ExpireInMinute</font>).",
        "Refresh tokens are valid for 30 days.",
        "Change the <font color='#c0392b'>Jwt → Key</font> value (minimum 32 characters) before going live. The default key is insecure.",
    ])]

    story += [H2("Email (SMTP)")]
    story += [bullets([
        "Configure <font color='#c0392b'>appsettings.json → SmtpSettings</font> with your SMTP provider details.",
        "Gmail users: use an App Password (not your account password) with <font color='#c0392b'>smtp.gmail.com:465</font>.",
        "Email is required for self-registration confirmation and forgot-password flows.",
        "If SMTP is not configured, disable <font color='#c0392b'>RequireConfirmedEmail</font> in the identity settings.",
    ])]

    story += [H2("File Storage")]
    story += [bullets([
        "Product images are stored in <font color='#c0392b'>wwwroot/app_data/images</font> (max 5 MB each).",
        "Attached documents are stored in <font color='#c0392b'>wwwroot/app_data/docs</font> (max 25 MB each).",
        "Application logs are written to <font color='#c0392b'>wwwroot/app_data/logs</font> by Serilog.",
        "Ensure the application has write permissions to these folders.",
    ])]

    story += [H2("Default Admin Account")]
    story += [P("On first startup the system seeds an administrator account:")]
    story += [field_table([
        ("Email",    "admin@root.com (configurable in appsettings.json → AspNetIdentity → DefaultAdmin → Email)"),
        ("Password", "123456 (configurable in aspNetIdentity → DefaultAdmin → Password)"),
    ]), SP()]
    story += [NOTE("!Change the default admin password immediately after your first login in a production environment.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 11. KEYBOARD & GRID TIPS
    # ═══════════════════════════════════════════════════════════════
    story += [H1("11. Keyboard & Grid Tips"), HR()]
    story += [P("The data grids throughout WHInventory are powered by Syncfusion EJ2 Grid. "
                "The following shortcuts and tips apply across all list pages:")]
    story += [field_table([
        ("Ctrl + F / Cmd + F", "Browser find — use the grid's own Search box for better results"),
        ("Click column header", "Sort ascending; click again for descending; third click removes sort"),
        ("Funnel icon (▼)",    "Opens column filter — supports CheckBox, text, and date filters"),
        ("Drag column header → Group Bar", "Group the data by that column"),
        ("Column right edge drag","Resize the column width"),
        ("Excel Export button", "Download all visible (filtered/sorted) data as .xlsx"),
        ("Page size selector",  "Change records per page: 10, 20, 50, 100, 200, or All"),
        ("Show/Hide columns",   "Click the column menu icon (≡) on any column header"),
    ]), SP()]
    story += [TIP("When grouping by order number on Report pages, a Sum aggregate appears at the bottom of each group showing the total amount.")]
    story += [SP(), NOTE("!The 'All' page size option loads every record. On large datasets this may be slow. Use date filters to limit the data set first.")]

    story.append(PageBreak())

    # ═══════════════════════════════════════════════════════════════
    # 12. GLOSSARY
    # ═══════════════════════════════════════════════════════════════
    story += [H1("12. Glossary"), HR()]
    story += [field_table([
        ("Inventory Ledger",     "The central table (InventoryTransaction) that records every stock movement as a signed entry. All stock-on-hand figures are derived from this ledger."),
        ("Module",               "A functional area of the application (e.g., Sales, Inventory). Modules also define role-based access control."),
        ("System Warehouse",     "A virtual warehouse used internally by the inventory engine as a transaction counterparty (Customer, Vendor, Transfer, Adjustment, StockCount, Scrapping). Do not delete."),
        ("Transaction Type",     "Either In (stock entering a warehouse) or Out (stock leaving a warehouse)."),
        ("Draft",                "A document that is still being prepared and has not yet affected stock levels."),
        ("Confirmed",            "A document that has been finalised. Confirmed documents post to the Inventory Ledger and affect stock on hand."),
        ("Archived",             "A read-only, closed document. No further changes are possible."),
        ("Number Sequence",      "The auto-numbering system that generates unique document numbers combining a prefix, sequential counter, date, and suffix."),
        ("UoM / Unit of Measure","The unit used to quantify a product (e.g., PCS, KG, LTR)."),
        ("Stock on Hand",        "The net sum of all Confirmed In transactions minus all Confirmed Out transactions for a specific product in a specific warehouse."),
        ("Transfer Out / In",    "A two-document process to move stock between warehouses. Transfer Out records stock leaving; Transfer In records stock arriving."),
        ("JWT",                  "JSON Web Token — the authentication mechanism. Tokens are short-lived (30 min by default) and refreshed automatically."),
        ("SMTP",                 "Simple Mail Transfer Protocol — the email sending mechanism used for confirmations and password resets."),
        ("Demo Mode",            "When IsDemoVersion=true in configuration, the system seeds example data on startup. Disable in production."),
        ("Soft Delete",          "Records are never physically deleted from the database. The IsDeleted flag is set to true and records are hidden from normal views."),
        ("PDF Export",           "Each transaction module provides a printer-friendly PDF document generated in the browser using html2canvas and jsPDF."),
        ("Drag-and-Drop Nav",    "The ability to reorder sidebar menu items within a module by dragging. The order is saved per user to the database."),
    ]), SP()]

    # ── Footer page ───────────────────────────────────────────────
    story.append(PageBreak())
    story += [SP(60)]
    story += [cover_table("Thank You for Using WHInventory", "", today)]
    story += [SP(20), P("For technical support, contact your system administrator or the development team.", "body")]

    # ── Build ─────────────────────────────────────────────────────
    doc.build(story, onFirstPage=header_footer, onLaterPages=header_footer)
    print(f"PDF saved → {OUTPUT}")

if __name__ == "__main__":
    build()
