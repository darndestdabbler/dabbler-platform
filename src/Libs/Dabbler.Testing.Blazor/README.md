# Dabbler.Testing.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Testing utilities for Blazor Server apps. Emphasis on deterministic
synchronization for Playwright E2E tests via an action-watermark
mechanism that exposes when components have settled after async work.

## Audience

Teams writing Playwright E2E tests against Blazor Server apps who are
frustrated by the click-then-arbitrary-wait pattern and want a
test-time signal that says "the click's handler finished AND the
re-render committed." This is the platform's answer to one of the
hardest E2E synchronization problems on Blazor Server.

## Prerequisites and stack constraints

- Blazor Server
- .NET 10 (current LTS)
- `Microsoft.AspNetCore.Components.Web`
- (Optional) Playwright for the E2E side; this library does not depend
  on Playwright but is designed to be observed by it

## Install

> **Adoption note (Session 5 packaging-and-template-readiness):**
> Supported adoption today is **integrated source** under
> `src/Libs/Dabbler.Testing.Blazor/`. NuGet preview lands at end of
> Session 7; the future install reads
> `dotnet add package Dabbler.Testing.Blazor --prerelease`.

## Minimal registration

```csharp
// Program.cs
using Dabbler.Testing.Blazor.Hosting;

builder.Services.AddTrackedActions(builder.Configuration);
```

Enable the tracker in `appsettings.Development.json`:

```json
{
  "Testing": {
    "ActionTracking": {
      "Enabled": true
    }
  }
}
```

It's `false` by default — so production deployments that happen to ship
this assembly carry zero runtime cost when not enabled.

## Smallest working example

### 1. Wire the JS shim into your app

In `App.razor`:

```razor
@inject IOptions<TrackedActionsOptions> Options

@code {
    private bool Enabled => Options.Value.Enabled;
}

@if (Enabled)
{
    <script src="_content/Dabbler.Testing.Blazor/dabbler-tracked-actions.js"></script>
}
```

### 2. Add a readiness probe

In `MainLayout.razor`:

```razor
@using Dabbler.Testing.Blazor.Components

<TestReadinessProbe />
```

### 3. Opt a component into watermarked actions

```csharp
public class OrderActions : TrackedComponentBase
{
    private async Task SaveOrderAsync()
    {
        await TrackAction("save-order", async () =>
        {
            await _service.SaveAsync(_model);
        });
    }
}
```

### 4. Use the watermark from Playwright

```csharp
// After clicking a button that triggers a handler:
await page.GetByTestId("save-order").ClickAsync();
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-actions-in-flight", "[]");
```

Or wait for initial readiness:

```csharp
await Assertions.Expect(page.Locator("html"))
    .ToHaveAttributeAsync("data-blazor-ready", "true");
```

## Configuration

| Setting | Type | Default | Notes |
|---------|------|---------|-------|
| `Testing:ActionTracking:Enabled` | `bool` | `false` | Set to `true` in Development; `false` in Production (safe no-op). |

## Features

- **Action watermark tracking** — DOM attributes (`data-actions-in-flight`,
  `data-last-action`, `data-render-version`) expose when Blazor
  components have settled after user actions.
- **Test readiness probe** — Signals when the initial page load is
  complete via `data-blazor-ready`.
- **`TrackedComponentBase`** — Opt-in base class for components that
  perform async work in event handlers.
- **JS interop shim** — Low-overhead JavaScript module that maintains
  watermark attributes.

## Related packages

- [`Dabbler.Components`](../Dabbler.Components/README.md) — typical
  consumer of the watermark (CRUD pages that drive grid refreshes from
  async handlers).
- [`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md) —
  admin pages that opt into watermarked actions for E2E coverage of
  permission-edit flows.
- [`Dabbler.Platform.Stubs`](../Dabbler.Platform.Stubs/README.md) —
  pairs with the watermark for bUnit tests that simulate async
  handlers against stub providers.

## Warnings and caveats

- **Watermark is opt-in, not automatic.** Components inherit
  `TrackedComponentBase` and wrap async handlers in `TrackAction(...)`
  to be observed; uninstrumented handlers are invisible to Playwright's
  watermark wait.
- **Animation waits (CSS transitions) are not render-correlated** and
  still require `Task.Delay()` with documentation.
- **External login pages (e.g., DevIdp) don't set
  `data-blazor-ready`** and require conditional handling in Playwright
  fixtures.
- **`TrackAction`'s JS-interop calls go through `TryInvokeJsAsync` and
  silently no-op if `IJSRuntime` is not yet dispatchable** (prerender
  boundary, intermediate render passes). Missing the watermark in
  those frames degrades to "no signal" rather than failing the page
  render. Without this guard, an early-fire of a `TrackAction`-wrapped
  event handler turns into an HTTP 500.
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).
