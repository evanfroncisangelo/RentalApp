# Copilot Instructions

## Project Guidelines
- User wants implementation executed batch-by-batch and prefers to manually test/verify each batch before proceeding to the next.
- User wants the login page as the app landing page, removing the home page flow. The post-login landing should be the dashboard.
- User wants pagination on all data grids.
- User wants a merged view of Expense Categories and Expenses.
- User prefers Add/Edit actions to use modal dialogs with in-place save and automatic grid refresh in the Razor Pages UI.
- User prefers UI terminology updates: use 'Apartments' instead of 'Leases' and use 'Max Capacity' wording in unit add/edit forms.
- User wants validation failures (e.g., inactive/nonexistent unit) surfaced as user-friendly error messages in Razor Pages UI instead of unhandled runtime exceptions.
- Use a Modern Minimal DataGrid Dashboard UI standard: data-grid-centric pages, dashboard summary cards, modal-based CRUD, status badges, sidebar navigation, and consistent page flow (header ? summary/filters ? grid ? actions/details) with soft SaaS styling (light background, white cards, deep teal accents, subtle borders/shadows, rounded corners, whitespace).
- For UI redesign, preserve existing features only (no new functional additions), use Razor Pages-compatible patterns only (no Blazor), keep responsive behavior for desktop/tablet/mobile, allow script adjustments only to support UI, and prioritize performant UI implementations.

## Utilities Module Guidelines
- Use BillingPeriod month key format (YYYY-MM).
- Implement flat-rate billing.
- Auto-generate bill when new meter reading is recorded.
- Apply proration for tenant responsibility changes.
- Enforce duplicate reading protection.
- Allow overpayment.
- Allow backdated readings with controlled recalculation workflow.
- Make due date configurable per customer.
- Keep rate configuration flexible (support global utility-type, per-customer, and period-specific overrides).