# Scaffolding Approach Review — gemini-pro

> **Session:** crud-scaffolding-spike Session 3 of 3 (tri-provider review)
> **Reviewer:** gemini-pro (google, model_id `gemini-2.5-pro`)
> **Date:** 2026-05-16
> **Routed via:** direct `call_model()` (route()'s auto-verifier on `architecture` would duplicate the manual tri-provider review).
> **Generation params:** {'thinking_budget': -1}
> **Cost:** $0.0398 (in=9788, out=2758, stop=end_turn, elapsed=44.8s)
> **Prompt:** `scaffolding-review-prompt.md` (this folder).

---

## Verdict
APPROVE WITH ADJUSTMENTS

## 1. Composition-over-configuration alignment
The proposal correctly aligns with the *Composition over configuration for CRUD page composites* principle. It avoids the rejected runtime composite pattern by focusing on code generation.

The key statement of intent is clear and correct: "the generator produces explicit, customizable code the developer owns, not a runtime configuration composite." The "generate once, own the code" philosophy directly supports this. The generated code uses the same à la carte Dabbler primitives (`EditSession<T>`, `<DeclarativeFilterPanel<TEntity>>`, etc.) that a developer would use manually, reinforcing the compositional model. This approach successfully avoids sneaking the rejected composite back in through a generator.

No concerns.

## 2. AI-led-development value
The proposal delivers significant value for AI-led development. The high-leverage action of adding a new entity and its corresponding UI/API is a perfect use case for this type of scaffolding. An AI orchestrator or developer can issue a simple command like `dotnet new dabbler-crud-slice --entity Shipment`, and the tedious boilerplate is handled instantly.

This allows subsequent AI-driven or human-driven iterations to focus on the valuable, entity-specific customizations mentioned in the proposal, such as "form validation, and entity-specific UI actions" and "business logic, validation rules, and non-standard endpoints." The "generate once, own the code" model fits this workflow well, as the generated code becomes a stable artifact for the AI or developer to modify in later steps.

No concerns.

## 3. Variation-axis trap analysis
The proposal demonstrates a clear understanding of the variation-axis trap and actively avoids it. The section "Filter Representation: QBE, Not OData" is the strongest evidence. It correctly identifies that supporting multiple filter representations was a primary flaw in the rejected runtime composite.

By quoting the spike spec—"validation against tri-provider review and documentation of reasoning so the implementation set does not relitigate"—and making an explicit, documented decision to use QBE only for the MVP, the proposal erects a strong guardrail against reintroducing this trap. The decision to make a potential OData implementation an "alternative template suite, not mixed into the same generated code" is the correct architectural choice.

No concerns.

## 4. Generated-code quality
The generated-code samples are realistic and high-quality, but they reveal unstated dependencies and one significant logical gap.

1.  **Hidden Dependencies in MVP Scope:** The `ProductList.razor` sample injects `IGridDataProvider<ProductDto>`, and `ProductDetail.razor` injects `ProductApiClient`. However, the proposal defers the implementation of these components (`ProductGridDataProvider.cs`, `ProductApiClient.cs`) to the post-MVP Granularity 3. This means that running Granularity 1 or Granularity 2 templates in isolation will produce code that does not compile or run, as these critical dependencies will be missing. This undermines the value of the granular MVP split.

2.  **Incomplete Service Logic:** The generated `ProductService.cs` contains a critical placeholder comment:
    ```csharp
    // TODO: Apply filter expression here (use QBE filter engine)
    ```
    This is a significant omission. The generator has reflected the entity's properties to create the UI; it should also generate the corresponding server-side filtering logic (e.g., a `switch` statement on property names or a simple expression builder) to make the `GetAsync` method functional out of the box. Leaving this as a TODO reduces the value of the scaffold.

3.  **Inefficient Sorting Implementation:** The `ProductService.cs` sample uses `query.OrderBy(e => EF.Property<object>(e, sort.Property))`. While functional, this pattern forces EF Core to use reflection-based property access, which can result in less efficient SQL query generation compared to a strongly-typed `OrderBy(e => e.PropertyName)`. For a scaffolding tool that aims to produce code "indistinguishable from what a careful developer would write," this is a minor quality shortfall.

4.  **Less Readable RenderFragment Syntax:** In `ProductDetail.razor`, the `CustomActionsFragment` is implemented using the raw `RenderTreeBuilder`:
    ```csharp
    private RenderFragment CustomActionsFragment => builder =>
    {
        builder.OpenComponent<MudButton>(0);
        // ...
    };
    ```
    While correct, this is less readable and harder for developers to customize than standard Razor syntax within the `@code` block.

## 5. CLI shape
The choice of `dotnet new` is correct and well-justified. The proposal's rationale regarding consistency, simplicity, discoverability, and customization support is sound. It correctly weighs the trade-offs against a Roslyn source generator (less suitable for one-time, customizable templates) and a separate `dotnet tool` (higher friction).

No concerns.

## 6. Idempotency / regeneration
The "generate once, own the code" stance is a strong, valid architectural choice that aligns with the composition principle and avoids complex merging logic. However, the proposal underestimates the friction of schema evolution.

The statement, "If a developer needs to re-scaffold (e.g., add a new field), they edit the generated file manually or delete and re-run the template" is dismissive of a common and painful workflow. Manually adding a new field requires touching the Razor list, detail form, filter config, service, DTO, and potentially the controller. This is precisely the kind of tedious, error-prone work a generator should help with. While a full regeneration story is rightly rejected, the proposal should acknowledge this friction point more directly and consider patterns (like C# partial classes) or roadmap items (like partial regeneration) to mitigate it in the future.

## 7. Entity discovery
The MVP plan for entity discovery is a blocking concern. The proposal states: "The template's post-generation script...loads the project's assembly, reflects over `Customer` and `CustomerDto`..." This approach is fragile and will fail in common development scenarios:

-   **Pre-build Scaffolding:** A developer often scaffolds code for a new entity *before* the project can be compiled successfully. The proposed workflow requires a successful build to exist first.
-   **Build Order Dependencies:** It creates an implicit dependency on the build order. The scaffolding command must be run after the entity-defining project is built.
-   **Multi-assembly Solutions:** The proposal does not specify how the tool would locate the correct assembly in a solution where entities, DTOs, and the web project live in different assemblies.

The deferred alternative of a schema file (`--schema customers.dabbler-entity.json`) is far more robust and should be considered the primary mechanism for the MVP.

## 8. MVP scope
The proposed split of Granularities 1 & 2 for MVP is logically flawed due to the dependency issues identified in section 4. As written, the MVP will not deliver standalone, working features. The UI layer (Granularity 1) depends on the API client and data provider (deferred to Granularity 3) to function.

To be valuable, the MVP templates must generate a self-contained, runnable slice of functionality. This means the `dabbler-crud-page` template must also generate the basic `ProductApiClient` and `ProductGridDataProvider` needed to connect to the service endpoints.

## 9. Non-MVP roadmap
The roadmap is generally well-structured. Deferring Granularity 3, OData, and multi-entity slices is pragmatic. However, it is missing a key item based on the concerns raised above.

-   **Missing Mitigation for Schema Evolution:** The roadmap should include an investigation into mitigating the friction of adding fields to an existing, scaffolded entity. This could involve exploring C# partial classes to separate generated code from user customizations, or a more advanced "partial regeneration" tool that can intelligently add new fields to existing files.

## Specific adjustment recommendations

### Adjustment 1: Resolve Inter-Granularity Dependencies in MVP
- **Severity:** BLOCKING
- **Concern:** The proposal's MVP scope is not self-sufficient. Granularity 1 (`dabbler-crud-page`) generates code that depends on components (`ProductApiClient`, `ProductGridDataProvider`) deferred to the post-MVP Granularity 3, rendering the generated UI non-functional in isolation.
- **Recommendation:** Adjust the MVP scope so that the `dabbler-crud-page` template also generates the minimal, necessary `ApiClient` and `GridDataProvider` implementations required to connect the UI to the API endpoints generated by `dabbler-crud-service`. These can be simple, un-customized implementations that fulfill the contract.
- **Acceptance signal:** A developer can run `dotnet new dabbler-crud-service` followed by `dotnet new dabbler-crud-page` for the same entity, and the resulting project compiles and runs without requiring any components from the deferred Granularity 3 slice.

### Adjustment 2: Make Entity Discovery Robust via Schema Files
- **Severity:** BLOCKING
- **Concern:** The proposed MVP entity discovery mechanism—"reflection over a loaded assembly"—is fragile. It fails in common pre-build scaffolding scenarios and has unaddressed complexities with build order and multi-assembly solutions.
- **Recommendation:** Elevate the "Schema File (Future)" approach to be the primary, recommended mechanism for entity discovery in the MVP. The `dotnet new` command should accept a `--schema <path-to-file.json>` parameter. The reflection-based approach should be demoted to a secondary, "best-effort" alternative with its limitations clearly documented.
- **Acceptance signal:** The final design documentation and `dotnet new` examples prominently feature the schema-file approach. The reflection method is documented as an alternative with explicit warnings about its prerequisites.

### Adjustment 3: Generate Complete Server-Side Filtering Logic
- **Severity:** SUBSTANTIVE
- **Concern:** The generated `ProductService.cs` contains a `// TODO` for filtering logic, which makes the scaffold incomplete and less valuable.
- **Recommendation:** The generator should use the information it has about the entity's properties (which it uses for the UI) to generate a functional, if simple, filtering implementation. This could be a `switch` statement on `filter.Property` that applies `.Where()` clauses for the scaffolded filterable fields.
- **Acceptance signal:** The generated `GetAsync` method in the service class contains working code that applies filters based on the `PagedRequest` input, not a placeholder comment.

### Adjustment 4: Address the "Add a New Field" Workflow
- **Severity:** SUBSTANTIVE
- **Concern:** The proposal's "generate once, own the code" stance dismisses the significant developer friction of updating scaffolded code when an entity model changes.
- **Recommendation:** In the "Idempotency and Regeneration" section, explicitly acknowledge this friction as a deliberate trade-off. In the "Non-MVP Roadmap," add an item to investigate future tooling or patterns (e.g., using C# `partial` classes, or a separate "add-field" tool) to ease this pain point.
- **Acceptance signal:** The proposal is updated with text acknowledging the trade-off, and the roadmap includes a new item for "Investigating Schema Evolution and Partial Regeneration."

### Adjustment 5: Improve Generated Code Readability
- **Severity:** MINOR
- **Concern:** The `CustomActionsFragment` in `ProductDetail.razor` uses raw `RenderTreeBuilder` syntax, which is less readable and maintainable than standard Razor.
- **Recommendation:** Modify the template to generate the `RenderFragment` using standard Razor component syntax within the `@code` block.
- **Acceptance signal:** The code sample for `ProductDetail.razor` is updated to show a more readable fragment, for example: `private RenderFragment CustomActionsFragment => @<MudButton ... OnClick="@(() => _session.Undo())">Undo</MudButton>;`.

## Endorsements (what the proposal got right)
- **Strong Principle Alignment:** The core design ("generate once, own the code") is an excellent interpretation of the "composition over configuration" principle.
- **Pragmatic CLI Choice:** Selecting `dotnet new` over alternatives is the right call for developer experience and discoverability.
- **Avoiding the Variation-Axis Trap:** The explicit, well-reasoned decision to standardize on QBE for the MVP and reject a multi-mode implementation is a sign of architectural maturity.
- **Clear and Logical Granularity:** The conceptual split into Page, Service, and Slice granularities is sound, even if the MVP implementation needs dependency adjustments.
- **Explicit Non-Goals:** The clear documentation of what is *not* being built (OData, regeneration, etc.) is crucial for keeping the implementation set focused.