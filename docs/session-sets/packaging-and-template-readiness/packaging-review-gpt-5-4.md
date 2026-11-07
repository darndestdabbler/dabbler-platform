## 1. One-line verdict

**`proceed-with-adjustments`** — the package split and template direction are broadly right, but the proposal still does not give outside consumers a clean day-1 path because the pack process is not yet hermetic, the validation gate is too shallow, and the public/external story is still under-specified.

## 2. Strengths

- **The package decomposition in §2 is defensible.** The split between `Components`, `Security`, `Security.Blazor`, and `Reports.*` matches real adoption seams instead of repo convenience.
- **§4 gets stub behavior right.** Making stubs explicit opt-in and refusing silent production fallback is the correct contract.
- **§5 makes the right template call.** Templates referencing published NuGet packages instead of project references is essential if this work is meant to prove external consumption.
- **§3.3 lock-step versioning is the right pre-1.0 tradeoff.** It removes a whole class of cross-package skew failures while the surface is still moving.
- **§6 correctly identifies consumer-install validation as a separate concern from normal test coverage.** That is the highest-value testing addition in this proposal.
- **§7 is disciplined.** The mapping from Session 4 carry-overs to concrete packaging actions is clear and reviewable.

## 3. Adjustments

- **Severity: Critical**  
  **Section: §3.4 `PackAsDependency` MSBuild conditional**  
  **Proposed change:** Make the pack pipeline hermetic. The current `tools/pack.ps1` must create and use an explicit temporary local feed for restore, point `RestoreSources`/`RestoreAdditionalProjectSources` at it, isolate `NUGET_PACKAGES` to a temp folder, and fail if any `Dabbler.*` package resolves from an external source or an old cache.  
  **Why it matters:** As written, the pack flow can restore stale published packages or simply fail unpredictably. That is not a packaging story; it is a trap.

- **Severity: Critical**  
  **Section: §6.1 consumer-install validation strategy**  
  **Proposed change:** Expand the smoke to exercise actual platform value, not just shell startup. For `dabbler-app`, the gate must log in, load the sample secured query page, render the sample report page, and execute one report export path; it should also run the scaffolded bUnit test project. For `dabbler-min`, it must hit a secured page, not just the home page.  
  **Why it matters:** A home-page render proves almost nothing. Consumers are adopting Dabbler for security, reports, and test/stub patterns; the package-validation gate must prove those paths work through NuGet.

- **Severity: Major**  
  **Section: §3.7 what is NOT exported as public API**  
  **Proposed change:** Do not ship the compatibility surfaces in `Dabbler.Components` at all. Move them to a separate `Dabbler.Components.Compat` package or exclude them from packaged output until a real consumer needs them.  
  **Why it matters:** `[Obsolete]` inside the main package still exports dead APIs, pollutes IntelliSense, expands the public-surface baseline, and contradicts §3.7’s own claim that these are not part of the public packaged contract.

- **Severity: Major**  
  **Section: §3.3 versioning policy**  
  **Proposed change:** Remove the intermediate “`0.x.0` without preview suffix” phase. Stay on `0.x.y-preview.N` for all public releases until Session 9 consumer validation is complete and the external docs/templates are fully tightened, then cut `1.0.0`.  
  **Why it matters:** A non-preview package is effectively a maturity signal. Shipping “stable-looking” pre-1.0 packages before the external path is proven will mislead adopters.

- **Severity: Major**  
  **Section: §3.1 common metadata, §7 carry-overs**  
  **Proposed change:** Make package README structure a required deliverable, not an implied cleanup. Every package README should have: audience, prerequisites/stack constraints, `dotnet add package` command, minimal registration snippet, smallest working example, related packages, and warnings/caveats. Add one repo-level “Which package do I install?” matrix for common scenarios.  
  **Why it matters:** §2 is library-centric. Real consumers think in scenarios: “I want the full platform”, “I want reports only”, “I want components + security”. Without scenario guidance, the package set is noisy.

- **Severity: Major**  
  **Section: §1 executive summary, §2.1, §3.1**  
  **Proposed change:** Formalize third-party license disclosure for the report stack. Ship a `THIRD-PARTY-NOTICES.txt` (or equivalent) in `Dabbler.Reports.Core`, `Dabbler.Reports.Export`, and `Dabbler.Reports.Blazor`, and surface the same warning in the `dabbler-app` template README.  
  **Why it matters:** README-only disclosure is too weak for enterprise review. If the report path pulls `DabblerPdf`, that needs to be explicit in the artifact itself.

- **Severity: Major**  
  **Section: §10.1 risks, §10.2 rollback paths**  
  **Proposed change:** Replace the “atomic publish” / “delete from nuget.org within 24h” rollback model with staged promotion. Publish first to a staging feed, validate install from that feed, then push to nuget.org in dependency order; if public push fails mid-stream, unlist and publish a fixed version.  
  **Why it matters:** nuget.org is not transactional, and deletion is not a safe rollback plan. Bad public versions are effectively permanent.

- **Severity: Major**  
  **Section: §2.2 `Dabbler.Security.DevIdp` does NOT pack, §8 Q3**  
  **Proposed change:** Remove “reference the source under `src/Libs/`” as the external guidance. Provide an external dev-auth story: container image, runnable sample app, or `dotnet tool`, and link it from the template docs.  
  **Why it matters:** Telling outside consumers to go back to repo source is exactly the repo leakage this session is supposed to eliminate.

- **Severity: Major**  
  **Section: §6.2 packaged-contract check**  
  **Proposed change:** Add artifact-content assertions alongside public-surface assertions. Validate `.nupkg`/`.nuspec` contents for readme, icon, license expression/notices, SourceLink metadata, dependency groups, and static-web-asset completeness.  
  **Why it matters:** Build success is not package quality. A package can restore and still be externally broken or incomplete on nuget.org.

## 4. Risks the proposal underweights

- **Hermetic restore risk is missing from §10.** The biggest implementation risk is not “pack fails on metadata gaps”; it is “pack succeeds against the wrong package source or stale cache” because of the §3.4 `PackAsDependency` pattern.
- **Third-party license/compliance risk is underplayed.** §1 and §2 treat `DabblerPdf` disclosure as a small metadata note. For many adopters, that is a release blocker, not a README footnote.
- **Compatibility drift with MudBlazor / W0 is not treated as a release risk.** §8 Q4 leaves the compatibility matrix open, but §10 should already recognize that upstream UI stack changes are a primary external-breakage vector.
- **Template security defaults being copied into production is underweighted.** §5.2 correctly says the template is not production-grade, but §10 does not treat “consumer cargo-cults dev defaults” as a real adoption risk.
- **Public-surface pollution from compat APIs is underweighted.** If the obsolete compatibility types ship in the main package, that becomes a long-tail maintenance burden immediately.
- **Rollback assumptions are too optimistic.** §10 treats nuget.org deletion as a normal escape hatch. It is not.

## 5. Out-of-scope items the proposal incorrectly defers

**None from §9 are clearly misplaced.** The §9 non-goals are mostly reasonable.

The problem is elsewhere: **§8 Q4 (compatibility matrix)** and **§2.2 / §8 Q3 (external DevIdp story)** should not stay as open/deferred items. Those are day-1 external-consumer concerns and belong inside this session set.

## 6. Anything missing entirely

- **Template first-run auth/bootstrap story.** §5.1 says `dabbler-app` and `dabbler-min` scaffold Security with cookie + JSON store, but the proposal never says how the first user signs in, what credentials exist, or how the initial admin/bootstrap flow works.
- **Template sample-data story.** §5.1 promises one query page and one report, but it does not define where their data comes from on first run. A scaffold that builds but shows empty/broken sample pages is a bad first impression.
- **A retrofit/adoption path for existing apps.** §2 gives package boundaries and §5 gives new-app templates, but there is no concrete answer for “I already have a Blazor Server app; what exact package set and registration order do I use?” The proposal needs that, and it should explicitly decide whether a thin umbrella package exists or not.