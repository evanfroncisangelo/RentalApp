# Copilot Instructions

## Project Guidelines
- User wants implementation executed batch-by-batch, preferring to manually test/verify each batch before proceeding to the next. Large implementation requests should be executed in batches, with scan/read/analyze first before making changes.
- User wants the login page as the app landing page, removing the home page flow. The post-login landing should be the dashboard.
- User wants pagination on all data grids and prefers AJAX-style pagination across all datagrids so page switches update grid content without full-page refresh.
- User wants a merged view of Expense Categories and Expenses.
- User prefers Add/Edit actions to use modal dialogs with in-place save and automatic grid refresh in the Razor Pages UI.
- User prefers UI terminology updates: use 'Apartments' instead of 'Leases'.
- User wants validation failures (e.g., inactive/nonexistent unit) surfaced as user-friendly error messages in Razor Pages UI instead of unhandled runtime exceptions.
- Use a Modern Minimal DataGrid Dashboard UI standard: data-grid-centric pages, dashboard summary cards, modal-based CRUD, status badges, sidebar navigation, and consistent page flow (header ? summary/filters ? grid ? actions/details) with soft SaaS styling (light background, white cards, deep teal accents, subtle borders/shadows, rounded corners, whitespace).
- For UI redesign, preserve existing features only (no new functional additions), use Razor Pages-compatible patterns only (no Blazor), keep responsive behavior for desktop/tablet/mobile, allow script adjustments only to support UI, and prioritize performant UI implementations.
- User wants add actions (payment/property/unit) to update datagrids without full-page refresh, and allows multiple deposits per unit only when deposit month differs.
- Use MMM-dd-yyyy date format (e.g., Jan-01-2026) for date-related displays across the app.
- User prefers tenant count to be computed automatically from tenants connected to the unit, removing the Max Capacity field from Add/Edit Tenant.
- User does not want the prior truncate/reset request treated as a standing preference or remembered instruction.
- User wants tenancy assignment behavior to be based only on Unit max capacity: no room creation and no room-based assignment; if unit is full, adding a tenant to that unit must be blocked.
- MaxCapacity default value must be 0, and MaxCapacity input must be present in Add Unit and Edit Unit modal flows.
- User prefers registration limit behavior to be UI-only (hide register button) and does not want backend registration to throw when user count exceeds 3.

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

## GitHub Actions Guidelines
- For this project’s GitHub Actions WebDeploy step, keep credentials/secrets out of logs and only append -allowUntrusted when WEBDEPLOY_ALLOW_UNTRUSTED equals 'true'.