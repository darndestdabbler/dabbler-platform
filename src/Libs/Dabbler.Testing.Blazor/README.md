# Dabbler.Testing.Blazor

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Testing utilities for Blazor Server applications, with emphasis on deterministic synchronization for Playwright E2E tests.

## Features

- **Action Watermark Tracking**: DOM attributes (`data-actions-in-flight`, `data-last-action`, `data-render-version`) that expose when Blazor components have settled after user actions.
- **Test Readiness Probe**: Signals when the initial page load is complete via `data-blazor-ready` attribute.
- **TrackedComponentBase**: Opt-in base class for components that perform async work in event handlers.
- **JS Interop Shim**: Low-overhead JavaScript module that maintains watermark attributes.

## Getting Started

### 1. Enable in Development

Add to `appsettings.Development.json`:

```json
{
  "Testing": {
    "ActionTracking": {
      "Enabled": true
    }
  }
}
```

### 2. Register Services

In `Program.cs`:

```csharp
using Dabbler.Testing.Blazor.Hosting;

builder.Services.AddTrackedActions(builder.Configuration);
```

### 3. Wire into App

In `App.razor`, conditionally load the JS shim:

```razor
@inject IOptions<TrackedActionsOptions> Options

@code {
    private bool Enabled => Options.Value.Enabled;
}

<!-- In body -->
@if (Enabled)
{
    <script src="_content/Dabbler.Testing.Blazor/dabbler-tracked-actions.js"></script>
}
```

### 4. Add Readiness Probe

In `MainLayout.razor`:

```razor
@using Dabbler.Testing.Blazor.Components

<TestReadinessProbe />
```

### 5. Opt-in Components

Inherit from `TrackedComponentBase` and wrap event handlers:

```csharp
public class MyComponent : TrackedComponentBase
{
    private async Task SaveAsync()
    {
        await TrackAction("save", async () =>
        {
            await _service.SaveAsync(_model);
        });
    }
}
```

### 6. Playwright Tests

Use the watermark attributes to wait for renders:

```csharp
// After clicking a button that triggers a handler:
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
| `Testing:ActionTracking:Enabled` | bool | false | Set to true in Development; false in Production (safe no-op) |

## Limitations

- Animation waits (CSS transitions) are not render-correlated and still require `Task.Delay()` with documentation.
- External login pages (e.g., DevIdp) don't set `data-blazor-ready` and require conditional handling in Playwright fixtures.
- Concurrent actions in the same component are tracked correctly but require coordinated teardown in tests.

## Testing

See `tests/Dabbler.Testing.Blazor.Tests/` for bUnit tests verifying watermark behavior.

## References

- [AI-Led Session Workflow](../../../docs/ai-led-session-workflow.md)
- [Project Guidance](../../../docs/planning/project-guidance.md) (Testing Hierarchy section)
