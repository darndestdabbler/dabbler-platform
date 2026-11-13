**Recommendation: Option A — publish as `dotnet tool` packages on nuget.org.**  
**Runner-up: Option C.**

### Why A
This is the only option that both:

- removes the current clone/build friction for **hundreds of consumer repos/CI lanes**, and
- stays aligned with the repo’s **already-locked feed architecture**.

These are **build-time commands**, not runtime libraries. Packaging them as tools matches their actual use, matches the existing `Dabbler.Security.DevIdp.Tool` precedent, and gives consumers a deterministic install surface on anonymous-read nuget.org.

### Why not B
Option B publishes the wrong shape. Ordinary NuGet packages blur the repo’s deliberate distinction between:

- **runtime libraries** (`<PackageReference>`)
- **developer/build-time commands** (`dotnet tool`)

That is hard to unwind once public IDs exist. It would “work,” but it would permanently strain the architecture.

### Why C is runner-up
C is operationally worse, but architecturally safer than B because it avoids minting the wrong permanent public surface. If the team were not ready to freeze public IDs, C would be the fallback. But it does **not** solve the stated scaling problem.

---

## Sub-decisions for Option A

### 1) One tool or two?
**Publish two tools.**

- **`Dabbler.CrudSlice.Tool`** — the front-door tool used by the orchestrator’s Step 7
- **`Dabbler.SchemaApplicator.Tool`** — separate standalone tool for re-stamping existing scaffolds

#### Trade-off
- **Cost:** one extra permanent nuget.org package ID and command; some duplicated shipped bits because CrudSlice already references SchemaApplicator in-process.
- **Benefit:** preserves the stated reality that SchemaApplicator is an **independent supported generator**, not just an internal implementation detail, and gives a clean **1:1 mapping from published artifact back to `tools/...` source**.

Given the traceability constraint and the fact that SchemaApplicator is explicitly a standalone tool, I would not hide it behind CrudSlice only.

That said, the **named consumer** only needs `CrudSlice.Tool` for Step 7. Publishing both does **not** mean every consumer installs both.

---

### 2) Naming
Use the existing tool precedent exactly:

- **PackageId:** `Dabbler.CrudSlice.Tool`
- **Command:** `dabbler-crudslice`

- **PackageId:** `Dabbler.SchemaApplicator.Tool`
- **Command:** `dabbler-schemaapplicator`

Reasoning: the repo already uses `.Tool` to mark “never a `<PackageReference>`” artifacts, and `dabbler-devidp` shows the command should be a short, branded executable name rather than the full namespace.

For traceability, the mapping is clean:

- `tools/.../Dabbler.CrudSlice` → `Dabbler.CrudSlice.Tool`
- `tools/.../Dabbler.SchemaApplicator` → `Dabbler.SchemaApplicator.Tool`

Update `platform-output-contract.md` to record that mapping explicitly.

---

### 3) Versioning
**No new scheme.** Both tools ride the single shared `DabblerVersion` exactly as locked.

Consumers should pin tools reproducibly via a **local tool manifest**, not global install.

Example:

```bash
dotnet new tool-manifest
dotnet tool install Dabbler.CrudSlice.Tool --version 1.2.3-preview.4
dotnet tool install Dabbler.SchemaApplicator.Tool --version 1.2.3-preview.4   # only if needed directly
dotnet tool restore
```

Then invoke via:

```bash
dotnet tool run dabbler-crudslice ...
```

This is deterministic, source-controlled, and CI-friendly.

Operationally, this just means adding the tool projects and tool PackageIds to the existing two pack/publish allowlists and solution filter.

---

### 4) `dotnet new` templates dependency
For the **full CrudSlice path**, a clean machine must have:

1. **.NET 10 SDK**
2. **`Dabbler.CrudSlice.Tool`** installed/restored at an exact version
3. **`Dabbler.Templates`** installed at the **same exact version**

Example:

```bash
dotnet new install Dabbler.Templates::1.2.3-preview.4
```

`SchemaApplicator.Tool` is **not required** for CrudSlice’s normal execution, because CrudSlice runs it in-process.

#### Implication for the clean-machine validation gate
The acceptance proof should be:

- on a fresh machine/agent,
- with **no source checkout of the platform**,
- with **no authenticated feed access**,
- install/restore the tool by exact version,
- install the templates by exact version,
- run the full Step 7 flow successfully.

Also validate the failure mode: if templates are missing or version-skewed, CrudSlice should fail fast with a clear message telling the user exactly what to install.

---

## Risks / failure modes and mitigations

### 1) Permanent public IDs
**Risk:** nuget.org IDs are hard to reverse.  
**Mitigation:** use the existing project names plus `.Tool`; do not invent broader or vaguer names.

### 2) Tool/template version skew
**Risk:** CrudSlice shells out to templates; mismatched versions can generate incompatible output.  
**Mitigation:** require consumers to pin the **same shared `DabblerVersion`** for tool and templates; validate this in the clean-machine gate; ideally fail with explicit guidance.

### 3) Missing templates on clean machines
**Risk:** tool installs successfully, then execution fails later at `dotnet new`.  
**Mitigation:** CrudSlice should preflight-check template availability and emit the exact `dotnet new install Dabbler.Templates::<version>` command.

### 4) SDK/runtime mismatch
**Risk:** framework-dependent .NET 10 tools require the right SDK/runtime.  
**Mitigation:** document `.NET 10 SDK required`; the clean-machine gate should prove this path.

### 5) Dual-surface maintenance
**Risk:** publishing SchemaApplicator separately adds one more supported surface.  
**Mitigation:** shared versioning and single-source projects keep drift low; document intended roles clearly: CrudSlice = front door, SchemaApplicator = focused restamping tool.

---

## Consistency with the locked architecture
**Fully consistent.**

Option A fits all locked decisions:

- **nuget.org anonymous read**
- **trusted publishing**
- **single shared version scheme**
- **explicit tool-vs-library distinction**
- **existing precedent for `dotnet tool` publication**

By contrast, **Option B strains** that architecture by publishing build-time commands in a library-shaped form.

## Bottom line
Publish them as **dotnet tools**, with **two tool packages**:

- `Dabbler.CrudSlice.Tool` / `dabbler-crudslice`
- `Dabbler.SchemaApplicator.Tool` / `dabbler-schemaapplicator`

Use the existing shared `DabblerVersion`, require exact version pinning, and make the clean-machine proof include **both tool restore and template install**. This is the right permanent shape.