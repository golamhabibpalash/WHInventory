# Premium UI Design & Frontend Engineering Standard

> This is the binding standard for all UI work in this repository. A UI task is not
> "done" until it meets Section 24. Read Section 22 before touching any markup or CSS.
>
> **How it maps to this codebase** (AdminLTE 3 + Bootstrap 4 base, Syncfusion EJ2
> `bootstrap5` theme, Vue 3 per page):
> - **Design tokens that exist**: `--primary: #1b84ff` (from `FrontEnd/Pages/Shared/AdminLTE/__css.cshtml`),
>   border `#dee2e6`, muted text `#6c757d`, subtle surface `#f8f9fa`, control border `#ced4da`,
>   radius `.25rem`, control height `calc(1.5em + .75rem + 2px)`, control font `.9rem`
>   (from the global `form .form-control` rule in the same file). Use these — do not invent values.
> - **Reusable components**: Bootstrap `.card` / `.card-header` / `.card-body` (already
>   re-styled by `__css.cshtml`), `.form-card*`, Bootstrap modals, SweetAlert2 dialogs,
>   Syncfusion Grid / DropDownList / DatePicker. Prefer these over bespoke markup.
> - **Icons**: Font Awesome 5 solid (`fas fa-*`) only. No emoji, no Bootstrap Icons mix in new work.
> - **Page-level CSS**: put shared rules in a real stylesheet under `wwwroot/css/` and
>   pull it in with `@section styles { <link rel="stylesheet" href="~/css/<file>.css" asp-append-version="true" /> }`.
>   Do not paste `<style>` blocks into `.cshtml`, and never duplicate the same block across pages.

---

## 1. Objective

All UI must have a polished, premium, production-grade SaaS appearance.

Do not interpret "professional UI" as simply adding colors, shadows, gradients, or rounded corners.

The quality must come primarily from:

* Typography
* Visual hierarchy
* Consistent spacing
* Alignment
* Component proportions
* Icon consistency
* Whitespace
* Information density
* Interaction states
* Responsive behavior
* Consistent design tokens

The UI should feel intentionally designed rather than generated or assembled from default components.

---

## 2. Design Philosophy

Follow these principles:

1. Minimal
2. Clean
3. Spacious
4. Consistent
5. Information-focused
6. Visually balanced
7. Accessible
8. Responsive
9. Predictable
10. Premium SaaS quality

Avoid unnecessary decoration.

Do not use visual effects merely because they are available.

Every spacing, font size, icon, border, shadow, and color should have a reason.

---

## 3. Design Tokens

Never randomly choose values throughout the UI.

Use centralized design tokens.

### Spacing Scale

Prefer a consistent 4px-based spacing system.

Recommended values:

* 4px — micro spacing
* 8px — compact spacing
* 12px — small spacing
* 16px — standard spacing
* 20px — medium spacing
* 24px — section/component spacing
* 32px — large spacing
* 40px — major spacing
* 48px — page-level spacing
* 64px — major section separation

Do not introduce arbitrary values such as 13px, 17px, 23px, 29px unless technically necessary.

---

## 4. Page Layout

Every page should have a clear visual hierarchy:

Page
→ Header
→ Page title / description
→ Primary actions
→ Main content
→ Supporting content

Use a consistent maximum content width.

Avoid content touching the viewport edges.

Recommended page horizontal padding:

* Desktop: 24–32px
* Tablet: 20–24px
* Mobile: 16px

Major sections should have enough whitespace to visually separate them.

Do not unnecessarily fill empty space.

Whitespace is part of the design.

---

## 5. Typography

Typography must create a clear hierarchy.

Use a consistent type scale.

Recommended baseline:

* Page title: 28–32px
* Section title: 20–24px
* Card title: 16–18px
* Body: 14–16px
* Secondary text: 13–14px
* Caption/helper text: 12–13px

Use font weight intentionally:

* Regular: 400
* Medium: 500
* Semibold: 600
* Bold: 700

Avoid excessive use of bold text.

Do not use many different font sizes on the same page.

Line height must be comfortable and readable.

Typical body line-height:

1.4–1.6

Headings should use tighter line-height.

---

## 6. Icons

Icons must be visually consistent.

Do not mix:

* Outline icons
* Filled icons
* Different icon libraries
* Different visual weights

unless there is a deliberate design reason.

Use one primary icon system throughout the application.

Recommended icon sizes:

* Small: 14–16px
* Standard: 18–20px
* Large: 24px
* Feature/icon container: 32–48px

Icons must be vertically and horizontally aligned with their text.

Do not use emojis as UI icons.

Do not use oversized icons for ordinary actions.

Icon buttons must have appropriate clickable areas.

Minimum interactive target should generally be around 40px.

---

## 7. Buttons

Buttons must have consistent:

* Height
* Padding
* Border radius
* Font size
* Font weight
* Icon spacing
* Hover state
* Focus state
* Disabled state
* Loading state

Recommended button heights:

* Small: 32px
* Medium: 36–40px
* Large: 44–48px

Use one primary button style.

Secondary and destructive actions should be visually distinguishable.

Do not make every button visually prominent.

Primary actions should have the strongest visual emphasis.

---

## 8. Form Controls

Inputs, selects, textareas, date pickers and other controls must use consistent dimensions.

Recommended:

* Height: 40–44px
* Horizontal padding: 12–14px
* Border radius: consistent with the application
* Label spacing: 6–8px
* Field spacing: 16–20px

Labels must be clearly associated with their controls.

Validation messages should appear close to the relevant field.

Avoid unnecessarily tall forms.

---

## 9. Cards

Cards should have clear internal hierarchy.

A card should normally contain:

* Header
* Content
* Optional actions/footer

Use consistent:

* Padding
* Border
* Radius
* Shadow
* Header spacing

Recommended card padding:

* Compact: 16px
* Standard: 20px
* Spacious: 24px

Avoid excessive shadows.

Prefer subtle borders and very soft elevation.

Do not make every section look like a floating card.

---

## 10. Tables

Tables must prioritize readability and information density.

Use:

* Clear column hierarchy
* Consistent row height
* Comfortable cell padding
* Proper numeric alignment
* Consistent action placement
* Sticky headers where appropriate
* Hover states when useful

Avoid unnecessarily large row heights.

Recommended table cell vertical padding:

8–12px.

Actions should not visually dominate the data.

---

## 11. Whitespace

Whitespace is intentional.

Do not compress components just because there is available space.

Do not create huge empty areas without purpose either.

Maintain consistent spacing between:

* Page sections
* Headings and descriptions
* Labels and inputs
* Cards
* Buttons
* Icons and text
* Table elements

If two elements belong together, keep them close.

If they represent different concepts, increase the separation.

Use spacing to communicate hierarchy.

---

## 12. Alignment

Alignment is critical to premium UI quality.

Everything should align to a predictable grid.

Pay special attention to:

* Page edges
* Card edges
* Headings
* Buttons
* Form controls
* Table columns
* Icons
* Text baselines

Do not visually center elements simply because flexbox makes it easy.

Use alignment based on the content hierarchy.

---

## 13. Color

Use a controlled color palette.

Define:

* Primary
* Secondary
* Background
* Surface
* Border
* Text
* Muted text
* Success
* Warning
* Error
* Info

Do not introduce new colors directly inside components.

Use semantic color tokens.

Avoid excessive gradients.

Avoid excessive saturated colors.

Status colors should communicate meaning consistently across the application.

---

## 14. Borders & Radius

Use a consistent radius scale.

Example:

* Small: 4px
* Medium: 6–8px
* Large: 10–12px
* Pill: 9999px

Do not mix many unrelated radius values.

Borders should generally be subtle.

---

## 15. Shadows

Use shadows sparingly.

Prefer subtle elevation.

Avoid:

* Strong black shadows
* Large blurred shadows
* Multiple shadows on the same element
* Shadows everywhere

A premium SaaS interface should generally feel clean and lightweight.

---

## 16. Visual Hierarchy

Every page must have a clear primary focus.

Ask:

"What should the user notice first?"

Then:

"What should they notice second?"

Then:

"What actions are most important?"

Visual weight should follow this hierarchy.

Do not make every element equally prominent.

---

## 17. Density

Do not optimize only for maximum information density.

Balance:

Information density
+
Readability
+
Whitespace

Administrative systems can be information-dense, but they must remain easy to scan.

---

## 18. Responsive Design

All UI must work properly on:

* Desktop
* Laptop
* Tablet
* Mobile

Do not simply shrink desktop layouts.

Re-evaluate:

* Navigation
* Tables
* Forms
* Cards
* Button groups
* Spacing
* Typography

for smaller screens.

---

## 19. Interaction States

Every interactive component should consider:

* Default
* Hover
* Focus
* Active
* Disabled
* Loading
* Error
* Success

Do not rely only on color to communicate state.

Transitions should be subtle and fast.

Avoid excessive animations.

---

## 20. Component Reuse

Do not repeatedly implement slightly different versions of the same component.

Create reusable components for common patterns:

* Button
* Input
* Select
* Modal
* Drawer
* Card
* Badge
* Alert
* Table
* Pagination
* Empty state
* Loading state
* Confirmation dialog
* Page header
* Section header

When a design change is required, prefer changing the shared component instead of duplicating styles.

---

## 21. Avoid AI-Generated UI Patterns

Do NOT blindly use:

* Excessive rounded cards
* Excessive gradients
* Huge icons
* Random colors
* Excessive shadows
* Giant headings
* Too many badges
* Too many decorative elements
* Excessive glassmorphism
* Arbitrary spacing
* Inconsistent border radius
* Mixed icon libraries
* Emoji as UI elements
* Random animations

The UI should look engineered, not AI-generated.

---

## 22. Before Writing UI Code

Before modifying a UI:

1. Inspect the existing page.
2. Inspect shared components.
3. Inspect existing design tokens.
4. Identify the current spacing system.
5. Identify the typography system.
6. Identify the icon library.
7. Identify reusable components.
8. Identify inconsistencies.
9. Create a visual hierarchy plan.
10. Only then modify the implementation.

Do not immediately start changing CSS.

---

## 23. UI Review Checklist

Before considering a UI task complete, review:

### Typography

* Is the hierarchy clear?
* Are font sizes consistent?
* Are weights appropriate?
* Is line height comfortable?

### Spacing

* Are margins consistent?
* Are paddings consistent?
* Are related elements grouped correctly?
* Is there enough whitespace?

### Icons

* Are icons from the same family?
* Are sizes consistent?
* Are icons aligned correctly?
* Are clickable areas large enough?

### Components

* Are buttons consistent?
* Are inputs consistent?
* Are cards consistent?
* Are states implemented?

### Layout

* Is everything aligned?
* Is the page visually balanced?
* Is the primary action obvious?
* Is the content too dense or too sparse?

### Responsive

* Does it work on mobile?
* Does it work on tablet?
* Do tables/forms remain usable?

### Code Quality

* Are existing components reused?
* Are design tokens reused?
* Are arbitrary CSS values avoided?
* Is duplicated styling avoided?
* Is the implementation maintainable?

---

## 24. Definition of Done

A UI task is NOT complete simply because:

* It compiles.
* It works functionally.
* The API works.
* The page is responsive.

It is complete only when:

1. Functionality works.
2. Visual hierarchy is clear.
3. Spacing is consistent.
4. Typography is polished.
5. Icons are consistent.
6. Components are reusable.
7. Responsive behavior is correct.
8. Interaction states are handled.
9. Accessibility is considered.
10. The page looks production-ready rather than merely functional.
