## 1. Versioning scheme + consumer upgrade story

**Recommendation:** use **SemVer 2 prerelease tags of the form `0.1.0-preview.<n>`**, with the **full version coming from the git tag**.  
Publish tags should be exactly like:

- `nuget-v0.1.0-preview.1`
- `nuget-v0.1.0-preview.2`
- …
- later: `nuget-v1.0.0`

**Do not derive public package versions from CI run numbers alone.** Use the tag as the source of truth; CI only parses and applies it.

### Why this is the right choice
- It is the most standard prerelease shape on NuGet.
- NuGet ordering is correct and obvious:
  - `0.1.0-preview.2` < `0.1.0-preview.10`
  - `0.1.0-preview.10` < `0.1.0`
- It gives consumers a real, pin-able version they can reason about.
- It matches your desired tag-triggered publish model cleanly.

### Deterministic source of `<n>`
**Authoritative source: the git tag.**

CI should:
1. Trigger only on tags matching `nuget-v*`
2. Parse the SemVer from the tag
3. Set `/p:DabblerVersion=<parsed-version>`
4. Fail if the version is not a valid SemVer 2 prerelease/stable version

So the publish command is deterministic and rerunnable. A rerun of the same tag produces the same version.

### Important ordering/pitfall rules
- Use **dot-separated numeric prerelease identifiers**: `preview.1`, `preview.2`, `preview.10`
- **Never zero-pad**: not `preview.01`
  - Numeric prerelease identifiers with leading zeros are invalid SemVer
- **Never use mixed ad hoc labels** like `session5`
  - `session5` is not part of a monotonic public scheme
  - alphanumeric identifiers sort lexically, which gets confusing fast
- **Do not use `preview10`**
  - that becomes lexical, so `preview10` sorts before `preview2`

### Why not `-rc.<n>` now?
`rc.<n>` is the runner-up, but I would **not** start there. `rc` implies “API is basically frozen.” You are still in platform-formation mode, and the consumer base is about to explode into hundreds of repos. `preview.<n>` is the honest label.

If later you want a short stabilization phase, you can do:

- `1.0.0-rc.1`
- `1.0.0-rc.2`
- `1.0.0`

But don’t introduce `rc` until you actually mean it.

### Stable `1.0.0`
Drop the suffix only when all three are true:
1. the generated app template is supportable,
2. the app-dependency package graph is supportable,
3. you are willing for consumers to treat the platform as “normal” production dependencies.

Then publish **`nuget-v1.0.0`**. No suffix.

### Consumer upgrade story
For **hundreds of independently CI’d repos**, the under-weighted part is not just the feed; it is **version pinning discipline**.

Generated apps should:
- pin **exact package versions**
- ideally centralize them in **`Directory.Packages.props`**
- update by changing **one version** across all `Dabbler.*` packages

Do **not** use floating versions like:
- `0.1.*`
- `0.1.0-preview.*`

That is how you create surprise breakages across hundreds of repos.

---

## 2. Public-vs-internal package split

**Recommendation:** publish the **consumer-facing platform surface** and **exclude the stub/demo packages**.

### Explicit allowlist: publish these to nuget.org
**Runtime / app-consumed libraries**
- `Dabbler.Api.Querying`
- `Dabbler.Components`
- `Dabbler.Components.Compat`
- `Dabbler.Reports.Core`
- `Dabbler.Reports.Export`
- `Dabbler.Reports.Blazor`
- `Dabbler.Security`
- `Dabbler.Security.Blazor`
- `Dabbler.Testing.Blazor`

**Dev-time / entry-point packages**
- `Dabbler.Security.DevIdp.Tool`
- `Dabbler.Templates`

### Exclude from public push
- `Dabbler.Platform.Stubs`
- `Dabbler.Platform.Stubs.Components`
- `Dabbler.Platform.Stubs.Reports`
- `Dabbler.Platform.Stubs.Security`

### Rationale by category

#### Publish the app-dependency libraries
These are the actual supported public surface for generated apps. They are exactly what anonymous restore on nuget.org is for.

#### Publish `Dabbler.Components.Compat`
Yes, publish it publicly.

A compat shim only works if the repos that need it can restore it easily. For a migration-heavy estate, compatibility bridges are not “internal”; they are part of the upgrade story. Keep it public until you no longer need it.

#### Publish `Dabbler.Templates`
Yes, absolutely.

It is the public entry point:
```bash
dotnet new install Dabbler.Templates::<version>
```

It belongs on the same public feed as the runtime packages. Given your current single-version stamp, I would keep it on the **same version train** for now. Simplicity matters more than trying to optimize template cadence early.

#### Publish `Dabbler.Security.DevIdp.Tool`
Yes, publish it.

A 20 MB `dotnet tool` is not a problem on nuget.org. The benefit is much larger than the cost:
- easy install/update
- works in CI/dev boxes without side-loading
- consistent public story

If it is part of the supported dev workflow, it should be distributed like a supported dev tool.

#### Exclude the `Platform.Stubs*` packages
Do **not** publish these publicly yet.

Why:
- They are not core app dependencies.
- They look like internal composition/testing aids.
- Public IDs are permanent, and these names create a support contract whether you want one or not.
- They invite misuse in production apps because “it’s on nuget.org, so it must be supported.”

If later you decide there is a real public testing-host story, publish a deliberately designed public package set then. Don’t fossilize the current internal stub topology on nuget.org.

### Risk of this split
The downside is that some external consumers may want the stub packages for bUnit or demo hosts.

That is still the right trade. You already have `Dabbler.Testing.Blazor` as the public testing surface. Keep the public story intentional.

---

## 3. Local `packages/` fallback in the template

**Recommendation:** **remove it entirely from the template.**  
Do **not** ship a local `packages/` source or a checked-in repo `nuget.config` that points to copied `.nupkg` files.

### Why
For the “hundreds of repos” target, the biggest risk is **stale local packages silently diverging from the real feed**.

That creates:
- “works on my machine” restores
- CI/dev mismatch
- upgrades that appear to succeed but restore old bits
- impossible-to-explain package provenance

This is worse than the current friction because it becomes invisible.

### The footgun is real
Once both a local folder source and nuget.org can satisfy `Dabbler.*`, you are in ambiguity territory. Even with `packageSourceMapping`, dual-homing the same package family is fragile:
- map `Dabbler.*` to both sources → shadowing risk remains
- map only to local → nuget.org is bypassed
- map only to nuget.org → the local fallback is pointless

For default generated repos, **one authoritative source** is the right answer.

### What the template should generate instead
Generate either:
- **no repo-level `nuget.config` at all**, or
- a **minimal one** that names only nuget.org

I recommend the minimal one for determinism:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

That makes restore behavior explicit and prevents inherited local junk from interfering.

### What to do for offline/air-gapped exceptions
Support offline restore as an **opt-in documented procedure**, not as template default.

Examples:
- a separate `NuGet.offline.config` sample
- a script that creates a local folder feed for emergencies
- an enterprise mirror/proxy if a customer truly needs offline/air-gapped CI

But that should be **out-of-band** and **not committed by default** in every generated repo.

### Risk
You lose turnkey offline restore for the rare exceptional case.

That is acceptable. The whole point of moving to nuget.org is to stop forcing the exceptional case on every repo.

---

## 4. Publish-pipeline mechanics

**Recommendation:** use a **two-stage, tag-triggered pipeline** with a **protected publish environment** and **artifact promotion**, and push with `--skip-duplicate`.

### High-level flow

#### Trigger
On a tag:
- `nuget-v0.1.0-preview.1`
- `nuget-v1.0.0`

#### Stage 1: validate + pack
Build once, pack once, and treat those artifacts as immutable.

Validation should include:
1. parse and validate the SemVer from the tag
2. verify the package ID allowlist
3. build with deterministic CI settings
4. pack with `/p:DabblerVersion=<tag-version>`
5. run tests
6. run a **consumer smoke test**
   - install `Dabbler.Templates` from the produced artifact
   - generate a sample app
   - restore/build/test against a temporary local feed containing the produced packages

That smoke test is the under-weighted guardrail. With hundreds of consumer repos, your publish pipeline must test the **actual consumer path**, not just library compilation.

#### Stage 2: publish
A separate job publishes **the exact artifacts from Stage 1** to nuget.org after approval.

This matters because `--skip-duplicate` only helps if a rerun is pushing the **same bits**. Do not rebuild on rerun.

### Idempotent/safe reruns
Use:
```bash
dotnet nuget push ... --skip-duplicate
```

Yes, this is the right call.

It makes partial failure recovery safe:
- package A already pushed? skipped
- packages B/C failed? rerun pushes remaining ones

### Important caveat on `--skip-duplicate`
It prevents failure on rerun; it does **not** let you fix a bad public package after the fact.

So pair it with:
- build-once/publish-artifact promotion
- protected approval before publish
- validation before first push

### Manual approval / draft gate
**Yes: gate the publish job behind a protected GitHub Environment.**  
I would do this for **every public push**, not just the first one.

That is the right bias for irreversible public publication. One approval click is cheap; a permanent bad package is not.

### Symbol packages
**Publish `.snupkg` symbol packages for the library packages.**

Also ensure:
- deterministic builds
- Source Link
- repository URL / commit metadata

That gives consumers usable debugging into `Dabbler.*`, which is valuable in a generated-app ecosystem.

I would not worry much about symbols for:
- `Dabbler.Templates`
- the public shape of the tool package

But for the libraries, yes, publish symbols.

### NuGet.org API key storage
Store the nuget.org API key as a **GitHub Actions Environment secret**, not a plain repo secret:
- Environment: `nuget-org-prod`
- Secret: `NUGET_API_KEY`
- Protection: required reviewers / manual approval

On the nuget.org side, create a key scoped as tightly as possible:
- package pattern: `Dabbler.*`
- push permission only

### Irreversibility guardrails before the first public push
Before first push, I would require all of these:

1. **Manual ID review**
   - search nuget.org for every intended ID
   - check for typos, naming inconsistencies, awkward long-term names

2. **Publish allowlist enforcement**
   - only the approved public package IDs can be packed/pushed

3. **Metadata validation**
   - license
   - README
   - icon
   - project/repository URL
   - package tags
   - authors/company
   - release notes source

4. **Package validation tooling**
   - enable SDK package validation where applicable
   - treat pack warnings as errors

5. **Consumer smoke test from produced artifacts**
   - template install
   - app generation
   - restore/build/test

6. **Protected publish approval**
   - no direct unattended first push

7. **After first successful publish: request NuGet prefix reservation for `Dabbler`**
   - not a blocker to first release
   - but do it immediately after

---

If I boil it down:

1. **Versioning:** `preview.<n>`, version comes from the tag, consumers pin exact versions centrally.  
2. **Public set:** publish the real runtime/testing/template/tool packages; **do not publish `Platform.Stubs*`**.  
3. **Template fallback:** **remove local `packages/` entirely** from the default template.  
4. **Pipeline:** tag-triggered, build once, validate hard, manual-approved publish, `--skip-duplicate`, publish symbols.

That is the cleanest path for a public, anonymous-restore ecosystem serving hundreds of generated repos.