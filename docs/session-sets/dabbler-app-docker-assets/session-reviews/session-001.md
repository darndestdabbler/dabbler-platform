# Session 001 Review — dabbler-app-docker-assets

**Date:** 2026-05-28  
**Orchestrator:** claude-sonnet-4-6  
**Verifier:** gpt-5-4 (OpenAI, cross-provider)  
**Verification cost:** $0.2198  

---

## Session Summary

Session 1 added complete Docker/compose infrastructure to the `dabbler-app` template. All deliverables from the spec were authored and smoke-tested.

### Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Mail service | Mailpit (`axllent/mailpit:latest`) | Actively maintained successor to MailHog; same ports 1025/8025; MailHog is unmaintained |
| Web port | 5010 | Spec requirement |
| Api port | 5020 | Spec requirement |
| SA password | `MSSQL_SA_PASSWORD` via `.env` | Parameterized to avoid hardcoded secrets in template |
| Docker image naming | `NameLower` template symbol | Docker requires lowercase repo names; `sourceName: DabblerApp` substitution preserves case |
| `--no-restore` on Web publish | Omitted | .NET 10 SDK lesson: `--no-restore` skips Blazor static-asset discovery pass |

### Unplanned Work

**NuGet packages not on nuget.org:** The `Dabbler.*` packages referenced by `DabblerApp.Web.csproj` are not published to nuget.org. Resolved by adding `nuget.config` + `packages/` local feed directory to the template. Consumers populate `packages/` from `dabbler-platform/pack-output/` before running Docker builds. A `.gitkeep` file keeps the directory tracked in the template.

---

## Deliverables Created

| File | Status |
|---|---|
| `docker/docker-compose.infra.yml` | ✅ Created |
| `docker/docker-compose.app.yml` | ✅ Created |
| `docker/.env.example` | ✅ Created |
| `src/DabblerApp.Web/Dockerfile` | ✅ Created |
| `src/DabblerApp.Api/Dockerfile` | ✅ Created |
| `src/DabblerApp.Web/appsettings.Docker.json` | ✅ Created |
| `src/DabblerApp.Web/Properties/launchSettings.json` | ✅ Created (port 5010) |
| `src/DabblerApp.Api/Properties/launchSettings.json` | ✅ Updated (port 5020) |
| `.gitignore` | ✅ Created |
| `.dockerignore` | ✅ Created (post-verification fix) |
| `nuget.config` | ✅ Created |
| `packages/.gitkeep` | ✅ Created |
| `.template.config/template.json` | ✅ Updated (NameLower symbol) |

**Program.cs changes:**  
- `src/DabblerApp.Web/Program.cs` — `/_health` endpoint with `.AllowAnonymous()`  
- `src/DabblerApp.Api/Program.cs` — `/_health` endpoint  

---

## Smoke Proof

```
dotnet new dabbler-app --name SmokeTest -o /tmp/SmokeTest
cp docker/.env.example docker/.env
# populate packages/ from pack-output/
docker compose -f docker/docker-compose.infra.yml up -d
docker compose -f docker/docker-compose.app.yml up --build -d
curl http://localhost:5010/_health   → {"status":"healthy"}
curl http://localhost:5020/_health   → {"status":"healthy"}
curl http://localhost:8025           → 200 (Mailpit UI)
```

Both images (`smoketest-api:local`, `smoketest-web:local`) built successfully. API healthcheck passed before web container started (correct `depends_on` chain). All three probes returned expected results.

---

## Verification Verdict

**VERIFIED** — gpt-5-4, no Critical or Major findings.

### Raw Verdict

Overall: the implementation shown is sound and the smoke result is believable. I do not see a blocking correctness issue in the Docker/compose setup as presented.

**Verified areas:** compose topology, Dockerfile .NET 10 Blazor practices, `NameLower` symbol, nuget.config/packages pattern, `/_health` endpoints, port consistency.

### Minor Findings

1. **API Dockerfile does not use nuget.config/packages/** — fine today (Api only references `DabblerApp.Shared`, no Dabbler.* packages). If Api gains Dabbler.* references in future, will need the same local feed setup. Noted; not addressed this session.

2. **Add `.dockerignore`** — `.git/`, `bin/`, `obj/`, `docker/.env` should be excluded from build context. **Fixed post-verification** by adding `.dockerignore` to template.

3. **`packages/` placeholder** — `.gitkeep` already in place; Docker `COPY packages/ packages/` is robust.

4. **`NameLower` character sanitization** — unusual project names with Docker-invalid characters could produce invalid image tags. Documented expected naming convention; no change made.

5. **`UseHttpsRedirection()` in Api under HTTP-only Docker** — may produce noisy logs. Optional hardening; not addressed this session.
