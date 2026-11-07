# Scaffolding Approach Review — gpt-5-4

> **Session:** crud-scaffolding-spike Session 3 of 3 (tri-provider review)
> **Reviewer:** gpt-5-4 (openai, model_id `gpt-5.4`)
> **Date:** 2026-05-16
> **Routed via:** direct `call_model()` (route()'s auto-verifier on `architecture` would duplicate the manual tri-provider review).
> **Generation params:** {'reasoning_effort': 'high'}
> **Cost:** $0.2623 (in=8931, out=16000, stop=max_tokens, elapsed=276.0s)
> **Prompt:** `scaffolding-review-prompt.md` (this folder).

---

## Verdict
NEEDS REWORK

## 1. Composition-over-configuration alignment
Mostly aligned, but not cleanly enough yet.

What the proposal gets right:
- It explicitly says the generator should emit `"explicit, customizable code the developer owns"`.
- It also repeats the right non-goal: `"No runtime configuration composite."`
- The rejection of `"No single 'super-scaffold'"` is directly aligned with the documented principle.

Where it starts to drift:
- The proposal also says the template will use `"A post-generation hook ... that calls reflection to discover the entity and DTO, then fills in method bodies and field bindings."`
- That is still generation-time, not runtime, so it is **not** the rejected `MasterDetailsConfig<TEntity>` pattern. But it does create pressure for the generator to become a large inference engine that decides page structure, transport contracts, bindings, auth, and filters all at once.
- The risk is highest in the deferred `"Granularity 3 (Full slice)"`, which includes `"data provider + API client + DI wiring + smoke tests + nav/security"`. That is a lot of app-shape variation in one scaffold.

So: this is **not sneaking the runtime composite back in**, but it is close to re-centralizing too many decisions in a generator. The safe path is to keep the generated units narrow, compile-ready, and convention-based, not option-driven.

## 2. AI-led-development value
There is real value here, but the current MVP shape leaves too much unfinished for it to be high-leverage.

Why it is valuable:
- New-entity CRUD is exactly the kind of repetitive work AI benefits from accelerating.
- Generated explicit code is a good fit for AI follow-up edits, because the AI can modify normal Razor/C# instead of opaque configuration.
- The proposal’s intended outcome — `"indistinguishable from what a careful developer would write by hand"` — is the right target for AI-era scaffolding.

Why the current version underdelivers:
- The workflow assumes `"The developer has already created Product and ProductDto"` and that the generator can reflect `"over a loaded assembly"`. That means the entity must already exist and build before scaffolding starts. That weakens fast iteration.
- The core filter path is not actually scaffolded end-to-end. The generated service contains `// TODO: Apply filter expression here (use QBE filter engine)`. For a QBE-first platform, that is a major missing piece.
- Granularity 1 is not runnable on its own, even though the proposal says `"Granularity 1 alone is valuable"`. The page sample injects `IGridDataProvider<ProductDto>` and `ProductApiClient`, but those are deferred or not generated in MVP.

Net: good AI-development premise, but the MVP needs to produce a buildable, wired, end-to-end path before it will feel high-leverage in practice.

## 3. Variation-axis trap analysis
The proposal avoids the trap in some important places, but there are still leaks.

Good:
- The QBE decision is disciplined. The proposal is explicit: `"Granularities 1 & 2 ship QBE-only."`
- It also correctly rejects `"Both QBE and OData"` in the same artifact.
- `"No single 'super-scaffold'"` is the right constraint.

Leaks / risks:
- The roadmap says: `"A separate template or configuration option supporting OData queries (e.g., --filter-mode odata)."`. The words **"configuration option"** and `--filter-mode` are exactly the kind of enum-style variation axis the proposal otherwise rejects.
- `"Granularity 3"` bundles `"DI registration, smoke tests, navigation, security policy definitions"`. Those are highly app-specific axes. If that later becomes one command with switches, the generator will recreate the rejected problem at codegen time.
- Reflection-based field inference can also become a trap if the team starts adding per-type/per-field switches for every special case.

Recommendation direction: separate templates, fixed conventions, very few parameters. Do not let one template grow toggles for filter mode, auth mode, transport mode, layout mode, or nav mode.

## 4. Generated-code quality
The samples are not yet at the level of `"indistinguishable from what a careful developer would write by hand."`

### Major issues

- **Granularity 1 depends on deferred Granularity 3 pieces.**
  - The list page injects `private IGridDataProvider<ProductDto> DataProvider { get; set; } = default!;`
  - The detail page injects `private ProductApiClient ApiClient { get; set; } = default!;`
  - But the roadmap says Granularity 3 adds `"ProductGridDataProvider"` and `"ProductApiClient"`.
  - So the MVP page scaffold is not self-contained. This is a hidden runtime/dependency requirement.

- **The detail-page sample likely does not compile as written because it mixes `@bind-Value` with `ValueChanged`.**
  - Example:
    ```razor
    @bind-Value="_record.Name"
    ...
    ValueChanged="@(() => _session.NotifyChanged())"
    ```
  - `@bind-Value` already expands to `Value`, `ValueChanged`, and `ValueExpression`. Supplying `ValueChanged` again is invalid.
  - The same pattern appears on `MudNumericField` and `MudCheckBox`.

- **This line is not realistic MudForm usage:**
  - ```csharp
    if (_form is not null && !await _form.Validate())
        return;
    ```
  - The common MudBlazor pattern is `await _form.Validate();` and then check bound validity state. As written, this assumes `Validate()` returns `bool`.

- **The service sample leaves the platform’s primary differentiator unimplemented.**
  - Quote:
    ```csharp
    // TODO: Apply filter expression here (use QBE filter engine)
    ```
  - Since the proposal’s core design decision is QBE-only, shipping a TODO here means the scaffolded list+filter story is not actually functional.

- **Multi-column sorting is wrong in the sample.**
  - Quote:
    ```csharp
    foreach (var sort in request.SortFields ?? Array.Empty<SortField>())
    {
        query = sort.Order == SortOrder.Ascending
            ? query.OrderBy(...)
            : query.OrderByDescending(...);
    }
    ```
  - Repeated `OrderBy` resets ordering; it should be first `OrderBy`, then `ThenBy`.

- **The transport contract is internally inconsistent.**
  - The UI sample builds a `GridDataRequest`.
  - The service sample expects `PagedRequest`.
  - The controller binds `[FromQuery] PagedRequest request`.
  - The proposal does not show the adapter between those shapes in MVP, and the page sample does not call the controller directly anyway.

- **QBE-over-HTTP is unresolved.**
  - The controller uses `[HttpGet]` with `[FromQuery] PagedRequest request`.
  - But the filter model is described as `CompositeFilterDescriptor`, i.e. a nested object graph. The proposal does not show how that is serialized into query-string form in a reliable way.

- **The controller sample has an auth/correctness ordering problem.**
  - In `CreateAsync`:
    ```csharp
    var product = await _service.CreateAsync(dto, cancellationToken);
    
    if (!_currentUserService.TryGetUserId(out var userId))
        return Unauthorized();
    ```
  - In `UpdateAsync`, the same pattern appears.
  - Unless `DabblerEndpointAuthorize` guarantees this can never happen, the generated code performs a mutation before the user check.

### Hidden dependencies / unstated assumptions

- The controller and service samples omit required `using` directives for ASP.NET Core, EF Core, AutoMapper, DTO namespaces, and Dabbler auth attributes. If the intent is to rely on global usings, that needs to be stated.
- The generated controller assumes `IPermissionService`, `ICurrentUserService`, and `DabblerEndpointAuthorize` already exist and are registered.
- Security is described as deferred in Granularity 3 (`"Security policy definitions"`), but the generated controller already references policy names like `"CreateProduct"` and `"DeleteProduct"`.

## 5. CLI shape
`dotnet new` is the right shape **only if** this stays a true template system.

Where the proposal is solid:
- Rejecting Roslyn source generators is reasonable for this scenario.
- The ownership model fits template-time generation better than build-time recurring generation.

Where the current design conflicts with `dotnet new`:
- The proposal says MVP will use a `"post-generation hook (ITemplateOperationProvider)"` that `"loads the project's assembly, reflects over Customer and CustomerDto, and emits ... field bindings."`
- That is no longer simple template expansion. It is closer to a custom code-generation tool hidden behind template packaging.
- `dotnet new` is good at file emission from declared parameters. It is much less compelling when the workflow needs project discovery, assembly loading, metadata inspection, contract generation, and code patching.

Cost tradeoff:
- **vs `dotnet tool`:** `dotnet new` gives discoverability, but a `dotnet tool` is a better fit for reflection, solution inspection, richer validation, and explicit project/assembly selection.
- **vs source generator:** the proposal is right to avoid source generators here.

My view: if you keep the dynamic discovery/hook model, a `dotnet tool` is the better CLI shape. If you keep `dotnet new`, strip the MVP back to deterministic file templates with explicit inputs.

Minor point:
- `"No new tool installation"` is overstated. A template package still has an install/update lifecycle, just via template installation rather than tool installation.

## 6. Idempotency / regeneration
The default call is right, but the workflow needs one more support mechanism.

Good:
- `"Generate once and own the code"` is consistent with composition-over-configuration.
- Rejecting in-place regeneration/merge logic is wise.

Concern:
- The proposal’s current answer to schema drift is effectively: edit manually, or `"delete and re-run the template"`.
- In an AI-heavy workflow, entities change often. That makes drift handling a recurring pain point.

Recommendation:
- Keep **no in-place regeneration**.
- But add a **non-destructive re-scaffold workflow**: e.g. generate to a temp/output folder for diffing, or provide an analyzer/checklist that flags new DTO/entity properties not represented in page/service code.
- That preserves ownership without pretending merge-safe regeneration exists.

## 7. Entity discovery
As written, this will not work reliably enough for real-world use.

Concerns grounded in the proposal:

- The design requires `"reflection over a loaded assembly"` and says:
  1. `"The developer has already created Product and ProductDto"`
  2. The template `"loads the project's assembly"`
- That means scaffolding now depends on:
  - the project already compiling,
  - the right target framework already building,
  - referenced assemblies being resolvable from the template host context.

- The CLI shown is too weak for multi-assembly solutions:
  - `dotnet new dabbler-crud-service --entity Customer --dto CustomerDto`
  - Real solutions often put EF entities, DTOs, API project, and web project in separate assemblies. The proposal needs explicit project/assembly/type inputs, not just short names.

- Pre-build scaffolding is explicitly deferred via the future schema-file option:
  - `"Deferred to a follow-on set after the MVP validates the core scaffold workflow."`
  - That excludes a common workflow: define shape, scaffold, then build.

- There is an internal contradiction:
  - Under rationale: `"The generator can also introspect DbSet<Product> via the DbContext"`
  - Later: `"DbContext introspection (future) ... Deferred."`
  - Those need to be reconciled.

- Assembly loading should be metadata-only, not normal runtime load.
  - The proposal currently says `"loads the project's assembly"`, which implies runtime loading semantics and associated dependency issues.

So: no, not reliably as written. Either simplify discovery dramatically for MVP, or move discovery into a tool built for it.

## 8. MVP scope
The split is conceptually right, but the actual MVP boundary is not.

What is right:
- UI scaffold and service scaffold are sensible separate units.
- Deferring multi-entity slices is correct.
- Deferring OData is correct.

What is wrong in the current split:
- The proposal claims `"Granularity 1 alone is valuable"` and `"Granularities 1 + 2 together deliver a complete CRUD feature"`.
- The samples do not support that claim:
  - Granularity 1 uses `ProductApiClient` and `IGridDataProvider<ProductDto>`.
  - Granularity 2 generates controller/service.
  - The proposal defers `ProductGridDataProvider`, `ProductApiClient`, and DI wiring to Granularity 3.
- That means 1 is not standalone, and 1+2 are not actually complete.

What should cross into MVP:
- At minimum, one end-to-end connection path:
  - either generate the minimal API client + grid data provider + registration snippets,
  - or redesign Granularity 1 so it depends only on stable abstractions already present in the platform and clearly document those prerequisites.
- Also bring the query/request contract decision into MVP. It is too foundational to defer.

What can stay deferred:
- Navigation
- Security policy definitions beyond basic hooks
- Smoke tests
- Multi-entity relationships
- OData
- Source-generator alternative

## 9. Non-MVP roadmap
A few items are missing, and one roadmap item should not be phrased the way it is.

Missing roadmap items:
- **Template validation harness.** There should be a documented test strategy that instantiates templates into a sample solution and verifies the output builds.
- **Drift support without regeneration.** A compare/diff workflow or analyzer belongs on the roadmap if not MVP.
- **Multi-assembly discovery strategy.** This is important enough to deserve explicit roadmap treatment if not solved in MVP.

Roadmap item that should be adjusted:
- The OData roadmap says `"A separate template or configuration option supporting OData queries (e.g., --filter-mode odata)."`.
- The **configuration option** part should be removed. If OData ever happens, it should be a separate template suite only.

Roadmap item that should move toward MVP:
- Minimal connector/wiring pieces needed to make Granularity 1 or 1+2 actually runnable should move into MVP, even if full slice orchestration remains deferred.

## Specific adjustment recommendations

### Adjustment 1: Make Granularity 1 actually runnable
- **Severity:** BLOCKING
- **Concern:** The MVP page scaffold depends on types the proposal explicitly defers: `IGridDataProvider<ProductDto>` in `ProductList.razor` and `ProductApiClient` in `ProductDetail.razor`, while the roadmap says Granularity 3 adds `"ProductGridDataProvider"` and `"ProductApiClient"`.
- **Recommendation:** Either:
  1. move minimal `ProductApiClient`, `ProductGridDataProvider`, and DI registration snippets into MVP, or
  2. redesign the page template to depend only on stable pre-existing abstractions and document them as prerequisites.
- **Acceptance signal:** A freshly scaffolded Granularity 1 output in the reference app builds and runs without hand-authoring deferred Granularity 3 artifacts.

### Adjustment 2: Pick one real generation mechanism
- **Severity:** BLOCKING
- **Concern:** The proposal wants a `dotnet new` template suite, but also relies on a `"post-generation hook (ITemplateOperationProvider)"` that loads assemblies and emits method bodies. That is much closer to a custom generator than a standard template.
- **Recommendation:** Decide explicitly:
  - **Option A:** keep `dotnet new`, but remove assembly-reflection/hook behavior from MVP and use explicit parameters only.
  - **Option B:** if reflection/discovery is essential, switch MVP to a `dotnet tool`.
- **Acceptance signal:** The proposal no longer relies on an unspecified template hook for assembly reflection, and the implementation plan matches the chosen mechanism.

### Adjustment 3: Define one end-to-end request contract for list/filter/sort/page
- **Severity:** BLOCKING
- **Concern:** The UI sample uses `GridDataRequest`, the service sample uses `PagedRequest`, and the controller expects `[FromQuery] PagedRequest`. The contract between generated layers is undefined.
- **Recommendation:** Standardize on one request/response contract across UI, API client/provider, controller, and service. Also choose and document the HTTP transport for QBE filters.
- **Acceptance signal:** The samples use the same request type end-to-end, and the proposal explains exactly how a `CompositeFilterDescriptor` crosses the wire.

### Adjustment 4: Generate working QBE support, not a TODO
- **Severity:** BLOCKING
- **Concern:** The service sample contains `// TODO: Apply filter expression here (use QBE filter engine)`, which makes the flagship QBE flow non-functional.
- **Recommendation:** Either generate a call into a reusable QBE→LINQ helper that already exists in Dabbler, or generate a minimal supported filter implementation for the scaffolded fields.
- **Acceptance signal:** The generated service/controller path supports filtering without manual TODO completion