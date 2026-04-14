# Parity Checklist

## Purpose

This checklist defines what the rebuilt app must match, what it may improve, and what must be deliberately rethought instead of copied.

## Must Match

### Security and access behavior

- [ ] The rebuilt app preserves the current role model:
  - `SystemAdmin`
  - `BackofficeUserAdmin`
  - `BackofficeUser`
  - `Customer`
- [ ] Authorization behavior is driven from one authoritative policy source
- [ ] `SystemAdmin` can reach `/admin/users`, `/admin/products`, and `/admin/security`
- [ ] `BackofficeUserAdmin` can reach `/admin/users`
- [ ] `BackofficeUserAdmin` is denied `/admin/products` unless the policy is intentionally changed
- [ ] `Customer` is denied admin-only surfaces
- [ ] Customer row-level order restrictions remain part of the intended model

### Auth behavior

- [ ] Login page renders for anonymous users
- [ ] Register page renders for anonymous users
- [ ] Magic-link login works for customers
- [ ] Entra/OIDC challenge link exists for backoffice sign-in
- [ ] Logout returns the user to the login page

### Shell and navigation behavior

- [ ] Authenticated users land in a styled shell/workspace experience
- [ ] Anonymous auth flows use a simpler auth-specific layout
- [ ] Admin navigation visibility matches role permissions
- [ ] Dashboard, products, customers, orders, reports, and admin surfaces remain accessible to the roles that should see them

### Business/domain behavior

- [ ] Product browsing remains available to customer and backoffice roles as intended
- [ ] Customer, order, and related-detail flows remain available to backoffice roles as intended
- [ ] Reports and dashboard remain part of the app scope

## May Improve

- [ ] denial UX may move from mixed `404`/page-denial semantics to one explicit, user-facing pattern
- [ ] admin navigation may become cleaner if library-level permission integration improves
- [ ] auth UX and testability may improve so long as supported user flows still work
- [ ] component styling and startup validation may become more self-protecting than in the current app

## Must Be Rethought

- [ ] manual asset-link maintenance in app hosts
- [ ] ambiguous dev email/test delivery modes
- [ ] test assumptions that are not tied to actual library behavior
- [ ] overreliance on page-smoke tests as evidence of end-to-end quality
- [ ] sibling-repo coordination as the default integration model

## Minimum Critical User Journeys Before Cutover

The rebuilt app should not cut over until these journeys are passing and trusted.

1. Anonymous user opens `/login`, sees the login UI, and can navigate to `/register`.
2. New or seeded customer completes a real magic-link login flow and lands in the authenticated app.
3. Customer attempts to reach admin-only pages and is denied according to the policy.
4. Backoffice user signs in and can reach core operational surfaces such as orders and customers.
5. Backoffice user admin signs in, sees the admin menu, and can reach user administration.
6. System admin signs in and can reach product administration and the security dashboard.
7. At least one meaningful business workflow changes data and is verified against backend truth, not just the UI.

## Rebuild Readiness Interpretation

### If the rebuilt app matches these items

The rebuild has reached functional parity on the core app surface and can be evaluated for cutover readiness.

### If the rebuilt app improves these items

That is acceptable, provided the security model and core user flows remain intact.

### If the rebuilt app changes these items

The change must be explicit, documented, and approved as an intentional product decision rather than an accidental divergence.
