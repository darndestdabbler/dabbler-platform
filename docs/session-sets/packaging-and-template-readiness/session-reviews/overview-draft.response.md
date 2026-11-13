# Dabbler Platform Overview

A set of reusable .NET libraries for building secure, data-driven Blazor Server applications.

The Dabbler Platform provides foundational components and services for common enterprise application needs. It is designed to accelerate development by offering robust, pre-built solutions for security, reporting, complex data filtering, and testing. The platform is built on a MudBlazor-native UI layer, preferring direct composition with MudBlazor components over heavy-handed abstractions.

This document provides a high-level overview of the platform's capabilities, the libraries it contains, and the intended path for adoption. After reading this, you should understand whether the platform fits your problem and where to go for more detailed, hands-on information.

## What Problems Does It Solve?

The Dabbler Platform is not a full-stack framework but a collection of targeted libraries that solve specific, recurring problems in enterprise application development:

-   **Complex Data Filtering:** Provides a declarative Query-by-Example (QBE) filter panel that allows users to build complex queries with AND/OR logic, which are then translated into server-side expressions.
-   **Runtime-Configurable Authorization:** Implements a sophisticated, restriction-based authorization model that allows permissions for UI elements, data rows, and data columns to be configured at runtime without a redeploy.
-   **Banded Reporting and PDF Export:** Includes a complete reporting engine for defining, executing, and rendering banded reports (e.g., invoices, summaries) with support for grouping, aggregates, and PDF export.
-   **Deterministic E2E Testing:** Solves the challenge of synchronizing Playwright tests with Blazor Server's asynchronous rendering lifecycle via a DOM "watermark" system, eliminating flaky tests caused by timing issues.
-   **Standardized CRUD Patterns:** Offers a small set of helpers for common Create, Read, Update, Delete (CRUD) page patterns, such as dirty-state tracking and consistent action button layouts.
-   **Testable Application Architecture:** Promotes a testable architecture with a supported, opt-in stubbing mechanism for dependency injection, simplifying unit tests (bUnit), demos, and the initial development of generated applications.

## Platform Libraries

The platform consists of several libraries, each with a distinct responsibility. They are designed to be composed together in an application.

### 1. `Dabbler.Components`

This library provides reusable UI components and data-handling abstractions for MudBlazor-native applications. It does not wrap or replace MudBlazor; instead, it provides higher-level components that application pages compose directly.

**Key Features:**

-   **`DeclarativeFilterPanel<TEntity>`:** A powerful QBE filter panel with AND/OR group logic, operator selection, presets, and active filter chips.
-   **CRUD Helpers:** A small set of composable helpers for master-detail pages, including:
    -   `EditSession<T>` for dirty-tracking a record.
    -   `<DirtyAwareActionRow>` for a consistent Save/Cancel/Delete action bar that responds to `EditSession<T>` state.
-   **Data Provider Abstractions:**
    -   `IGridDataProvider<T>` provides a standard contract for connecting `MudDataGrid` to a backend data source, with helpers for applying filter expressions from the `DeclarativeFilterPanel`.
-   **Expression Engine:** A core service that translates filter descriptors from the UI into server-side query expressions.

A typical query page uses these components together:

```razor
<DeclarativeFilterPanel TEntity="Customer"
                        FilterColumns="@filterColumns"
                        Coordinator="@coordinator" />

<MudDataGrid T="Customer"
             ServerData="@dataProvider.ToServerDataFunc(coordinator.GetCompositeFilter)">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.City" Title="City" />
    </Columns>
</MudDataGrid>
```

[Deep Dive: `Dabbler.Components` README](../src/Libs/Dabbler.Components/README.md)

### 2. `Dabbler.Platform.Stubs`

This is a dedicated library for providing in-memory, stub implementations of the platform's data provider abstractions. It is an **opt-in** dependency intended strictly for non-production scenarios.

**Use Cases:**

-   **bUnit Testing:** Isolate components from real data sources during unit tests.
-   **Demos and Prototypes:** Build functional UI demos with hardcoded data.
-   **Bootstrapping Generated Apps:** Provide a working UI for generated applications before real backend services are implemented.

It provides stub implementations for grid data, combo-box items, reports, metadata, and authentication state, which can be wired up via DI helpers.

[Deep Dive: `Dabbler.Platform.Stubs` README](../src/Libs/Dabbler.Platform.Stubs/README.md)

### 3. `Dabbler.Reports.Core`

The core reporting engine. It processes JSON-based Report Definition Files (`.rdf.json`) to lay out banded reports. It is a backend library with no UI components.

**Key Features:**

-   **Banded Layout:** Supports detail, group headers/footers, page headers/footers, and report headers/footers.
-   **Data Shaping:** Handles multi-level grouping, sorting, aggregates, and running totals.
-   **Dynamic Layout:** Includes support for conditional visibility (`PrintWhen`), subreports, and elements that can grow or shrink with their content.

[Deep Dive: `Dabbler.Reports.Core` README](../src/Libs/Dabbler.Reports.Core/README.md)

### 4. `Dabbler.Reports.Export`

A companion to `Dabbler.Reports.Core` that renders composed report pages into PDF documents. It handles fonts, styles, images, and multi-page output.

[Deep Dive: `Dabbler.Reports.Export` README](../src/Libs/Dabbler.Reports.Export/README.md)

### 5. `Dabbler.Reports.Blazor`

Provides the Blazor UI components for interacting with the reporting engine.

**Key Features:**

-   **`DabblerReportPage`:** A full-lifecycle component that can load a report definition, prompt for parameters, execute the report, and display the results.
-   **`ParameterPromptForm`:** A dedicated form for entering report parameters.
-   **`ReportViewer`:** A component for rendering composed report pages in the browser with zoom support.

[Deep Dive: `Dabbler.Reports.Blazor` README](../src/Libs/Dabbler.Reports.Blazor/README.md)

### 6. `Dabbler.Security`

The core authentication and authorization library for ASP.NET Core. It provides backend services for managing identity and enforcing access control.

**Key Features:**

-   **Authentication:**
    -   Passwordless "magic link" login via email.
    -   Time-based One-Time Password (TOTP) multi-factor authentication (MFA).
    -   Integration with external OIDC identity providers (e.g., Microsoft Entra ID).
-   **Runtime-Configurable Authorization:** A powerful restriction-based system.
    -   Five baked-in roles (`SystemAdmin`, `UserAdmin`, etc.) with compiled-in privileges.
    -   Application-defined "user-type" roles configured via JSON.
    -   Fine-grained restrictions:
        -   `RowRestriction`: Filter expressions to scope data access per role.
        -   `ColumnRestriction`: Mark fields as view-only or hidden per role.
        -   `UiRestriction`: Disable or hide pages or UI controls per role.
    -   Sensitive data masking with per-role exemptions.
    -   **No role inheritance:** Users with multiple roles must explicitly switch between them.

[Deep Dive: `Dabbler.Security` README](../src/Libs/Dabbler.Security/README.md)

### 7. `Dabbler.Security.Blazor`

Provides the Blazor UI components for the security system, including login pages, administration screens, and components for enforcing authorization rules in the UI.

**Key Features:**

-   **Authentication Pages:** UI for magic link login and MFA setup.
-   **Administration Pages:** A full suite of pages for managing users, roles, and assignments, plus an audit log viewer.
-   **`DabblerSecurityDashboard`:** A central admin panel for configuring the restriction-based authorization system.
-   **`DabblerSecured`:** A wrapper component to declaratively hide or disable UI content based on the current user's permissions.
-   **`DabblerRoleSwitcher`:** A dropdown allowing users with multiple roles to switch their active context.

[Deep Dive: `Dabbler.Security.Blazor` README](../src/Libs/Dabbler.Security.Blazor/README.md)

### 8. `Dabbler.Testing.Blazor`

A testing utility library focused on enabling stable and deterministic end-to-end (E2E) tests with Playwright for Blazor Server applications.

**Key Features:**

-   **Action Watermark Tracking:** In development mode, it adds special `data-*` attributes to the DOM that signal when Blazor components have finished rendering after an async operation.
-   **Test Readiness Probe:** Adds a `data-blazor-ready` attribute to signal when the initial page load and render is complete.
-   **`TrackedComponentBase`:** An opt-in base class that helps components correctly signal the start and end of their async work to the watermark system.

Playwright tests can wait on these attributes, making them resilient to variations in network latency or rendering speed.

```csharp
// In a Playwright test, wait for all async actions to complete before asserting
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-actions-in-flight", "[]");
```

[Deep Dive: `Dabbler.Testing.Blazor` README](../src/Libs/Dabbler.Testing.Blazor/README.md)

### Development Helpers

The repository also includes `Dabbler.Security.DevIdp`, a simple, local-only OIDC identity provider project. It is used for testing external login flows during development and is **not** a consumable library intended for production deployment.

## Adoption Path

Currently, the Dabbler Platform is consumed directly from source. The libraries reside as integrated projects under the `src/Libs/` directory in this repository. Applications are expected to include these projects in their solution and reference them directly.

A future goal is to publish these libraries as versioned NuGet packages. For now, the integrated source model allows for rapid, coordinated development across the platform and consuming applications.

## Opt-In Stub Mode for Testing and Demos

The platform has a first-class story for testing and prototyping using `Dabbler.Platform.Stubs`. Instead of mocking services or writing one-off test harnesses, you can use the provided stub implementations and register them with the application's dependency injection container. This is the recommended approach for bUnit tests and for creating standalone demo applications.

This behavior is strictly opt-in and controlled via DI. A production application would register real data providers, while a bUnit test host or demo app would register the stubs.

```csharp
// Example: Registering stubs in a test or demo Program.cs

// Define some stub data
var definition = new StubPlatformDefinition();
definition.Navigation.AddRecords(
    "Customer",
    "Recent",
    [new NavigationRecord(101, "Ada Lovelace")]);

// Register the stubs with the DI container
services.AddDabblerPlatformStubs(definition);
services.AddStubGridDataProvider(
    [
        new CustomerRow(101, "Ada Lovelace"),
        new CustomerRow(102, "Grace Hopper")
    ]);
```

## Accessibility Posture

The platform targets **WCAG 2.1 Level AA** as the accessibility standard for all reusable components and application pages. The testing hierarchy includes automated checks that assert against common accessibility issues like color contrast, focus visibility, and layout problems to maintain this posture.

## Where to Go Next

This overview provides a starting point. For more specific information, consult the following documents:

-   **Repository Layout and Local Commands:** See the top-level [README.md](../README.md).
-   **Platform Development Workflow:** Understand how the platform is built using an AI-led workflow by reading the [AI-Led Session Workflow](./ai-led-session-workflow.md) guide.
-   **Toolchain Versioning:** For details on the compatibility between different platform and tooling versions, see the [Compatibility Matrix](./COMPATIBILITY.md).
-   **Governance:** To understand the governance and review processes that apply to platform development, see the [Governance Two-Pager](./planning/governance-two-pager.md).

### Per-Library Deep Dives

For detailed API information, setup instructions, and examples for a specific library, read its dedicated README file:

-   [`Dabbler.Components`](../src/Libs/Dabbler.Components/README.md)
-   [`Dabbler.Platform.Stubs`](../src/Libs/Dabbler.Platform.Stubs/README.md)
-   [`Dabbler.Reports.Core`](../src/Libs/Dabbler.Reports.Core/README.md)
-   [`Dabbler.Reports.Export`](../src/Libs/Dabbler.Reports.Export/README.md)
-   [`Dabbler.Reports.Blazor`](../src/Libs/Dabbler.Reports.Blazor/README.md)
-   [`Dabbler.Security`](../src/Libs/Dabbler.Security/README.md)
-   [`Dabbler.Security.Blazor`](../src/Libs/Dabbler.Security.Blazor/README.md)
-   [`Dabbler.Testing.Blazor`](../src/Libs/Dabbler.Testing.Blazor/README.md)

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

- **Major completeness/style miss:** the response is substantially shorter than the requested **~300-500 lines** for a canonical overview. It reads more like a concise summary than the substantial entry-point doc the task asked for.

- **Partial coverage of shipped surfaces:** several libraries are described accurately but not completely enough for a first-read overview. Notably:
  - `Dabbler.Components` omits a number of shipped primitives called out in the source (`MasterDetailExpansionShell`, `FilteringCoordinator`, `FilterColumnDefinition`, `FilterGroup`, `CompositeFilterDescriptor` / `FilterDescriptor`, `FilterSummaryBuilder`, `ExpressionEngine`, `IGridDataProvider<T>`, and the legacy `FilterPanel<T>` note).
  - `Dabbler.Security` compresses the baked-in role model too much (`SystemAdmin`, `CrossOrgUserAdmin`, `CrossOrgMonitor`, `UserAdmin`, `User`) and doesn’t clearly distinguish baked-in roles from user-type roles with the same precision as the source.
  - `Dabbler.Reports.Core` and `Dabbler.Reports.Blazor` are accurate but light on the concrete supported contract that the source README explicitly names.

- **Adoption path is a bit underdeveloped:** it does say the platform is consumed from integrated source under `src/Libs/` today and that NuGet packaging is future work, which is correct. But given the task emphasis, that distinction should be more explicit and central rather than a short paragraph.

- **What looks correct:** I did not find invented APIs or obviously false claims. The stub-mode story, accessibility posture, dev-only `Dabbler.Security.DevIdp`, and required cross-links are all present and broadly aligned with the source.
