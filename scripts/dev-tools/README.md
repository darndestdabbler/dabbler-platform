# scripts/dev-tools

Developer-only helpers for the AcmeCoffeeOnline `/test-auth/*` endpoints.
Both scripts require `Authentication:SeededSession:Enabled=true`, which is
validated as Development- or Test-environment only and rejected in
Production.

## Preferred UX: the in-app Dev menu

For interactive use, the **Dev** menu in the AcmeCoffeeOnline app bar is the
preferred developer surface. It appears when seeded sessions are enabled and
exposes three actions:

- **Quick Sign-In** — swap between seeded personas without logging out.
- **Session Info** — current session snapshot in a scrollable JSON view.
- **Magic Links** — dev email inbox with copy-link, open-in-new-tab, filter,
  and clear-inbox actions.

The menu is gated on the same `SeededSession.Enabled` option as the
endpoints, renders nothing in Production, and keeps the shared secret
server-side (no secret ships to the browser bundle).

## When to use these scripts

The PowerShell helpers remain for scenarios where the in-app menu is not
convenient:

- Headless inspection from a terminal during automated runs.
- CI or scripted diagnostics that cannot drive the browser.
- Quick snapshots while the app is running without needing to log in.

## Scripts

### `Get-SessionInfo.ps1`

Fetches `/test-auth/session-info` and returns the current session JSON.

```powershell
.\scripts\dev-tools\Get-SessionInfo.ps1
# or point at a non-default base URL / secret
.\scripts\dev-tools\Get-SessionInfo.ps1 -BaseUrl "http://localhost:5010" -Secret "acme-local-seeded-session"
```

### `Get-MagicLinks.ps1`

Fetches `/test-auth/magic-links`. Optional `-Email` filter (exact match) and
`-Clear` switch (empties the in-memory inbox after read).

```powershell
# All captured messages
.\scripts\dev-tools\Get-MagicLinks.ps1

# Only for a specific recipient
.\scripts\dev-tools\Get-MagicLinks.ps1 -Email james.smith@example.com

# Drain the inbox for a recipient after inspection
.\scripts\dev-tools\Get-MagicLinks.ps1 -Email james.smith@example.com -Clear
```

## Security notes

- Both scripts refuse to run against anything other than a loopback host.
- The `X-Test-Auth-Secret` header is required; default value matches the
  development `appsettings` entry. Override with `-Secret` when needed.
- The target endpoints are rejected at startup in Production regardless of
  whether the header is supplied.
