# CRUD Scaffolding Spike — Change Log

**Outcome**
This three-session planning spike successfully delivered a comprehensive design for a `dotnet new` template suite to scaffold CRUD pages, service endpoints, and feature slices within the Dabbler platform. The system will generate explicit, owner-customizable code, adhering to a "composition over configuration" principle. The spike resolved all six open design questions and established a clear, adjusted MVP scope based on tri-provider review, defining the gates for the subsequent implementation set.

**Session 1: Helper And Pattern Inventory**
This session cataloged existing Dabbler CRUD helpers such as `EditSession<T>`, `DirtyAwareActionRow`, and `DeclarativeFilterPanel<TEntity>`, along with established API patterns and reference pages. The key finding was a robust set of UI components and backend service patterns that the scaffolding must replicate, ensuring generated code is indistinguishable from hand-authored code.

**Session 2: Scaffolding Approach Proposal**
The design decisions centered on a `dotnet new` template suite offering three granularities: single Razor CRUD page (MVP), service endpoint trio (MVP), and full feature slice (Post-MVP). This session also articulated the "composition over configuration" principle, the QBE-only filtering strategy, and the no-regeneration stance, resolving the initial open design questions. The proposal was authored with full orchestrator context.

**Session 3: Tri-Provider Review And Implementation Spec**
The tri-provider review (Opus self-review, Gemini Pro, GPT-5.4) resulted in a "NEEDS REWORK" verdict, primarily due to issues with MVP self-sufficiency, a placeholder QBE filter, and conflicts between `dotnet new` semantics and proposed post-generation reflection. The BLOCKING adjustments included requiring MVP to be a self-sufficient, compiling slice, mandating working QBE filter generation, unifying transport contracts, and exclusively using schema-file/explicit-parameter entity discovery for `dotnet new`. Larger adjustments landed in the implementation-set spec, while smaller, specific code-quality issues were corrected inline.

**Resolved design decisions (6 of 6)**
1. **Scaffolding Granularities:** Three nested granularities, with Granularities 1 & 2 shipping in MVP.
2. **Scaffolding CLI Shape:** Pure `dotnet new` templates, no post-generation reflection hook in MVP.
3. **Filtering Approach:** QBE-only filters for MVP, with generated service code including a working `query.ApplyQbeFilter(...)` call.
4. **Idempotency & Regeneration:** No in-place regeneration; developers own the generated code.
5. **Entity Discovery:** Schema-file as primary per-property input for `dotnet new` in MVP.
6. **OData Roadmap:** OData as a separate template suite, never a `--filter-mode` flag.

**Open items for the implementation set**
The implementation set inherits seven BLOCKING and four SUBSTANTIVE adjustments as MVP gates. These are detailed in `docs/session-sets/crud-scaffolding/spec.md`.

**Cost summary**
The tri-provider review panel incurred a total cost of $0.3021. Opus self-contribution was orchestrator work and incurred no router cost.