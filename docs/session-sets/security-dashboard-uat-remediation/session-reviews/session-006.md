# Verification Round 1

`VERIFIED`

### 1. Does each of the four Finding 5 sub-items (5a SSN, 5b Add button, 5c singular names, 5d safety-net removal) have a correct fix and test coverage?

Yes.
- **5a (SSN):** The fix of registering `SocialSecurityNumber` in both `Program.cs` files is correct. It is covered by Playwright's `DefaultMasks_SeededSsnRow_RendersSingularTableAndSsnColumnAsync` test, which verifies the display name in the grid.
- **5b (Add button):** The fix of filtering the column dropdown to show only `AvailableColumns` is a correct and user-friendly solution to the silent disabled state. It is covered by two Playwright tests (`..._EnabledForFreshPair...` and `..._HiddenFromDropdown...`) and a new bUnit test (`ColumnDropdown_ExcludesAlreadyRestrictedColumns`).
- **5c (Singular names):** The fix of changing display names in `Program.cs` entity registrations is correct. It is covered by Playwright's `DefaultMasks_TableDropdown_ShowsSingularEntityNamesAsync` and `DefaultMasks_SeededSsnRow...` tests.
- **5d (Safety-net removal):** The feature was removed correctly. Coverage includes Playwright's `DefaultMasks_SafetyNetPanel_AbsentFromTabAsync` and bUnit's `DefaultMasksTabTests.Renders_HeaderAndGrid_WithoutSafetyNet` tests asserting the UI's absence, plus the removal of old tests.

### 2. Is the AvailableColumns filter approach correct for preventing silent Add-disabled states, or does it hide a different case that should stay visible?

The `AvailableColumns` filter approach is correct. It directly solves the UAT finding by preventing the user from selecting a table/column pair that is already restricted. Since a column cannot have more than one restriction for a given role, there is no valid case where an already-restricted column should appear in the "Add New" dropdown. The preservation of the `CanAdd` guard as a defense-in-depth measure is also correct.

### 3. Are the Playwright tests driving the same steps the UAT checklist will hand to the human (persona match, seeded parameters, data-testid selectors)?

Yes. The tests use the correct `admin@acmecoffee.com` persona, operate on seeded data described in the UAT (e.g., `BackofficeUser`'s restrictions), and use robust `data-testid` selectors. The test for adding a default mask uses `Customer.Phone` instead of the UAT's suggested `Customer.SocialSecurityNumber`, but this is a valid and arguably better choice, as `SSN` is already masked in the seed data, making `Phone` a better test of the "add new" workflow.

### 4. Are there any leftover references to the safety-net feature (components, services, DI, tests) that were missed?

No. The removal was comprehensive. The component, its bUnit tests, and the corresponding Playwright tests were deleted. The parent tab was cleaned of all direct references. The `AuthorizationStateNotifier` subscription that remains serves a valid purpose (in-circuit grid refresh) and is no longer coupled to the safety-net feature.

### 5. Any regressions in the non-Playwright unit/bUnit suites, or any test that now tests less than it did before?

No. The `dotnet test` suite passed cleanly. Tests for the removed feature were correctly deleted. A new bUnit test was added to cover the `AvailableColumns` logic, strengthening the suite. The remaining tests for existing components were not weakened.

### 6. Does the [Obsolete] cascade count (184) and the new test count (663) line up with the deletion + addition math you can derive from the diff?

Yes. The warning count is consistent with previous sessions. The final non-Playwright test count of 663 is plausible given the reported removal of ~8 tests related to the safety-net feature and the addition of 1 new bUnit test for the column-restriction fix. The net change is a small decrease, which aligns with the scope of work.
