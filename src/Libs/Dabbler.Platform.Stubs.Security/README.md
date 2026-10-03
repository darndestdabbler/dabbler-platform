# Dabbler.Platform.Stubs.Security

Stub authentication-state provider for Dabbler bUnit and demo-host consumers.

Provides `AddStubAuthentication` for wiring a mutable in-memory `ClaimsPrincipal` without a real IdP.
Compatible with any Blazor Server host that uses `AuthenticationStateProvider` — no dependency on `Dabbler.Security` itself.

## Audience

External adopters and integrated-source users who write bUnit tests or demo hosts that need a controllable auth state.

## Prerequisites

- .NET 10 (uses `Microsoft.AspNetCore.Components.Authorization` via `Microsoft.AspNetCore.App`)

## Install

Until preview ships: reference `src/Libs/Dabbler.Platform.Stubs.Security/` from your test or demo project.
After preview ships: `dotnet add package Dabbler.Platform.Stubs.Security --prerelease`.

## Minimal registration

```csharp
services.AddStubAuthentication(new StubUserContext
{
    UserId = "test-user-1",
    DisplayName = "Test User",
    Email = "test@example.test",
    Roles = ["User"]
});
```

## Related packages

- `Dabbler.Security` — the production authorization library this stub supports in tests
- `Dabbler.Platform.Stubs` — meta-package; install this for full-platform stub coverage
- `Dabbler.Platform.Stubs.Components` — Components stubs
- `Dabbler.Platform.Stubs.Reports` — Reports stubs
