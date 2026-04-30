_Task prompt templates — one section per routed task type._

This file contains the user-message template applied by `prompting.py`
for each routed task type. `config.py` parses the `#` (H1) headers; the
slug of each header becomes the key in `config["_task_templates"]` that
`prompting.py` looks up. H1 is used rather than H2 because the template
content itself begins with H2 headers that would otherwise collide with
the section-boundary parser.

> **Migration note (2026-04-30, `uat-workflow-dsl-alignment`).** The
> `uat-coverage-review` task type has been retired. Its job — verifying
> step-by-step parity between a UAT checklist and a hand-authored
> `*UatCoverageTests.cs` — became unnecessary once the DSL-driven
> pipeline (`uat-script-author` → W0 compile → W2 runner) made the
> checklist the compiler input and the Playwright `.cs` the compiler
> output. Parity is now structural; the W2 runner's deterministic pass
> is the coverage proof. The retired prompt section has been removed
> from this file along with the matching entries in
> `router-config.yaml`. Closed session sets that committed
> `*UatCoverageTests.cs` files retain them; new sets use the DSL
> pipeline. See `docs/ai-led-session-workflow.md` → *UAT/E2E pipeline*
> and *Migration: from manual `*UatCoverageTests.cs` pairing to the DSL pipeline*.

Placeholder contract — every template uses these two tokens and only
these two:

- `{content}` — the primary input for the task (the code under review,
  the subject to analyze, the objective to plan, etc.).
- `{context}` — any supporting material (surrounding files, existing
  patterns, specific questions). If the caller provides no context,
  `prompting.py` substitutes the literal string `(no additional context)`.

`prompting.py` does the substitution. Do not add other placeholders
here without updating the loader.

# analysis

## Analysis Task

Analyze the following and provide a structured assessment:

### Subject

{content}

### Specific Questions

{context}

### Response Format

Provide your analysis with clear sections, concrete recommendations, and supporting reasoning.

# code-review

## Code Review Task

Review the following code for:
1. Bugs and logic errors
2. Security vulnerabilities
3. Performance issues
4. Style and maintainability

For each issue found, provide:
- **Severity:** Critical / Major / Minor / Suggestion
- **Location:** File and line reference
- **Issue:** What's wrong
- **Fix:** Specific code change

### Code to Review

{content}

### Context

{context}

# documentation

## Documentation Task

Write documentation for the following code. Include:
1. Purpose and overview
2. Public API (classes, methods, parameters, return values)
3. Usage examples
4. Important notes or caveats

### Code

{content}

### Additional Context

{context}

# planning

## Planning Task

Break down the following into a sequenced implementation plan.

For each step, provide:
1. **What:** Specific deliverable
2. **Files:** Which files are created or modified
3. **Dependencies:** Which prior steps must complete first
4. **Complexity:** Simple / Medium / Complex
5. **Notes:** Any risks, decisions, or alternatives

### Objective

{content}

### Current State

{context}

# test-generation

## Test Generation Task

Generate tests for the following code. Requirements:
1. Cover all public methods
2. Include edge cases and error paths
3. Use the testing framework and patterns shown in the context
4. Each test should be independent

### Code Under Test

{content}

### Existing Test Patterns and Framework

{context}

# uat-plan-generation

## UAT Checklist Generation Task

Generate a UAT checklist JSON file that conforms to the canonical schema at
`https://raw.githubusercontent.com/darndestdabbler/uat-checklist-editor/main/checklist-schema.json`
and follows the UAT/E2E DSL convention defined in
`docs/planning/project-guidance.md` → Conventions → Human UAT And Checklist Editor.

### Hard requirements

1. **Schema conformance** — root must use `ProjectName` and `Review` (singular, array).
   Each Review item requires `FunctionalArea`, `Subarea`, `ItemLabel`. Each item also
   sets `E2ETestReference` (form: `Class.Method` or `Namespace.Class.Method`) — this
   is the pairing mechanism between the UAT item and its Playwright test method.
   Each step uses `StepNumber` (integer, sequential 1..N) and `Action` (string).
   Verifications are objects with `Text` (not plain strings). `additionalProperties:
   false` at every nesting level — no extra fields. Do NOT include `Id` (not in the
   schema).

2. **DSL conformance for every step `Action`.** Use exactly one verb from this
   closed list (parameters in `<...>`, optional scope in `[in <scope>]`):

   **Lifecycle:** `NAVIGATE TO <URL>` · `WAIT FOR <visible state>` · `PRESS KEY <key>`

   **Interaction:** `CLICK BUTTON [in <scope>] with caption <caption>` ·
   `CLICK ICON BUTTON [in <scope>] with aria-label <label>` ·
   `CLICK LINK [in <scope>] with text <text>` ·
   `CLICK TAB [in <scope>] with name <name>` ·
   `CLICK MENU ITEM [in <scope>] with label <label>` ·
   `CLICK INPUT [in <scope>] with label <label> and enter <value>` ·
   `CLICK CHECKBOX [in <scope>] with label <label>` ·
   `CLICK SWITCH [in <scope>] with label <label>` ·
   `CLICK SWITCH [in <scope>] in column with header <header>` ·
   `OPEN DROPDOWN [in <scope>] with label <label> and select item whose text = <text>` ·
   `PICK DATE [in <scope>] with label <label> and choose <date>` ·
   `SCROLL <grid name> to row where <col> = <value> [and ...]` ·
   `CLICK ROW in <grid name> where <col> = <value> [and ...]` ·
   `EXPAND ROW in <grid name> where <col> = <value> [and ...]`

   **Verification:** `VERIFY <user-visible property or event>`

   Locators are by visible label or visible text only. Never by testid, xpath,
   CSS class, or DOM index. Row identification is always by column-value match.
   Composites do not exist — "sign in" is 5–8 primitive steps.

3. **Per-step reachability.** Every step must be performable by a real human in a
   browser AND by a Playwright test using only user-visible surfaces. Reject any
   step that depends on a test framework backdoor (`SeededSignInAsync(roles=)`,
   `/test-auth/*` shortcuts, direct `security-config.json` edits, EF API pokes).

### Spec / source material

{content}

### Existing context

{context}

### Output

Output ONLY valid JSON. No markdown fences. No explanatory prose before or after.

# uat-script-author

## Catalog-Grounded UAT Checklist Authoring Task

Author a UAT checklist JSON file whose every label, caption, text value, tab
name, and scope descriptor matches a verbatim entry in the supplied component
catalog for the active route × role. Validation is performed by the
`dabbler-uat-dsl validate` CLI from the `dabbler-uat-dsl` library — the
companion grammar at
`https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/grammar/DabblerUatDsl.g4`
is the authoritative source for syntax. The catalog is owned by this repo and
produced by `tools/ComponentCatalogCrawler/` (see
`docs/component-catalog/index.json`).

### Hard requirements

1. **Schema conformance.** Use the canonical UAT-checklist-editor schema at
   `https://raw.githubusercontent.com/darndestdabbler/uat-checklist-editor/main/checklist-schema.json`.
   Root: `ProjectName` (string, required), `Review` (array, required).
   Each Review item — required: `FunctionalArea`, `Subarea`, `ItemLabel`,
   `E2ETestReference`, `Steps`. Optional on Review items: `Feedback`
   (string — used for catalog-gap notes and judgment-item rationale),
   `IsJudgmentItem` (bool), `IsOtherItem` (bool), `Passes` (bool — written
   inline by the human after review), `HumanAction` and `Expectation`
   (legacy free-form pair). Each step: `StepNumber` (1..N), `Action`
   (string), optional `Verifications` (array of `{ Text, ... }` objects).
   `additionalProperties: false` at every nesting level. **Do NOT include**
   `Id`, `Title`, `Description`, `JudgmentJustification`, `Reviews`
   (plural), or `SessionSetSlug` — these are not in the schema.

2. **DSL conformance.** Every `Action` and verification clause must use exactly
   one verb from the closed list defined in
   `docs/planning/project-guidance.md` → Conventions → Human UAT And Checklist
   Editor (and codified by the dabbler-uat-dsl ANTLR grammar). Locators are by
   visible label or visible text only — never by testid, xpath, CSS class, or
   DOM index. Row identification is always by column-value match. Composites
   do not exist; "sign in" is 5–8 primitive steps.

3. **Quoted-string requirement.** All user-visible labels, captions, text
   values, and scope descriptors must be **double-quoted string literals**
   inside the `Action` text. Keywords (`with`, `in`, `and`, `where`, `to`,
   `enter`, `caption`, `label`, `text`, `name`, `aria-label`, etc.) appear
   bare. Examples:
   - ✓ `CLICK BUTTON in "Add Mask Exemption" dialog with caption "Save"`
   - ✗ `CLICK BUTTON in Add Mask Exemption dialog with caption Save`
   This makes the grammar unambiguous: keywords inside quoted values are never
   tokenized as structural keywords.

4. **Catalog grounding (the rule that makes this task type distinct).** Every
   double-quoted label, caption, text, tab name, column header, and scope
   value must match a `userVisibleLabel` or `ariaLabel` entry in the supplied
   catalog **for the current route × role**. The catalog has columns
   `routePath`, `role`, `elementRole`, `userVisibleLabel`, `ariaLabel`,
   `pageTitle`, etc.
   - For an interaction step on route `R` performed by role `Q`, every
     quoted value in that step's `Action` must appear as the
     `userVisibleLabel` of a catalog row where `routePath == R` and
     `role == Q`.
   - The DSL verb's element type must match the catalog `elementRole`. A
     row whose `elementRole` is `link` is `CLICK LINK`, not `CLICK BUTTON`.
     A row whose `elementRole` is `tab` is `CLICK TAB`. A row whose
     `elementRole` is `iconButton` (button + non-empty `ariaLabel` + empty
     visible text) is `CLICK ICON BUTTON with aria-label "<ariaLabel>"`.
   - Scope descriptors (`in "Add Mask Exemption" dialog`, `in the "Mask
     Exemptions" tab`, `in "Customers" grid`) must reference a catalog
     entry that names that scope (a `dialog`, `tab`, `grid`, or named
     landmark `Element`).

5. **FORBIDDEN: inventing a label.** If a step needs to name a control or
   text that is NOT in the catalog for the current route × role, **STOP and
   report the gap** as a `// CATALOG GAP:` JSON-comment-style note in your
   output preamble (or in a `Feedback` note on the affected Review item).
   Do NOT emit a step that references a label the catalog does not contain.
   The remediation for a real gap is a targeted re-crawl of that
   route × role, not a guessed label.

6. **Prefer structured VERIFY forms.** The structured forms (defined in
   project-guidance and the grammar) compile to deterministic Playwright
   assertions:
   - `VERIFY ELEMENT [in <scope>] with label "<label>" is visible | not
     visible | enabled | disabled`
   - `VERIFY TEXT [in <scope>] equals "<expected>"` /
     `contains "<expected>"` / `matches "<regex>"`
   - `VERIFY URL matches "<pattern>"`
   - `VERIFY SNACKBAR shows "<text>"`
   - `VERIFY ROW in "<grid>" where "<col>" = "<value>" exists | does not exist`

   Free-form `VERIFY <text>` is allowed (legacy compatibility) but emits
   `// PENDING` in generated test code. Prefer structured forms.

7. **Per-step reachability.** Every step must be performable by a real human
   in a browser AND by a Playwright test using only user-visible surfaces.
   Reject any step that depends on a test framework backdoor
   (`SeededSignInAsync(roles=)`, `/test-auth/*` shortcuts, direct
   `security-config.json` edits, EF API pokes, etc.) when the step is
   something a human is also expected to perform.

8. **Watermark synchronization (`IsWatermarked` routes).** Each catalog
   route entry carries an `IsWatermarked: bool` field, true iff the page
   component inherits from `Dabbler.Testing.Blazor.Components.TrackedComponentBase`
   and is therefore instrumented to advertise outstanding async work via
   `data-actions-in-flight`. For steps on a route whose catalog entry has
   `IsWatermarked: true`:

   - Emit a `WAIT FOR ACTIONS QUIET` step **immediately after each
     interaction verb** (`CLICK BUTTON`, `CLICK ICON BUTTON`, `CLICK LINK`,
     `CLICK TAB`, `CLICK MENU ITEM`, `CLICK INPUT`, `CLICK CHECKBOX`,
     `CLICK SWITCH`, `OPEN DROPDOWN`, `PICK DATE`, `EXPAND ROW`,
     `CLICK ROW`, `PRESS KEY` when the keypress drives a re-render).
   - Do NOT emit `WAIT FOR ACTIONS QUIET` after pure navigation
     (`NAVIGATE TO`), pure verification (`VERIFY ...`), `WAIT FOR ...`
     state polls, or on routes whose catalog entry has
     `IsWatermarked: false` (or where the field is absent — defaults to
     not-watermarked).
   - The verb has no parameters and no scope. It compiles to
     `await fixture.WaitForQuietAsync(page);`.

   **Positive example** (route `/admin/products`, catalog
   `IsWatermarked: true`):
   ```
   CLICK SWITCH in column with header "Available"
   WAIT FOR ACTIONS QUIET
   VERIFY SNACKBAR shows "Saved"
   ```

   **Negative example** (route `/dashboard`, catalog
   `IsWatermarked: false`):
   ```
   CLICK BUTTON with caption "Refresh"
   VERIFY ELEMENT with label "Sales today" is visible
   ```
   (no `WAIT FOR ACTIONS QUIET` — the route is not watermarked. Rely on
   Playwright's built-in auto-wait inside the `VERIFY` assertion, or add
   a free-form `WAIT FOR "<visible state>"` step only if an explicit
   settle period is needed. Do not invent a substitute wait verb.)

   The watermark integration closes the click-during-async-rerender race
   that legacy UAT checklists hit on watermarked pages. Hand-authored
   checklists may still skip the verb when the author knows an
   interaction does not produce an async re-render (e.g., a no-op
   acknowledgement); for AI-authored checklists, the safe default is
   "every interaction on a watermarked route gets the wait."

### Spec, reference checklist, and intended scenario coverage

{content}

### Component catalog and grammar reference

{context}

### Output

Output ONLY valid JSON conforming to the UAT-checklist-editor schema. No
markdown fences. No explanatory prose before or after the JSON. If a real
catalog gap blocks a step, emit a `Feedback` note on the affected Review
item beginning with `CATALOG GAP:` and continue with what coverage is
possible — the orchestrator will triage the gap and decide whether to
re-crawl or revise scope.

# uat-failure-triage

## UAT Failure Triage Task

Classify the following UAT runner failure into exactly one of four runtime
failure classes. The W0 compiler has already eliminated translation errors,
label-drift errors, and element-type-drift errors at compile time; these four
classes are the only ones that can appear at runtime.

### Failure taxonomy

| Class | Auto-applied? | Description |
|---|---|---|
| `APP_FUNCTIONAL_BUG` | Never | A `VERIFY` step's user-visible expectation did not match the observed state. The system under test has a bug. **This is the default when uncertain.** |
| `CATALOG_STALE` | Yes — re-crawl | A locator compiled successfully but now finds nothing in the live app because the app changed after the catalog was generated. |
| `SEED_DATA_GAP` | No — human review | A step references test data that was not seeded (e.g., `CLICK ROW in "Customers" where "Name" = "Acme Corp"` but "Acme Corp" does not exist in the DB). |
| `AMBIGUOUS_LOCATOR` | No — human review | A locator matched more than one element; a `data-testid` discriminator is needed. |

### Decision rules

1. If a `VERIFY` step's assertion describes a **visible value or visible state**
   (text content, URL, element visibility, control enabled/disabled state) and
   the observed value differs from the expected value, classify as
   `APP_FUNCTIONAL_BUG` regardless of whether locators are also suspect.
   **Preserving the bug-detection signal is more important than self-healing
   speed.**

2. Classify as `CATALOG_STALE` only when the error is clearly a
   **"element not found"** failure on an *interaction* verb (CLICK, OPEN
   DROPDOWN, NAVIGATE TO) and the failing locator was valid at catalog-crawl
   time but the app has likely changed since. Do NOT classify a missing-element
   error on a VERIFY as CATALOG_STALE — a VERIFY that asserts presence and
   finds absence is `APP_FUNCTIONAL_BUG`.

3. Classify as `SEED_DATA_GAP` when the error message references a specific
   named entity (customer name, order ID, email address) that is not present in
   the database, and the step was trying to select or navigate to that entity.

4. Classify as `AMBIGUOUS_LOCATOR` only when the error message explicitly
   states that multiple elements matched the given locator (e.g.,
   "strict mode violation: locator … resolved to N elements").

5. **When uncertain between `APP_FUNCTIONAL_BUG` and any other class, always
   default to `APP_FUNCTIONAL_BUG`.** A false positive (filing a non-bug as a
   bug) is far less harmful than a false negative (auto-fixing a real bug away).

### Failure record (JSON)

{content}

### Supporting context (catalog entry, if available)

{context}

### Output

Return ONLY a JSON object with these four fields. No markdown fences. No prose.

```
{
  "classification": "<one of the four class names above>",
  "confidence": <float 0.0–1.0>,
  "evidence": "<one or two sentences quoting specific observed vs. expected values>",
  "proposed_fix": "<optional short suggestion for CATALOG_STALE/SEED_DATA_GAP/AMBIGUOUS_LOCATOR; empty string for APP_FUNCTIONAL_BUG>"
}
```
