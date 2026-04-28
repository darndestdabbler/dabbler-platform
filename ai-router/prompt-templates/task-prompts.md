_Task prompt templates — one section per routed task type._

This file contains the user-message template applied by `prompting.py`
for each routed task type. `config.py` parses the `#` (H1) headers; the
slug of each header becomes the key in `config["_task_templates"]` that
`prompting.py` looks up. H1 is used rather than H2 because the template
content itself begins with H2 headers that would otherwise collide with
the section-boundary parser.

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

# uat-coverage-review

## UAT Coverage Review Task

Pair every UAT checklist Review item to its E2E test method using the
`E2ETestReference` + `// begin STEP NNN` convention defined in
`docs/planning/project-guidance.md` → Conventions → Human UAT And Checklist
Editor. Return VERIFIED only when every check below passes.

### Pairing protocol

1. **Method existence.** The Review item's `E2ETestReference` value
   (form: `Class.Method` or `Namespace.Class.Method`) resolves to an actual
   `[Fact]` or `[Theory]` method in the Playwright test file shown in
   `{context}`. Reject any non-judgment Review item with no method match.

2. **Step-block walk.** Inside the matching method body, `// begin STEP NNN`
   comments (zero-padded to three digits) delimit step blocks. The step
   numbers and order match the Review item's `Steps[].StepNumber`. Each
   numbered UAT step pairs to exactly one E2E step block. Mismatched count
   or out-of-order numbering is a failure.

3. **DSL conformance check (UAT side).** Every Review step's `Action` text
   must parse as exactly one verb from the DSL closed list. Reject steps
   that use locators by testid/xpath/index, refer to row index instead of
   column-value match, or compose multiple actions into one step.

4. **DSL conformance check (E2E side).** The Playwright code inside each
   `// begin STEP NNN` block must be implementable through user-visible
   locators (`page.GetByLabel`, `page.GetByRole`, `page.GetByText`, etc.).
   Reject any block that uses `page.GetByTestId`, raw selectors, framework
   backdoors (`SeededSignInAsync(roles=)`, `/test-auth/*` calls, direct
   file edits) when the step is something a human is also expected to
   perform.

5. **Per-step reachability cross-verify.** For every numbered step, confirm
   both (a) a real human in a browser can perform the step using only the
   visible surfaces named in the DSL, and (b) the E2E code in the matching
   step block performs the same action through the same visible surfaces.
   If either side cannot, return ISSUES_FOUND.

6. **Judgment items.** Items with `IsJudgmentItem: true` are exempt from the
   step-by-step DSL match but still require a populated `Feedback` line
   ("Judgment item — <rationale>") and a valid `E2ETestReference` resolving
   to a method that drives the UI to the state the human will judge.

### Bias

Prefer false-positive `ISSUES_FOUND` over false-negative `VERIFIED`. The cost
of one extra remediation round is far lower than the cost of an unrunnable
checklist reaching the human.

### Inputs

{content}

### Reference material

{context}

### Output

Return either:

  VERIFIED

OR

  ISSUES_FOUND

  - Item `<Subarea / ItemLabel>` step `<N>`: `<specific failure>`
  - ...

with one bullet per failed step or pairing, and the specific protocol number
(1–6) the failure violates.

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
