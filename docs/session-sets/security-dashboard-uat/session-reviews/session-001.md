# Verification Round 1

ISSUES_FOUND

---

### Major

*   **Issue**
    The checklist expects the reviewer to verify user roles that are not mentioned in the spec's context. The `User Assignments Tab` expectation mentions "casey.hart has dual internal/retail membership" and "sloane.reyes=regional rep," but the spec and the `Roles Tab` test case only account for four roles: Customer, BackofficeUser, BackofficeUserAdmin, and SystemAdmin. This discrepancy will confuse the reviewer and may make the test item impossible to pass.
*   **Location**
    `FunctionalArea`: "User Assignments Tab"
    `Subarea`: "User List"
    `ItemLabel`: "Seeded users appear with their roles"
*   **Fix**
    Update the `Expectation` field to align with the four roles defined in the spec. For example, determine the correct roles for `casey.hart` and `sloane.reyes` from the seed data (e.g., `BackofficeUser`, `Customer`) and state them explicitly.

### Minor

*   **Issue**
    A test case requires the reviewer to sign in as a `BackofficeUserAdmin` but does not provide a specific, seeded user email for that role. This violates the requirement to include concrete seeded test data in `HumanAction` fields.
*   **Location**
    `FunctionalArea`: "Shell and Access"
    `Subarea`: "Role-Based Access"
    `ItemLabel`: "BackofficeUserAdmin cannot reach the dashboard"
*   **Fix**
    Update the `HumanAction` to specify a seeded user with the `BackofficeUserAdmin` role. If no single user has only that role, instruct the reviewer to temporarily assign it to `jdoe@acmecoffee.com` for the test.

*   **Issue**
    The checklist lacks open-ended feedback rows (`IsOtherItem: true`) for two functional areas, creating an inconsistency with the other six areas which all include one.
*   **Location**
    `FunctionalArea`: "Shell and Access"
    `FunctionalArea`: "Tab Strip"
*   **Fix**
    Add an `IsOtherItem: true` row to both the `Shell and Access` and `Tab Strip` functional areas, consistent with the pattern used for all other areas.

*   **Issue**
    The test case for the `Endpoint Access Tab` provides an incomplete list of endpoints in the `Expectation` field while simultaneously asserting that all 21 should be present. The reviewer cannot verify that all 21 are present from the partial list.
*   **Location**
    `FunctionalArea`: "Endpoint Access Tab"
    `Subarea`: "Endpoint List"
    `ItemLabel`: "All 21 registered endpoints are shown"
*   **Fix**
    Update the `Expectation` to be more general and verifiable, for instance: "The list contains 21 endpoints, covering all major operations for Product, Customer, Order, etc. The list is comprehensive and matches the API surface." Alternatively, list all 21 endpoint keys explicitly.

---

# Verification Round 2

VERDICT: VERIFIED

All issues from the round-1 review have been resolved. The checklist is now concrete, consistent, and ready for human execution. No new defects were found.

---

### Round 1 Issue Verification

*   **Major: Mismatched user roles in "User Assignments Tab" test case**
    *   **Status:** Resolved
    *   **Verification:** The `Expectation` field for `ItemLabel`: "Seeded users appear with their roles" now explicitly lists the complex role assignments for `sloane.reyes` and `casey.hart` using the four canonical roles from the spec (e.g., `BackofficeUser` + `BackofficeUserAdmin`), removing the confusing, non-spec terminology. The test is now precise and verifiable against the seed data.

*   **Minor: Missing seeded user for `BackofficeUserAdmin` sign-in**
    *   **Status:** Resolved
    *   **Verification:** The `HumanAction` for `ItemLabel`: "BackofficeUserAdmin cannot reach the dashboard" now specifies signing in as `kbrown@acmecoffee.com`, providing the concrete test data required.

*   **Minor: Missing open-ended feedback rows**
    *   **Status:** Resolved
    *   **Verification:** Both the "Shell and Access" and "Tab Strip" functional areas now contain a final checklist item with `IsOtherItem: true`, matching the pattern of all other areas.

*   **Minor: Incomplete endpoint list**
    *   **Status:** Resolved
    *   **Verification:** The `Expectation` for `ItemLabel`: "All 21 registered endpoints are shown" now contains a complete, comma-separated list of all 21 endpoint keys, making the requirement fully verifiable.
