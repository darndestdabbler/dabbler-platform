# Refresh response for `src/Libs/Dabbler.Testing.Blazor/README.md`

- Model: gemini-pro
- Input tokens: 1667
- Output tokens: 807
- Cost: $0.0102

---

```markdown
# Dabbler.Testing.Blazor

Testing utilities for Blazor Server applications, with emphasis on deterministic synchronization for Playwright E2E tests.

## Features

- **Action Watermark Tracking**: DOM attributes (`data-actions-in-flight`, `data-last-action`, `data-render-version`) that expose when Blazor components have settled after user actions.
- **Test Readiness Probe**: Signals when the initial page load is complete via a `data-blazor-ready` attribute.
- **TrackedComponentBase**: Opt-in base class for components that perform async work in event handlers.
- **JS Interop Shim**: Low-overhead JavaScript module that maintains the watermark attributes.

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
- Concurrent actions in the same component are tracked correctly but may require coordinated teardown in tests.

## Testing

See `tests/Dabbler.Testing.Blazor.Tests/` for bUnit tests verifying watermark behavior.

## References

- [AI-Led Session Workflow](../../../docs/ai-led-session-workflow.md)
- [Project Guidance](../../../docs/planning/project-guidance.md) (Testing Hierarchy section)
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

- **Stale/incorrect reference in References**: `docs/planning/project-guidance.md` is not the current authoring source of truth called out by platform truth. The doc should point consumers to `docs/planning/session-set-authoring-guide.md` instead, or omit the secondary guidance link entirely.
- **Likely stale example**: the limitation calling out **DevIdp** looks like legacy sibling-repo/test-fixture terminology. Given the integrated repo state and the removal of sibling-repo coupling, this should be generalized to “external login pages / external auth providers” unless DevIdp is still an explicit supported fixture.
- **Missing host-page context for the JS shim**: for a Blazor Server consumer, “conditionally load the JS shim in `App.razor`” is under-specified and potentially misleading. The doc should say exactly where the static asset is expected to be loaded so consumers don’t wire it into the wrong render surface.
