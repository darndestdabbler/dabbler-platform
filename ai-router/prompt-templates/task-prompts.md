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
   carries an `Id` (GUID, required for E2E pairing — generate with `uuid.uuid4()`).
   Each step uses `StepNumber` (integer) and `Action` (string). Verifications are
   objects with `Text` (not plain strings). `additionalProperties: false` at every
   nesting level — no extra fields.

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

Pair every UAT checklist Review item to its E2E test method using the GUID +
step-number convention defined in `docs/planning/project-guidance.md` → Conventions
→ Human UAT And Checklist Editor. Return VERIFIED only when every check below passes.

### Pairing protocol

1. **GUID match.** Each Review item has an `Id` (GUID) — fall back to
   `TestData._uatItemId` if `Id` is absent. The matching E2E test method's
   header `<summary>` comment contains a line `UAT Item Id: <guid>` whose
   value equals the Review item's `Id`. Reject any Review item with no
   matching method.

2. **Method existence.** The `E2ETestReference` value (form: `Class.Method`)
   resolves to an actual `[Fact]` or `[Theory]` method in the Playwright test
   file shown in `{context}`.

3. **Step-block walk.** Inside the matching method body, `// begin STEP NNN`
   comments delimit step blocks. The step numbers and order match the Review
   item's `Steps[].StepNumber`. Each numbered UAT step pairs to exactly one
   E2E step block.

4. **DSL conformance check (UAT side).** Every Review step's `Action` text
   must parse as exactly one verb from the DSL closed list. Reject steps
   that use locators by testid/xpath/index, refer to row index instead of
   column-value match, or compose multiple actions into one step.

5. **DSL conformance check (E2E side).** The Playwright code inside each
   `// begin STEP NNN` block must be implementable through user-visible
   locators (`page.GetByLabel`, `page.GetByRole`, `page.GetByText`, etc.).
   Reject any block that uses `page.GetByTestId`, raw selectors, framework
   backdoors (`SeededSignInAsync(roles=)`, `/test-auth/*` calls, direct
   file edits) when the step is something a human is also expected to
   perform.

6. **Per-step reachability cross-verify.** For every numbered step, confirm
   both (a) a real human in a browser can perform the step using only the
   visible surfaces named in the DSL, and (b) the E2E code in the matching
   step block performs the same action through the same visible surfaces.
   If either side cannot, return ISSUES_FOUND.

7. **Judgment items.** Items with `IsJudgmentItem: true` are exempt from the
   step-by-step DSL match but still require a populated `Feedback` line
   ("Judgment item — <rationale>") and a valid GUID-paired method.

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

  - Item `<UAT Id or Subarea>` step `<N>`: `<specific failure>`
  - ...

with one bullet per failed step or pairing, and the specific protocol number
(1–7) the failure violates.
