# Non-MVP Roadmap

The CRUD scaffolding templates focus on delivering a self-contained, immediate-value CRUD slice for a single entity. This document outlines features explicitly deferred to future work, organized by anticipated effort and dependencies.

## Reauthorization Gate

> **Granularity 3 and beyond items listed below are deferred until existing MVP templates have been used on at least 2-3 real entities by a downstream consumer. Reauthorization review required before beginning any deferred item.**

The intent of this gate is to prevent speculative investment in higher-granularity automation (auto-DI, auto-nav, reflection-driven discovery, regeneration, OData, master-detail) before the MVP templates have accumulated enough real-world consumption to ground a redesign in actual friction signals rather than imagined ones. The MVP shipped 2026-05-16 with the demo app's `Supplier` slice as the only consumer; that is one entity, by the team that built the templates. Two or three independent uses by downstream consumers — ideally engineers who did not author the templates — are the minimum bar before any item below is reauthorized.

The review should answer:

1. **Which MVP friction points are real?** (e.g., is the four-hand-edit integration burden actually noticed in practice, or is it a non-issue once a developer has scaffolded their second entity?)
2. **Which roadmap items, if any, address those friction points cleanly?** (a Granularity 3 auto-DI helper is a small ergonomic win if the manual `AddSupplierScaffolding()` call is the most common omission; it is a large investment for negligible payoff if developers report the four-hand-edit pattern is fine.)
3. **What is the smallest reauthorized scope?** (start with one item — the highest-value, lowest-risk — rather than reauthorizing the whole roadmap.)

Until the gate clears, items below remain frozen. New "Granularity 3 / Granularity 4 / reflection-discovery / drift-detector / OData / master-detail" work should be **declined** at spec-authoring time and the requester redirected back here.

## Deferred Items

### 1. Granularity 3: Automation of Registration and Navigation

**Scope:** Automatically generate and wire up:
- Dependency injection (DI) registration in `Program.cs`
- Navigation menu entries in the main layout or nav component
- Database `DbSet` registration in the EF `DbContext`
- Security policy registration in the authorization configuration

**Status:** Planned for a follow-on session set after Granularities 1 and 2 prove out in production.

**Rationale:**

- **Complexity:** Requires parsing and modifying existing C# and Razor files (Roslyn), which is beyond the scope of pure `dotnet new` file substitution.
- **Risk:** Roslyn-based codegen carries higher risk of mis-parsing or corrupting existing files. Better to ship with zero-risk manual registration and automate after gathering production experience with the Granularity 1+2 approach.
- **Value chain:** Developers are comfortable with four documented hand-edits (DbSet, DI, AutoMapper, nav). Automating these reduces cognitive load by ~20% but increases implementation complexity by ~3×. Only worth doing after templates stabilize in production use.

**Successor task type:** `dotnet-scaffolding-granularity-3` (Roslyn-based file augmentation).

---

### 2. Reflection-Based Entity Discovery and DbContext Introspection

**Scope:** A `dotnet tool` variant that discovers entity types, primary keys, and properties by reflecting over the compiled assembly, eliminating the need for the `.dabbler-entity.json` schema file.

**Status:** Deferred to a follow-on session set.

**Rationale:**

- **Limitation:** `dotnet new` is a CLI templating engine with no reflection capabilities. Adding reflection requires a separate entry point (`dotnet tool`).
- **Prerequisite:** Granularities 1 and 2 must be stable and production-proven before investing in the higher-fidelity discovery approach.
- **Trade-off:** The schema-file approach is transparent and explicit (developers see exactly what the scaffolder will do); reflection is magical but opaque. The explicit approach is better for a new feature.
- **Scaling:** Reflection-based discovery becomes valuable when scaffolding 10+ entities per project. For typical projects (5-7 entities), the schema file is acceptable friction.

**Successor task type:** `dotnet-tool-entity-discovery` or `dotnet-scaffolding-granularity-4`.

---

### 3. Schema Evolution and Drift Support

**Scope:** Tooling to detect when an entity's properties change between schema-file versions, and to semi-automatically update the scaffolded files to match:

- A `generate-to-temp` + `diff` workflow showing what changed
- A Roslyn analyzer that flags entity properties not represented in scaffolded code
- A `--regenerate` flag that updates generated files in-place (with safeguards for customizations)

**Status:** Deferred to a follow-on session set.

**Rationale:**

- **Pain point:** If an entity gains a new property, the developer must manually update the schema file, re-run the template, and re-apply the schema script. This is ~5 minutes of friction per property.
- **Mitigation:** For the first few iterations of an entity, the schema-file friction is acceptable. Once an entity is mature (stable property set), the schema file is rarely updated.
- **Implementation:** Detecting and preserving customizations (column rendering, validation, custom actions) during re-generation is complex. The "generate once, own the code" philosophy avoids this problem.
- **Future:** After a year of production use and enough entity-evolution examples, the team can design a regeneration strategy that balances safety and convenience.

**Successor task type:** `dotnet-scaffolding-schema-evolution`.

---

### 4. OData Template Suite

**Scope:** An alternative set of templates (`dabbler-crud-service-odata`, `dabbler-crud-page-odata`) that use OData instead of the QBE → LINQ filtering mechanism.

**Status:** Deferred pending OData adoption decision.

**Rationale:**

- **Requirement:** OData is a standard query protocol with broad tooling support (client libraries, query builders, etc.). If the platform adopts OData, a dedicated template suite is justified.
- **Constraint:** The current templates use the platform's QBE mechanism, which is simpler and sufficient for 95% of CRUD scenarios. Shipping both OData and QBE templates from day one would double maintenance burden and introduce choice paralysis.
- **Design:** OData templates would be a **separate suite** (`dabbler-crud-*-odata`), not a flag on the existing templates. This ensures no flag creep and clear semantics (pick one or the other, not both).
- **Timeline:** Only pursue if stakeholders explicitly request OData support.

**Successor task type:** `dotnet-scaffolding-odata-templates`.

---

### 5. Multi-Entity Slices (Master-Detail CRUD)

**Scope:** Templates that generate master-detail pages (one-to-many relationships), such as Orders → OrderItems, without hand-editing the detail component.

**Status:** Deferred; no near-term demand signal.

**Rationale:**

- **Complexity:** Master-detail interactions introduce cascading-delete concerns, foreign-key validation, and richer UX patterns (inline detail grids, expansion panels, etc.). A single template cannot cover all variants.
- **Guidance:** For the next 1-2 years, master-detail scenarios are authored by combining two single-entity scaffolds and hand-customizing the interaction. This is a viable pattern.
- **Maturity gate:** After single-entity scaffolding is proven in 10+ entities across the codebase, the team can study whether a master-detail template reduces friction meaningfully enough to justify the complexity.

**Successor task type:** `dotnet-scaffolding-master-detail`.

---

## Implied Design Constraints

These deferral decisions imply the following constraints for the current MVP:

| Constraint | Rationale |
|-----------|-----------|
| **No `--filter-mode` flag** | Prevents flag creep; OData is a separate template suite, not a flag. |
| **No `--regenerate` flag** | Avoids complexity of preserving customizations; templates are single-use. |
| **No reflection-driven property discovery** | Deferred to a `dotnet tool` variant; `dotnet new` is intentionally templating-only. |
| **No post-generation hooks** | Keeps templates pure file substitution; any post-processing is explicit scripting (like `Apply-{Entity}Schema.ps1`). |
| **Single-entity focus** | Multi-entity relationships are out of scope; combine two scaffolds for master-detail. |

---

## Path to Granularity 3

Once Granularities 1 and 2 are production-proven (estimated 2-3 months), the team can pursue Granularity 3 automation:

1. **Collect feedback** on the four hand-edit integration points (DbSet, DI, AutoMapper, nav).
2. **Assess friction** — is manual registration a real pain point, or acceptable overhead?
3. **Design the approach** — Roslyn AST modifications vs. regex-based marker insertion (the latter is riskier but simpler).
4. **Prototype** — author a `dotnet-scaffolding-granularity-3` session set exploring Roslyn-based automation.
5. **Measure** — compare time-to-completion for scaffolding + registration with the current manual approach.

If automation proves 3+× faster and maintains high safety, it becomes a candidate for a follow-on session set.

---

## See Also

- [README](README.md)
- [CRUD Service Template Documentation](crud-service-template.md)
- [CRUD Page Template Documentation](crud-page-template.md)
