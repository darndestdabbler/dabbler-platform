# Health Endpoint Contract

Both application hosts expose a lightweight `GET /health` endpoint for startup
checks, smoke validation, and local diagnostics.

## Supported Contract

All health responses must return HTTP 200 with these JSON fields:

| Field | Type | Meaning |
|---|---|---|
| `service` | string | Stable service identifier: `web` or `api` |
| `status` | string | Current health state. The supported healthy value is `healthy` |
| `environment` | string | ASP.NET Core environment name for the running host |
| `version` | string | Build/version identifier from the host assembly |

The Web host also returns:

| Field | Type | Meaning |
|---|---|---|
| `deliveryMode` | string | Current auth delivery mode from `AppAuthenticationOptions` |

## Compatibility Notes

- Do not add session-specific breadcrumbs such as migration stage labels.
- Keep `environment` and `version` stable enough for automation and operators to
  compare across runs.
- Additive fields are acceptable when they represent durable diagnostics, but
  the supported fields above should not be renamed or removed without updating
  the contract doc and tests together.
