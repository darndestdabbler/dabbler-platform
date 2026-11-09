# Dabbler.Security.DevIdp.Tool

`dabbler-devidp` is a loopback development identity provider for Dabbler
platform apps. It runs an OpenIddict-backed OIDC server on
`https://localhost:5100` that issues Entra-ID-compatible tokens, so an app
can sign in locally without a real cloud IdP. It is for development only.

## Install and run

```bash
dotnet tool install --global Dabbler.Security.DevIdp.Tool
dabbler-devidp
```

| Option | Default | Meaning |
|---|---|---|
| `--urls <url>` | `https://localhost:5100` | Listen URL. |
| `--environment <name>` | `Development` | ASP.NET Core environment name. |
| `--help` | | Show help and exit. |

| Configuration | Default | Meaning |
|---|---|---|
| `ConnectionStrings__DevIdp` | `Data Source=devidp.db` | SQLite connection string. |
| `DevIdp__TenantId` | `00000000-0000-0000-0000-000000000001` | Mock tenant GUID issued in tokens. |

See [`docs/getting-started.md`](https://github.com/darndestdabbler/dabbler-platform/blob/master/docs/getting-started.md)
for wiring an app to it.

## If this library stops being maintained

This tool is MIT-licensed, and its source is in
[`tools/Dabbler.Security.DevIdp.Tool`](https://github.com/darndestdabbler/dabbler-platform/tree/master/tools/Dabbler.Security.DevIdp.Tool)
and the library it hosts,
[`src/Libs/Dabbler.Security.DevIdp`](https://github.com/darndestdabbler/dabbler-platform/tree/master/src/Libs/Dabbler.Security.DevIdp).
If it stops being maintained, copy that source into your own repository,
build the tool from your copy, and own it from there. Keep the MIT license
notice with the copied code.
