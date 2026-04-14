# Do Not Carry Forward

## Fragile Patterns To Leave Behind

| Current fragile pattern | Why it is fragile | Preferred replacement |
| --- | --- | --- |
| Manual app-host CSS link lists with no validation | Missing one required asset can break the shell or component pages in subtle ways | One supported Dabbler asset entrypoint plus startup/smoke validation |
| Using the authenticated shell layout for anonymous auth pages | It couples unrelated concerns and makes auth-page rendering easier to break | Separate `AuthLayout` and authenticated shell layout from the start |
| Assuming dev-email behavior from naming alone | Tests and humans infer the wrong delivery mechanism | Explicit, correctly named dev/test auth delivery modes |
| Treating policy expectations as tribal knowledge | Tests drift from real authorization intent | Validate all representative auth expectations against authoritative config |
| Depending on UI login for every browser test | It slows tests and makes non-auth flows brittle | Use seeded-session sign-in for non-auth tests and keep real login only for auth-specific tests |
| App-level permission bridging everywhere | It increases drift and duplicates policy interpretation | Prefer library-supported shell/nav/permission integration where practical |
| Page-load-only test assertions | They create false confidence | Require meaningful assertions, and backend verification for important data changes |
| Accepting repo split drift as normal | It hides version and integration faults until late | One integrated repo with atomic app/library/test changes |

## Preferred Replacement Rules

- templates must provide the supported layout, asset, and auth/test baseline
- library behavior must make supported dev/test modes explicit
- tests must separate auth-flow validation from non-auth feature validation
- configuration must be authoritative and machine-validated
- app/library integration must be intentional, documented, and repeatable

## Explicit Anti-Patterns For Humans And AI Agents

- Do not create a layout without rendering routed content.
- Do not assume a third-party or sibling library asset is loaded unless the template or smoke tests prove it.
- Do not hard-code authorization expectations that are not checked against the policy config.
- Do not write browser tests that depend on external delivery systems unless the test is specifically about that system.
- Do not describe page-smoke coverage as meaningful end-to-end coverage.
- Do not copy app-specific workaround code into the rebuild without re-justifying it.
- Do not preserve misleading names for development modes or test helpers.

## Carry-Forward Rule Of Thumb

If a behavior exists because the current repo was awkward to integrate, rebuild it.

If a behavior exists because it reflects actual business intent, preserve it.
