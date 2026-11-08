# Change Log: Composable CRUD Helpers (2026-05-13 to 2026-05-14)

**Purpose:** This session set focused on replacing the monolithic `ConfigurableMasterDetails<TEntity>` with smaller, composable helpers, eliminating inline dirty-state boilerplate, and extracting a declarative filter DSL.

## Overview

Replaced `ConfigurableMasterDetails<TEntity>` with a new set of composable helpers for managing dirty state, action rows, and declarative filtering. This streamlines CRUD operations across the application and improves maintainability.

## New Components

*   **`EditSession<T>`**: JSON-snapshot based dirty state tracking for entities. Provides `Live`, `IsDirty`, `NotifyChanged()`, `Undo()`, `Commit()`, `CommitFrom(fresh)`.
*   **`DirtyAwareActionRow`**: A configurable action row (Save/Cancel/Delete) that reacts to `IsDirty` state, with a `CustomActions` slot.
*   **`DeclarativeFilterPanel`**: Renders MudBlazor controls based on `FilterConfig<TEntity>`. Supports server-side URL query-string persistence and client-side in-memory filtering.
*   **`FilterConfig` / `FilterProperty` types**: Defines filter criteria (Text, Numeric, Date, Bool, EnumVal, ForeignKey).
*   **`LookupSource<TForeign>`**: Provides static or async data for foreign key lookups.

## Page Migrations

*   **Products (Sessions 1-2)**: `ProductDetail` migrated to `EditSession` and `DirtyAwareActionRow`. `ProductList` migrated to `DeclarativeFilterPanel` (6 filter properties). Existing Playwright tests remain unchanged.
*   **Orders (Session 3)**: `OrderDetail` migrated to `EditSession` and `DirtyAwareActionRow`, including custom status workflow buttons (Confirmed/Shipped/Delivered/Cancelled). `OrderList` migrated to `DeclarativeFilterPanel`.
*   **Customers (Session 4)**: `CustomerDetail` and `CustomerList` migrated.
*   **Nested 1:N OrderItems (Session 5)**: `MasterDetailExpansionShell` used for per-row `DirtyAwareActionRow` on `OrderItems`. Supports in-memory mutations and `ADD PRODUCT` picker, with a single server commit for the parent order and its full item list (`OrderService.UpdateAsync`).

## Test Coverage

*   **Component Tests**: Added 130 tests for `EditSessionTests` and `DirtyAwareActionRowTests`.
*   **API Integration Tests**: Added 5 tests for `OrderServiceMergeItemsTests`.
*   **Unit Tests**: Added 10 tests for `OrderDetailStateNestedItemsTests`.
*   **Playwright Tests**: Added `ProductsFilterQueryStringTests`, `OrdersMigrationTests`, `CustomersMigrationTests`, `OrderNestedItemsSaveTests`.
*   **Bug Fix**: `CustomersMigrationTests` `RunId` changed from `Guid` hex to `Random.Shared.Next` digits to avoid validation issues.

## UAT Checklist

Passed 10 items covering save lifecycle, undo, toggle availability, filter URL persistence (x3), status workflow, and nested item add/save. Solution compiles without warnings.

## Out of Scope

*   Order Discount semantics inconsistency (Web % vs API 0..1 fraction). Tracked as candidate: `order-discount-semantics-alignment`.