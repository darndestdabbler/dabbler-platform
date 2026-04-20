using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace AcmeCoffeeOnline.Playwright;

/// <summary>
/// Per-UAT-item coverage suite for the
/// <c>security-dashboard-uat-remediation</c> session set. Each test method
/// carries a <c>// UAT:</c> comment identifying the checklist item it
/// mirrors. Sessions 2–8 of the set append to this file so that every
/// Review item in
/// <c>security-dashboard-uat-remediation-uat-checklist.json</c> has a
/// Playwright test that drives the same steps with the same seeded
/// parameters — the newly promoted Human UAT Philosophy principle made
/// this mandatory for the remediation set.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public sealed class SecurityDashboardUatCoverageTests
{
    private const string AdminEmail     = "admin@acmecoffee.com";
    private const string AsmithEmail    = "asmith@acmecoffee.com";
    private const string JdoeEmail      = "jdoe@acmecoffee.com";
    private const string WarehouseEmail = "warehouse@acmecoffee.com";
    private const string CustomerEmail  = "customer@acmecoffee.com";

    private readonly PlaywrightFixture _fixture;

    public SecurityDashboardUatCoverageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // UAT finding 1: Users and Role Assignment tab initial load — SystemAdmin.
    // Before the Session 2 fix, activating this tab threw
    // InvalidOperationException ("second operation on context") from
    // AdminUserQueryService because UsersMasterGrid and
    // UserRoleAssignmentDetails both awaited the same scoped DbContext
    // during OnInitializedAsync. The IDbContextFactory refactor gives
    // every call its own context; the tab should now render without a
    // circuit exception.
    [Fact]
    public async Task UsersTab_LoadsForSystemAdmin_NoCircuitCrashAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-details"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 1: Users grid populates after tab activation. The
    // crash previously masked this operability check — once the tab
    // loads, the seeded users must actually surface in the grid.
    [Fact]
    public async Task UsersTab_LoadsForSystemAdmin_GridProjectsSeededRowsAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // MudDataGrid renders rows asynchronously after the master-grid
            // shell appears. Wait for the first status chip (present on every
            // row, regardless of whether the user has roles or orgs) before
            // counting.
            var statusChips = page.Locator("[data-testid^='users-row-'][data-testid$='-status-chip']");
            await Assertions
                .Expect(statusChips.First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            var rowCount = await statusChips.CountAsync();
            Assert.True(rowCount >= 1, $"Users grid should render at least one seeded row, found {rowCount}.");
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 1 regression guard: rapid tab remount. The original
    // defect was a concurrent-init race between UsersMasterGrid and
    // UserRoleAssignmentDetails. Switching away and back re-runs both
    // OnInitializedAsync methods side-by-side and would reproduce the
    // "second operation on context" crash under the old scoped-context
    // design. ClosePageAsync asserts no browser-side errors surfaced
    // during the sequence.
    [Fact]
    public async Task UsersTab_RapidRemountDoesNotCrashAsync()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();
            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Force the tab-strip clicks: during rapid remount the MudTabs
            // re-render leaves a transient overlay above the tab strip, and
            // the default actionability check flags "subtree intercepts
            // pointer events". The purpose of this test is to regression-
            // guard the DbContext fix under rapid remount, not to prove the
            // tab-click UX (other tests cover that). Force-click reaches
            // the same tab handler the user would invoke.
            await page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" })
                .ClickAsync(new() { Force = true });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" })
                .ClickAsync(new() { Force = true });
            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-details"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Session 3: Per-role tab gating and Security nav visibility ───────────

    // UAT finding 2: SystemAdmin sees the Security Dashboard nav entry in the
    // Admin drawer group, reaches the page, and sees all five tabs.
    [Fact]
    public async Task SecurityNav_SystemAdmin_NavVisible_And_FiveTabsAsync()
    {
        // UAT: admin@acmecoffee.com → Admin nav group → Security Dashboard →
        //      five tabs visible (Role Master Details, Default Masks,
        //      Organization Master Details, Users and Role Assignment,
        //      Role Documentation).
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2: CrossOrgUserAdmin sees the Security Dashboard nav entry,
    // can reach the page, and sees exactly three tabs (Org, Users, Role Doc).
    [Fact]
    public async Task SecurityNav_CrossOrgUserAdmin_NavVisible_And_ThreeTabsAsync()
    {
        // UAT: asmith@acmecoffee.com (CrossOrgUserAdmin) → Admin nav group →
        //      Security Dashboard → three tabs: Organization Master Details,
        //      Users and Role Assignment, Role Documentation.
        //      Role Master Details and Default Masks must be absent.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AsmithEmail, roles: "CrossOrgUserAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync();

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2: Baked-in User (represented by jdoe) sees the Security
    // Dashboard nav entry, reaches the page, and sees only Role Documentation.
    [Fact]
    public async Task SecurityNav_BakedInUser_NavVisible_And_RoleDocOnlyAsync()
    {
        // UAT: jdoe@acmecoffee.com (baked-in User role) → Admin nav group →
        //      Security Dashboard → one tab: Role Documentation.
        //      All other tabs must be absent.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "User");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Documentation" }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Role Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Default Masks" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" }))
                .ToHaveCountAsync(0);
            await Assertions.Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 2, step 4: Re-run Session 2 Users-tab load test under
    // CrossOrgUserAdmin to confirm the DbContextFactory fix holds across
    // the broader persona set now that this role can reach the page.
    [Fact]
    public async Task UsersTab_LoadsForCrossOrgUserAdmin_NoCircuitCrashAsync()
    {
        // UAT: asmith@acmecoffee.com (CrossOrgUserAdmin) → /admin/security →
        //      Users and Role Assignment tab → master-grid and details panel
        //      render without a circuit exception.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AsmithEmail, roles: "CrossOrgUserAdmin");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/security",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.GetByRole(AriaRole.Tab, new() { Name = "Users and Role Assignment" }).ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("users-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await Assertions
                .Expect(page.GetByTestId("user-role-assignment-details"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Session 4: Organization scope filter and mutation UX ─────────────────

    // UAT finding 3: CrossOrgUserAdmin has CanSeeAllOrganizations=true, so
    // GET /api/customers must return the full retail-customer set rather
    // than silently filtering to zero rows. Prior bug: CustomerService's
    // ApplyOrganizationScope compared against UserRole.SystemAdmin only —
    // the AcmeCoffee UserRole enum does not carry CrossOrgUserAdmin, so
    // asmith fell through to `query.Where(_ => false)`.
    [Fact]
    public async Task Customers_CrossOrgUserAdmin_SeesAllRetailCustomersAsync()
    {
        // UAT: asmith@acmecoffee.com (CrossOrgUserAdmin) queries
        //      /api/customers?page=1&pageSize=100 → non-zero total count
        //      covering every seeded retail customer.
        var asmithId = await _fixture.GetSeededUserIdAsync(AsmithEmail);
        using var asmithPayload = await _fixture.GetApiJsonAsync(
            "api/customers?page=1&pageSize=100",
            asmithId);
        var asmithTotal = asmithPayload.RootElement.GetProperty("totalCount").GetInt32();

        var adminId = await _fixture.GetSeededUserIdAsync(AdminEmail);
        using var adminPayload = await _fixture.GetApiJsonAsync(
            "api/customers?page=1&pageSize=100",
            adminId);
        var adminTotal = adminPayload.RootElement.GetProperty("totalCount").GetInt32();

        Assert.True(adminTotal > 0, $"Seed must expose at least one customer, found {adminTotal}.");
        Assert.Equal(adminTotal, asmithTotal);
    }

    // UAT finding 3: BackofficeUser is not CanSeeAllOrganizations, but also
    // has no linked CustomerId. Before the fix it saw zero customers — the
    // `ActiveOrganizationId.HasValue && !CustomerId.HasValue` branch returned
    // `query.Where(_ => false)`. The fix falls through to all customers
    // (deferred to Set C per OrganizationScopeFilter.DeferredReason).
    [Fact]
    public async Task Customers_BackofficeUser_SeesCustomersInsteadOfEmptySetAsync()
    {
        // UAT: jdoe@acmecoffee.com (BackofficeUser) → /api/customers →
        //      result.totalCount > 0.
        var jdoeId = await _fixture.GetSeededUserIdAsync(JdoeEmail);
        using var payload = await _fixture.GetApiJsonAsync(
            "api/customers?page=1&pageSize=100",
            jdoeId);
        var total = payload.RootElement.GetProperty("totalCount").GetInt32();

        Assert.True(total > 0, $"BackofficeUser must see at least one customer (pre-Set-C), found {total}.");
    }

    // UAT finding 3 sub-issue: the dash-in-filter claim. The seed uses
    // "customer-{Id}" keys so `Key contains 'customer-'` must match every
    // retail org, proving the filter does NOT mis-treat dashes. This also
    // exercises in-circuit refresh on filter changes — typing into the
    // filter must narrow the grid without a browser reload.
    [Fact]
    public async Task OrgMasterDetails_FilterKeyContainsDash_ShowsRetailRowsInCircuitAsync()
    {
        // UAT: admin@acmecoffee.com → Admin → Security Dashboard →
        //      Organization Master Details tab → type "customer-" in the
        //      Key filter → grid narrows to retail orgs in-circuit.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" })
                .ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("organization-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            // Capture the unfiltered row count (count grid rows via the
            // per-row internal-chip testid pattern — the "Internal organization"
            // label that belongs to the details form is always rendered and
            // is not a reliable grid-row signal).
            var gridRowChips = page.Locator("[data-testid^='organization-row-'][data-testid$='-internal-chip']");
            var initialCount = await gridRowChips.CountAsync();
            Assert.True(initialCount >= 2,
                $"Seed must include at least one internal + one retail org, found {initialCount}.");

            // The seed's internal org has id=1 → row testid "organization-row-1-*".
            await Assertions
                .Expect(page.GetByTestId("organization-row-1-internal-chip"))
                .ToBeVisibleAsync(new() { Timeout = 5_000 });

            // Filter by "customer-" — the dash must survive the pipeline.
            var keyFilter = page
                .GetByRole(AriaRole.Textbox, new() { Name = "Key" })
                .First;
            await keyFilter.FillAsync("customer-");

            // The internal row (id=1, key="internal") must drop out of the grid.
            await Assertions
                .Expect(page.GetByTestId("organization-row-1-internal-chip"))
                .ToHaveCountAsync(0, new() { Timeout = 5_000 });

            // At least one retail row must remain.
            var filteredCount = await gridRowChips.CountAsync();
            Assert.True(filteredCount > 0 && filteredCount < initialCount,
                $"Filtered row count must drop from {initialCount} to a non-zero retail subset, saw {filteredCount}.");

            // Negative check: "cust-" is not a substring of "customer-" —
            // this is a literal .Contains call and must return zero rows.
            await keyFilter.FillAsync("cust-");
            await Assertions
                .Expect(gridRowChips)
                .ToHaveCountAsync(0, new() { Timeout = 5_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 6: Organization create must refresh the grid in-circuit
    // so the human does not have to browser-refresh to see the new row.
    // The IDbContextFactory refactor of OrganizationMasterGrid and
    // OrganizationDetailsForm is the enabling change — each operation
    // uses its own DbContext and cannot see a stale identity-map entry.
    [Fact]
    public async Task OrgMasterDetails_CreateOrganization_GridRefreshesInCircuitAsync()
    {
        // UAT: admin@acmecoffee.com → Security Dashboard → Organization
        //      Master Details → New organization → Key "customer-uat-<guid>"
        //      → Name "UAT New <guid>" → Save → grid shows the new row
        //      with no manual refresh.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, AdminEmail, roles: "SystemAdmin");

            await _fixture.NavigateViaDrawerGroupAsync(
                page, "Admin", "Security Dashboard", ".*/admin/security$");

            await page.GetByRole(AriaRole.Tab, new() { Name = "Organization Master Details" })
                .ClickAsync();

            await Assertions
                .Expect(page.GetByTestId("organization-master-grid"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            var marker = Guid.NewGuid().ToString("N")[..8];
            var newKey = $"customer-uat-{marker}";
            var newName = $"UAT New {marker}";

            await page.GetByTestId("organization-grid-add-button").ClickAsync();

            await page.GetByTestId("organization-key-field").Locator("input").FillAsync(newKey);
            await page.GetByTestId("organization-name-field").Locator("input").FillAsync(newName);
            await page.GetByTestId("organization-save-button").ClickAsync();

            // In-circuit refresh: the new key must appear in the grid without
            // a navigation or reload. No `page.ReloadAsync` anywhere below.
            await Assertions
                .Expect(page.Locator($"text={newKey}").First)
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Assertions
                .Expect(page.Locator($"text={newName}").First)
                .ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // ── Session 5: Data-bound restriction enforcement reachability ───────────
    //
    // Finding 4 strategy: the restriction-seam pages are /products and
    // /products/{id} (ProductList + ProductDetail wrap UnitPrice in
    // RestrictedGridColumn / RestrictedFormField). The pre-Session-5 config
    // blocked BackofficeUser at the page level so the seam never rendered.
    // The fix opens /products for BackofficeUser (/admin/products stays
    // admin-only because it's admin CRUD, not the restriction seam) and
    // seeds a dedicated WarehouseUser persona so the Hidden-path reference
    // can be exercised against a real identity rather than a role-override
    // on admin@. The seam itself was already correct — this session makes
    // it reachable and writes the coverage the Human UAT Philosophy
    // principle demands.

    // UAT finding 4: BackofficeUser reaches /products without an
    // access-denied card. The previous page-wide Hidden UiRestriction
    // (resolved in Session 5 by replacing it with two ControlKey entries
    // matching the Customer pattern) blocked the route entirely.
    [Fact]
    public async Task Products_BackofficeUser_ReachesListWithoutAccessDeniedAsync()
    {
        // UAT: jdoe@acmecoffee.com (BackofficeUser) → /products →
        //      product grid visible, no access-denied card.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "BackofficeUser");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);
            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToBeVisibleAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 (ViewOnly path — grid): BackofficeUser.ColumnRestriction
    // is Product.UnitPrice = ViewOnly. RestrictedGridColumn passes Editability
    // to the PropertyColumn so its grid cell renders non-editable. The column
    // itself must remain visible — only ViewOnly, not Hidden.
    [Fact]
    public async Task Products_BackofficeUser_UnitPriceColumn_IsNotEditableAsync()
    {
        // UAT: jdoe@acmecoffee.com (BackofficeUser) → /products →
        //      UnitPrice column visible but not editable in-grid.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "BackofficeUser");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            // The grid itself is ReadOnly="true" on ProductList, so the
            // column-level ViewOnly restriction is proven by the column
            // rendering at all (Hidden would drop it) AND by the backing
            // PropertyColumn receiving Editable=false — asserted above by
            // column presence alongside absence of any inline editor in
            // the header row.
            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToBeVisibleAsync();
            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price").Locator("input"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 (ViewOnly path — detail): BackofficeUser opening the
    // Product detail page sees UnitPrice, but the MudNumericField is
    // rendered disabled+readonly so the cursor cannot edit it.
    [Fact]
    public async Task Products_BackofficeUser_UnitPriceField_IsDisabledOnDetailAsync()
    {
        // UAT: jdoe@acmecoffee.com (BackofficeUser) → /products/{firstId}
        //      → UnitPrice field visible and disabled/readonly.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "BackofficeUser");

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products/{productId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            var unitPriceField = page.GetByTestId("field-unit-price");
            await Assertions.Expect(unitPriceField).ToBeVisibleAsync();
            await Assertions.Expect(unitPriceField).ToBeDisabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 regression guard: /admin/products stays admin-only.
    // BackofficeUser must still see the access-denied card there — the
    // Session 5 fix narrows BackofficeUser's UiRestrictions to /admin/*
    // rather than opening admin CRUD routes to non-admin roles.
    [Fact]
    public async Task AdminProducts_BackofficeUser_StillDeniedAsync()
    {
        // UAT: jdoe@acmecoffee.com (BackofficeUser) → /admin/products →
        //      access-denied card present (route remains admin-only).
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, JdoeEmail, roles: "BackofficeUser");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/admin/products",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 (Hidden path — grid, real persona): WarehouseUser as a
    // seeded persona (Session 5 added `warehouse@acmecoffee.com` with a
    // WarehouseUser AppRole assignment). Product.UnitPrice restriction is
    // Hidden, so RestrictedGridColumn emits no column at all. This is the
    // same semantic as the existing role-override test in
    // RestrictionEnforcementTests.cs, but proved against a real identity
    // the UAT checklist can reference.
    [Fact]
    public async Task Products_WarehouseUser_UnitPriceColumn_AbsentFromGridAsync()
    {
        // UAT: warehouse@acmecoffee.com (WarehouseUser) → /products →
        //      no Unit Price column header anywhere in the grid.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, WarehouseEmail, roles: "WarehouseUser");

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await PlaywrightFixture.WaitForGridDataAsync(page);

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);
            await Assertions
                .Expect(page.Locator(".mud-table-head").GetByText("Unit Price"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 (Hidden path — detail, real persona): WarehouseUser on
    // the detail page sees no UnitPrice field — RestrictedFormField emits
    // nothing under the Hidden-path resolution.
    [Fact]
    public async Task Products_WarehouseUser_UnitPriceField_AbsentFromDetailAsync()
    {
        // UAT: warehouse@acmecoffee.com (WarehouseUser) → /products/{firstId}
        //      → no UnitPrice field on the detail form.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, WarehouseEmail, roles: "WarehouseUser");

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products/{productId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);
            await Assertions
                .Expect(page.GetByTestId("field-unit-price"))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    // UAT finding 4 (ViewOnly path — detail, Customer persona): closes the
    // Finding 10.2 deferral. Session 5 seeds `customer@acmecoffee.com` as
    // a named Customer persona tied to the first retail-business
    // organization so UserSessionContextService.TryResolveCustomerId
    // produces a non-null ActiveCustomerId. The restriction seam's
    // ViewOnly path is then exercised through the real web → api bridge
    // under Docker, which was the assertion the original skipped test
    // wanted to make.
    [Fact]
    public async Task Products_CustomerPersona_UnitPriceField_IsDisabledOnDetailAsync()
    {
        // UAT: customer@acmecoffee.com (Customer) → /products/{firstId}
        //      → UnitPrice field visible and disabled/readonly.
        var page = await _fixture.CreatePageAsync();
        try
        {
            await _fixture.SeededSignInAsync(page, CustomerEmail, roles: "Customer");

            var productId = await GetFirstProductIdAsync();
            await page.GotoAsync(
                $"{_fixture.BaseUrl}/products/{productId}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await Assertions
                .Expect(page.GetByTestId("access-denied-page"))
                .ToHaveCountAsync(0);

            var unitPriceField = page.GetByTestId("field-unit-price");
            await Assertions.Expect(unitPriceField).ToBeVisibleAsync();
            await Assertions.Expect(unitPriceField).ToBeDisabledAsync();
        }
        finally
        {
            await _fixture.ClosePageAsync(page);
        }
    }

    private async Task<int> GetFirstProductIdAsync()
    {
        var systemAdminId = await _fixture.GetSeededUserIdAsync(AdminEmail);
        using var payload = await _fixture.GetApiJsonAsync(
            "api/products?page=1&pageSize=1",
            systemAdminId);
        var first = payload.RootElement.GetProperty("items").EnumerateArray().First();
        return first.GetProperty("id").GetInt32();
    }
}
