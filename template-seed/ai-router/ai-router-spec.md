# AI Router — Lightweight Model Routing for Claude Code

> **Purpose:** A Python module that Claude Code calls to route AI work to the cheapest capable model.
> **Created:** 2026-04-07
> **Workflow:** Claude Chat (Opus) plans → Claude Code (Sonnet) orchestrates → AI APIs execute subtasks

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Directory Structure](#2-directory-structure)
3. [Configuration: `router-config.yaml`](#3-configuration-router-configyaml)
4. [Model-Specific Prompting](#4-model-specific-prompting)
5. [The Python Module: `ai_router/`](#5-the-python-module-ai_router)
6. [Cross-Provider Verification (IV&V)](#6-cross-provider-verification-ivv)
7. [Session Sets](#7-session-sets)
8. [How Claude Code Uses the Router](#8-how-claude-code-uses-the-router)
9. [The Session Set Workflow](#9-the-session-set-workflow)
10. [Human Setup and Operation Guide](#10-human-setup-and-operation-guide)
11. [Example Walkthrough](#11-example-walkthrough)
12. [CLAUDE.md Instructions for Claude Code](#12-claudemd-instructions-for-claude-code)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    YOUR MACHINE                         │
│                                                         │
│  ┌──────────────┐    specs     ┌──────────────────────┐ │
│  │ Claude Chat   │ ──────────> │ Claude Code (Sonnet) │ │
│  │ (Opus)        │             │                      │ │
│  │               │  <───────── │  reads spec          │ │
│  │ Plans work,   │  code docs  │  breaks into tasks   │ │
│  │ reviews docs  │  for review │  implements files    │ │
│  └──────────────┘              │  calls ai_router for │ │
│                                │  cheap subtasks      │ │
│                                │                      │ │
│                                │  ai_router/          │ │
│                                │  ├─ calls Flash for  │ │
│                                │  │  simple analysis  │ │
│                                │  ├─ calls Pro for    │ │
│                                │  │  medium tasks     │ │
│                                │  └─ lets Sonnet      │ │
│                                │     handle the rest  │ │
│                                └──────────┬───────────┘ │
│                                           │             │
│              ┌────────────────────────────┼──────┐      │
│              │ activity-log.json          │      │      │
│              │ (progress, costs)          │      │      │
│              └────────────────────────────┘      │      │
│                                                  │      │
└──────────────────────────────────────────────────┼──────┘
                                                   │
                    ┌──────────────────────────────┘
                    │ HTTPS
                    ▼
        ┌───────────────────┐
        │ Anthropic API     │  Sonnet, Opus
        │ Google AI API     │  Flash, Pro
        └───────────────────┘
```

**Key insight:** Claude Code is already an orchestrator with filesystem access, shell access, and reasoning capability. The router module is just a Python library it calls when it wants to offload reasoning work to a cheaper model. Claude Code handles all file I/O, sequencing, and decision-making itself.

---

## 2. Directory Structure

Place this at the root of any project where Claude Code will use routing:

```
your-project/
├── ai-router/
│   ├── __init__.py              ← public API: route(), verify(), query(), get_costs()
│   ├── config.py                ← loads router-config.yaml
│   ├── models.py                ← model registry, complexity estimation
│   ├── providers.py             ← Anthropic and Google API callers
│   ├── verification.py          ← cross-provider IV&V logic
│   ├── rate_limiter.py          ← per-provider rate limiting
│   ├── session_log.py           ← JSON session log tracking
│   ├── prompting.py             ← model-specific prompt formatting
│   ├── escalation.py            ← escalation detection and model upgrade logic
│   ├── cli.py                   ← optional CLI for cost reports
│   └── router-config.yaml       ← all configuration (models, thresholds, prompts)
│
├── requirements.txt             ← Python dependencies (httpx, pyyaml)
├── prompt-templates/
│   ├── anthropic-system.md      ← system prompt for Claude models
│   ├── google-system.md         ← system prompt for Gemini models
│   ├── verification.md          ← per-task verification prompt template
│   ├── session-verification.md  ← end-of-session verification prompt template
│   └── task-types/
│       ├── code-review.md       ← task-specific prompt template
│       ├── documentation.md
│       ├── analysis.md
│       ├── test-generation.md
│       └── planning.md
│
├── docs/
│   └── session-sets/                        ← one folder per session set
│       └── {session-set-name}/
│           ├── spec.md                      ← implementation spec with session plan
│           ├── activity-log.json            ← step-by-step progress + API costs
│           ├── change-log.md                ← generated on last session only
│           ├── session-reviews/
│           │   ├── session-001.md           ← verifier raw output for session 1
│           │   ├── session-002.md           ← verifier raw output for session 2
│           │   └── ...
│           └── issue-logs/
│               ├── session-001.json         ← issues + fix/defer status for session 1
│               ├── session-002.json         ← issues + fix/defer status for session 2
│               └── ...
│
├── CLAUDE.md                    ← Claude Code instructions (session workflow built in)
└── ... (your project files)
```

### File Responsibilities (each file has one job)

| File | Owner | Job |
|------|-------|-----|
| `spec.md` | Claude Chat (Opus) | What to build — features, types, session plan |
| `activity-log.json` | Claude Code (Sonnet) | Step-level progress tracking and routed API costs |
| `session-reviews/session-NNN.md` | Verifier AI (cross-provider) | Raw verification output, untouched by Sonnet |
| `issue-logs/session-NNN.json` | Claude Code (Sonnet) | Structured issues with fix/defer status and response |
| `change-log.md` | Claude Code (Sonnet) | Inventory of all new/changed files (last session only) |

---

## 3. Configuration: `router-config.yaml`

```yaml
# ============================================================
# AI Router Configuration
# ============================================================
# All model info, pricing, thresholds, and behavior in one file.
# API keys are NEVER stored here — use environment variables.
# ============================================================

# --- Provider Configuration ---
providers:
  anthropic:
    api_key_env: ANTHROPIC_API_KEY
    base_url: https://api.anthropic.com/v1/messages
    api_version: "2023-06-01"
    rate_limit:
      requests_per_minute: 50
      tokens_per_minute: 80000
    timeout_seconds: 300
    retry:
      max_retries: 2
      backoff_base_seconds: 2  # exponential: 2s, 4s

  google:
    api_key_env: GEMINI_API_KEY
    base_url: https://generativelanguage.googleapis.com/v1beta
    rate_limit:
      requests_per_minute: 60
      tokens_per_minute: 100000
    timeout_seconds: 300
    retry:
      max_retries: 2
      backoff_base_seconds: 2

# --- Model Registry ---
# Each model has a tier (1=cheap/fast, 2=balanced, 3=powerful/expensive),
# pricing, context limits, and a reference to its prompt template.
models:
  gemini-flash:
    provider: google
    model_id: gemini-2.5-flash
    tier: 1
    input_cost_per_1m: 0.15
    output_cost_per_1m: 0.60
    max_context_tokens: 1048576
    max_output_tokens: 65536
    system_prompt_file: prompt-templates/google-system.md
    notes: "Best for simple analysis, formatting, boilerplate generation"

  gemini-pro:
    provider: google
    model_id: gemini-2.5-pro
    tier: 2
    input_cost_per_1m: 1.25
    output_cost_per_1m: 10.00
    max_context_tokens: 1048576
    max_output_tokens: 65536
    system_prompt_file: prompt-templates/google-system.md
    notes: "Good for medium-complexity analysis, documentation, code review"

  sonnet:
    provider: anthropic
    model_id: claude-sonnet-4-6-20250514
    tier: 2
    input_cost_per_1m: 3.00
    output_cost_per_1m: 15.00
    max_context_tokens: 200000
    max_output_tokens: 16000
    system_prompt_file: prompt-templates/anthropic-system.md
    notes: "Use when Anthropic quality needed but Opus overkill"

  opus:
    provider: anthropic
    model_id: claude-opus-4-6
    tier: 3
    input_cost_per_1m: 15.00
    output_cost_per_1m: 75.00
    max_context_tokens: 200000
    max_output_tokens: 32000
    system_prompt_file: prompt-templates/anthropic-system.md
    notes: "Reserved for complex architectural reasoning"

# --- Routing Thresholds ---
# Complexity score 1-100 mapped to tiers.
routing:
  tier1_max_complexity: 30    # score ≤ 30 → tier 1 (Flash)
  tier2_max_complexity: 65    # score 31–65 → tier 2 (Pro or Sonnet)
  default_tier: 2             # fallback when estimation is uncertain

  # Which model to use for each tier
  tier_assignments:
    1: gemini-flash
    2: gemini-pro
    3: opus

  # Override: force a specific model for specific task types
  task_type_overrides:
    # Task types where the Anthropic style matters (e.g., careful reasoning)
    # can be forced to Sonnet even at tier 2 instead of Gemini Pro
    # code-review: sonnet

# --- Complexity Estimation Weights ---
# These weights control how the complexity score is calculated.
# Adjust based on observed results.
complexity:
  weights:
    context_length: 0.30      # longer prompts = more complex
    keyword_signals: 0.35     # content-based signals
    task_type: 0.20           # some task types are inherently harder
    explicit_hint: 0.15       # caller can provide a hint (1-100)

  # Context length breakpoints → sub-scores
  context_length_scores:
    - { max_chars: 500, score: 10 }
    - { max_chars: 2000, score: 25 }
    - { max_chars: 5000, score: 45 }
    - { max_chars: 10000, score: 65 }
    - { max_chars: 999999, score: 85 }

  # Task type base scores
  task_type_scores:
    code-review: 40
    documentation: 25
    analysis: 55
    test-generation: 35
    planning: 70
    formatting: 10
    summarization: 20
    architecture: 80
    security-review: 75
    refactoring: 65
    session-verification: 65
    general: 50

  # Keywords that push complexity up (+3 each, capped at +15)
  high_complexity_keywords:
    - architect
    - security
    - performance
    - concurrency
    - migration
    - breaking change
    - cross-cutting
    - distributed
    - race condition
    - backward compatible
    - critical
    - complex

  # Keywords that push complexity down (-3 each, capped at -15)
  low_complexity_keywords:
    - rename
    - typo
    - format
    - lint
    - boilerplate
    - simple
    - straightforward
    - placeholder
    - stub
    - comment
    - log
    - copy

# --- Escalation ---
escalation:
  enabled: true
  max_escalations: 2          # tier 1 → 2 → 3, then give up
  triggers:
    empty_response: true
    max_tokens_hit: true       # response truncated
    min_output_tokens: 30      # suspiciously short response
    refusal_detection: true    # model declined the task
  refusal_phrases:
    - "i can't help with"
    - "i'm unable to"
    - "i cannot assist"
    - "beyond my capabilities"
    - "i don't have enough context"

# --- Verification (IV&V) ---
# Cross-provider verification: a model from a different provider
# checks the work of the original model to catch provider-specific
# blind spots and biases.
verification:
  enabled: true

  # Cross-provider model pairings.
  cross_provider_map:
    gemini-flash: sonnet        # Google Tier 1 → Anthropic Tier 2
    gemini-pro: sonnet           # Google Tier 2 → Anthropic Tier 2
    sonnet: gemini-pro           # Anthropic Tier 2 → Google Tier 2
    opus: gemini-pro             # Anthropic Tier 3 → Google Tier 2

  # Which task types are auto-verified when using route().
  auto_verify_task_types:
    - code-review
    - security-review
    - architecture

  # Verification behavior
  settings:
    check_categories:
      - correctness
      - completeness
      - false_positives

    on_disagreement: merge     # "merge" | "flag" | "re-route"
    tiebreaker_model: opus
    max_cost_multiplier: 3.0
    prompt_template_file: prompt-templates/verification.md

# --- Session Logging ---
session_logs:
  session_sets_dir: docs/session-sets   # relative to project root
  log_responses: false                   # if true, stores full responses (large!)

# --- Output ---
output:
  cost_report_on_exit: true
  verbose: false
```

---

## 4. Model-Specific Prompting

Different models respond best to different prompt structures. The router handles this transparently.

### 4.1 System Prompt Files

**File:** `prompt-templates/anthropic-system.md`

```markdown
You are an expert software engineer assisting with a development project. You produce precise, actionable output. Follow these rules:

- Be direct. No preamble, no "Sure, I'd be happy to help."
- Output exactly what was requested — code, analysis, documentation — nothing more.
- If generating code, include no explanatory text unless specifically asked.
- If reviewing code, structure your response as: Issue → Location → Fix.
- Use markdown formatting for readability.
```

**File:** `prompt-templates/google-system.md`

```markdown
You are an expert software engineer. Produce precise, actionable output.

Rules:
- Be direct. No conversational filler.
- Output exactly what was requested.
- If generating code, include only the code unless asked for explanation.
- If reviewing, structure as: Issue → Location → Fix.
- Use markdown formatting.
```

### 4.2 Task-Type Templates

These are optional prompt wrappers that structure the user message for specific task types. Claude Code inserts the actual content into the template.

**File:** `prompt-templates/task-types/code-review.md`

```markdown
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
```

**File:** `prompt-templates/task-types/documentation.md`

```markdown
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
```

**File:** `prompt-templates/task-types/analysis.md`

```markdown
## Analysis Task

Analyze the following and provide a structured assessment:

### Subject

{content}

### Specific Questions

{context}

### Response Format

Provide your analysis with clear sections, concrete recommendations, and supporting reasoning.
```

**File:** `prompt-templates/task-types/test-generation.md`

```markdown
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
```

**File:** `prompt-templates/task-types/planning.md`

```markdown
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
```

### 4.3 Verification Prompt Template

**File:** `prompt-templates/verification.md`

```markdown
## Independent Verification

You are an independent verifier. A different AI model completed the task below. Your job is to check its work for errors, omissions, and incorrect reasoning. You have no loyalty to the original response — evaluate it objectively.

### Original Task

{original_task}

### Original Task Type

{task_type}

### Response Under Review

{original_response}

### Your Instructions

Evaluate the response against these criteria:

1. **Correctness:** Are there factual errors, logical flaws, incorrect code, or wrong conclusions?
2. **Completeness:** Did the original response miss anything important that the task required?
3. **False Positives:** (For reviews/audits) Did the original flag issues that aren't actually problems?

### Response Format

Start with one of these verdicts:

- **VERIFIED** — The response is correct and complete. No significant issues found.
- **ISSUES FOUND** — The response has problems that should be addressed.

If VERIFIED, briefly state why you're confident (1–2 sentences).

If ISSUES FOUND, list each issue:
- **Issue N:** [description]
  - **Category:** Correctness / Completeness / False Positive
  - **Severity:** Critical / Major / Minor
  - **Details:** What's wrong and what the correct answer should be

Do NOT re-do the entire task. Only evaluate what was already produced.
```

### 4.4 Session Verification Prompt Template

**File:** `prompt-templates/session-verification.md`

```markdown
## End-of-Session Verification

You are an independent verifier. A different AI model implemented the changes below
as part of a session plan. Check that the implementation matches the spec, is correct,
and is complete.

### Session Spec

{session_spec}

### Files Created or Modified

{file_contents}

### Build/Test Results

{build_results}

### Your Instructions

Evaluate against these criteria:

1. **Spec Compliance:** Does each file implement what the session spec described?
2. **Correctness:** Are there bugs, logic errors, type mismatches, null reference risks,
   or missing edge cases?
3. **Completeness:** Were all steps in the session plan implemented? Is anything missing?
4. **No Regressions:** Do the changes preserve existing functionality?

### Response Format

Start with **VERIFIED** or **ISSUES FOUND**.

If VERIFIED, state why in 1–2 sentences.

If ISSUES FOUND, list each issue:
- **Issue N:** [description]
  - **Severity:** Critical / Major / Minor
  - **File:** Which file is affected
  - **Fix:** What should be changed
```

---

## 5. The Python Module: `ai_router/`

The router is implemented as a set of Python files in the `ai-router/` directory. Each file has a single responsibility. All source files are delivered alongside this spec document.

No heavyweight dependencies. `httpx` for HTTP calls, `pyyaml` for config. Session logging uses JSON (stdlib). See `requirements.txt`.

### 5.1 Module Files

| File | Purpose | Key Functions / Classes |
|------|---------|------------------------|
| `__init__.py` | Public API and orchestration | `route()`, `query()`, `verify()`, `get_costs()`, `print_cost_report()`, `RouteResult`, `VerificationResult` |
| `config.py` | Loads and validates `router-config.yaml` | `load_config(path)` → `dict` |
| `models.py` | Complexity estimation and model selection | `estimate_complexity(text, task_type, hint, config)` → `int`, `pick_model(score, max_tier, task_type, config)` → `str` |
| `providers.py` | Anthropic and Google API callers with retry | `call_model(provider_name, model_id, system_prompt, user_message, max_tokens, config)` → `APIResult` |
| `verification.py` | Cross-provider IV&V logic | `pick_verifier_model()`, `build_verification_prompt()`, `parse_verification_response()` |
| `rate_limiter.py` | Per-provider rate limiting (token bucket) | `RateLimiter(rpm, tpm)` with `.wait()` |
| `session_log.py` | JSON session log tracking for session sets | `SessionLog(session_set_dir)` — see §7.8 |
| `prompting.py` | Model-specific prompt formatting from templates | `build_prompt(content, context, task_type, model_cfg, config)` → `(system_prompt, user_message)` |
| `escalation.py` | Escalation detection and model upgrade | `should_escalate(result, config)` → `bool`, `get_escalation_model(current, config, count)` → `str | None` |
| `cli.py` | Command-line cost report | `python -m ai_router.cli --session-set <path>` |

### 5.2 Public API: `route()`

The primary entry point. Routes a task to the cheapest capable model based on complexity estimation.

```python
from ai_router import route

result = route(
    content="<the text to analyze or process>",
    task_type="code-review",       # or analysis, documentation, etc.
    context="<optional background>",
    complexity_hint=None,          # optional 1-100 override
    max_tier=3                     # cap the maximum model tier
)
```

**Parameters:**
- `content` (str): The main content/prompt — code, text, or question.
- `task_type` (str): One of the task types in `router-config.yaml` (code-review, documentation, analysis, test-generation, planning, formatting, summarization, architecture, security-review, refactoring, session-verification, general).
- `context` (str): Additional context (project info, existing patterns, etc.)
- `complexity_hint` (int | None): Optional manual complexity score (1–100). Weighted into the estimate.
- `max_tier` (int): Maximum tier to use (1, 2, or 3). For cost-capped runs.

**Returns:** `RouteResult` with:
- `content` (str): The AI response. If auto-verified with `on_disagreement="merge"`, includes verifier feedback.
- `model_name` (str): Which model handled it (e.g., "gemini-flash").
- `model_id` (str): The provider's model ID (e.g., "gemini-2.5-flash").
- `tier` (int): The tier used (1, 2, or 3).
- `input_tokens` / `output_tokens` (int): Token usage.
- `cost_usd` (float): Cost of the generation call only.
- `total_cost_usd` (float): Generation + verification combined.
- `complexity_score` (int): The estimated complexity (1–100).
- `escalated` (bool): Whether the model was escalated.
- `escalation_history` (list): List of `(model, reason)` tuples if escalated.
- `elapsed_seconds` (float): Wall-clock time.
- `verification` (VerificationResult | None): Populated if auto-verified.

**Behavior:**
1. Estimates complexity from content length, keywords, task type, and optional hint.
2. Picks the cheapest model at the appropriate tier.
3. Builds a model-specific prompt using the task-type template.
4. Calls the API with retry and rate limiting.
5. If the response indicates failure (empty, truncated, refusal), escalates to the next tier.
6. If the task type is in `auto_verify_task_types`, runs cross-provider verification automatically.
7. Returns `RouteResult` with all metadata. The caller is responsible for logging via `SessionLog.log_step()`.

### 5.3 Public API: `query()`

Calls a specific model directly, bypassing complexity estimation.

```python
from ai_router import query

result = query(
    model="gemini-flash",
    content="Reformat this JSON:\n...",
    task_type="formatting"
)
```

### 5.4 Public API: `verify()`

Independently verifies a `RouteResult` using a model from a different provider.

```python
from ai_router import route, verify

result = route(content="...", task_type="documentation")
check = verify(result, original_task="...", task_type="documentation")
print(check.verdict)    # "VERIFIED" or "ISSUES_FOUND"
print(check.issues)     # list of issue dicts
```

**Returns:** `VerificationResult` with:
- `verdict` (str): "VERIFIED" or "ISSUES_FOUND"
- `verified` (bool): True if verdict == "VERIFIED"
- `issues` (list): List of issue dicts with `description`, `category`, `severity`
- `verifier_model` / `verifier_provider`: Which model and provider verified
- `generator_model` / `generator_provider`: Which model and provider generated the original
- `verifier_input_tokens` / `verifier_output_tokens` / `verifier_cost_usd`: Verification cost
- `raw_response` (str): Full verifier response text

### 5.5 Public API: `get_costs()` and `print_cost_report()`

```python
from ai_router import get_costs, print_cost_report

costs = get_costs("docs/session-sets/my-feature")
print_cost_report("docs/session-sets/my-feature")
```

Both require the path to the session set directory. Returns/prints aggregated cost data from the activity log.

### 5.6 How `route()` Internally Works

```
route(content, task_type="code-review")
  │
  ├─ estimate_complexity() → score (1-100)
  ├─ pick_model(score) → e.g., "gemini-pro"
  ├─ build_prompt() → (system_prompt, user_message)
  │
  ├─ ESCALATION LOOP:
  │   ├─ rate_limiter.wait()
  │   ├─ call_model() → APIResult
  │   ├─ should_escalate(result)?
  │   │   YES → get_escalation_model() → next tier model, retry
  │   │   NO  → break
  │
  ├─ _calculate_cost()
  ├─ Build RouteResult
  │
  ├─ is task_type in auto_verify_task_types?
  │   YES
  │   ├─ pick_verifier_model() → cross-provider model
  │   ├─ cost guard: verification too expensive?
  │   ├─ call verifier
  │   ├─ parse_verification_response() → verdict, issues
  │   ├─ on_disagreement == "merge"? → append feedback
  │   ├─ on_disagreement == "re-route"? → tiebreaker via Opus
  │   └─ attach VerificationResult to RouteResult
  │
  └─ return RouteResult
```

### 5.7 Configuration Loader (`config.py`)

`load_config(path)` reads `router-config.yaml` and performs:
- Validation that API key environment variables exist.
- Validation that model references in tier assignments are valid.
- Resolution of prompt template file paths (system prompts, task-type templates, verification template) relative to the config file's location. Resolved content is stored in private `_system_prompt`, `_task_templates`, and `_verification_template` keys.

### 5.8 Complexity Estimation (`models.py`)

`estimate_complexity()` produces a score from 1–100 using four weighted factors:
- **Context length** (30%): Longer prompts → higher complexity.
- **Keyword signals** (35%): Presence of high-complexity keywords (e.g., "architect", "security", "concurrency") pushes score up; low-complexity keywords (e.g., "rename", "typo", "format") push it down.
- **Task type** (20%): Each task type has a base score (e.g., formatting=10, architecture=80).
- **Explicit hint** (15%): Optional caller-provided override. If not provided, its weight is redistributed to the other factors.

`pick_model()` maps the complexity score to a tier (1/2/3) using the configured thresholds, checks for task-type overrides, and returns the assigned model name.

### 5.9 Provider Callers (`providers.py`)

`call_model()` dispatches to the appropriate provider (Anthropic or Google) and handles retries with exponential backoff. Returns an `APIResult` dataclass with `content`, `input_tokens`, `output_tokens`, and `stop_reason`.

The Anthropic caller uses the Messages API (`/v1/messages`). The Google caller uses the Gemini `generateContent` endpoint. Both normalize stop reasons to a common format.

### 5.10 Escalation Logic (`escalation.py`)

`should_escalate()` checks if a response indicates the model couldn't handle the task: empty response, max tokens hit (truncation), suspiciously short output, or refusal phrases detected.

`get_escalation_model()` returns the next-tier model, or `None` if the maximum escalation count has been reached.

---

## 6. Cross-Provider Verification (IV&V)

### 6.1 Design Principles

Cross-provider verification exploits the fact that models from different providers have different training data, different biases, and different failure modes. When Gemini Pro reviews code, it may miss issues that Sonnet catches, and vice versa. By having a second model from a different provider check the first model's work, you get higher confidence at a modest cost increase.

Key design decisions:

- **Verification is always cross-provider.** A Google model checks Anthropic's work; an Anthropic model checks Google's work. Same-provider verification adds less value because the models share training biases.
- **Equal tier is sufficient.** A Tier 2 model can verify another Tier 2 model's work. Verification is cognitively easier than generation — you're checking work, not creating it. Exception: if the generator was Tier 1 (Flash), the verifier bumps to Tier 2 since there's no Tier 1 Anthropic model.
- **Auto-verification is task-type-driven.** Only high-stakes task types (code review, security review, architecture) are auto-verified. Documentation and formatting are not worth the extra cost.
- **Three disagreement strategies.** When the verifier finds issues: **merge** (append feedback to the response), **flag** (return both and let Claude Code decide), or **re-route** (send to a Tier 3 tiebreaker for the final answer).

### 6.2 Verification Module (`verification.py`)

The verification module provides three functions:

- **`pick_verifier_model(generator_model, config)`** — Selects a cross-provider verifier. First checks the explicit `cross_provider_map` in config. Falls back to finding any model from a different provider at the closest tier.

- **`build_verification_prompt(original_task, original_response, task_type, template)`** — Builds the verification prompt from the configured template file, or uses a built-in default template. Substitutes `{original_task}`, `{original_response}`, and `{task_type}` placeholders.

- **`parse_verification_response(response)`** — Parses the verifier's free-text response into a structured `(verdict, issues)` tuple. Looks for "VERIFIED" or "ISSUES FOUND" at the start, then extracts individual issues with their category and severity using pattern matching.

### 6.3 Verification Flow Through `route()`

```
route(content, task_type="code-review")
  │
  ├─ estimate complexity → pick model (e.g., gemini-pro)
  ├─ call gemini-pro → get response
  │
  ├─ is "code-review" in auto_verify_task_types?
  │   YES
  │   │
  │   ├─ pick_verifier_model("gemini-pro") → "sonnet" (cross-provider)
  │   ├─ cost guard: would verification exceed 3× original cost?
  │   │   NO → proceed
  │   │
  │   ├─ call sonnet with verification prompt
  │   ├─ parse response → VERIFIED or ISSUES_FOUND
  │   │
  │   ├─ if VERIFIED:
  │   │   └─ attach VerificationResult, return
  │   │
  │   ├─ if ISSUES_FOUND:
  │   │   ├─ on_disagreement == "merge"?
  │   │   │   └─ append verifier feedback to content, return
  │   │   ├─ on_disagreement == "flag"?
  │   │   │   └─ attach VerificationResult, return original content
  │   │   └─ on_disagreement == "re-route"?
  │   │       ├─ call opus with both responses as tiebreaker
  │   │       └─ return opus response with verification attached
  │   │
  │   NO → return without verification
```

### 6.4 Cross-Provider Pairing Table

| Generator | Provider | → Verifier | Provider | Rationale |
|-----------|----------|-----------|----------|-----------|
| gemini-flash | Google | → sonnet | Anthropic | No Tier 1 Anthropic; Sonnet is affordable enough |
| gemini-pro | Google | → sonnet | Anthropic | Same tier, different provider |
| sonnet | Anthropic | → gemini-pro | Google | Same tier, different provider |
| opus | Anthropic | → gemini-pro | Google | Pro is sufficient to verify; Opus-tier Google isn't needed |

These are defaults in `router-config.yaml`. Override them if you add models or have different preferences.

### 6.5 When NOT to Verify

- **Formatting and boilerplate** — no value in verifying trivial output
- **Documentation** — unless it's API documentation where accuracy matters
- **Planning** — the plan will be reviewed by Claude Chat anyway
- **When cost guard triggers** — if the original call cost $0.0003, spending $0.01 to verify it doesn't make sense

The `max_cost_multiplier` setting (default 3.0) handles this automatically. For a Flash call costing $0.0003, verification by Sonnet at ~$0.003 is 10× — it would be skipped. For a Pro call costing $0.005, Sonnet verification at $0.003 is 0.6× — well within budget.

### 6.6 Explicit Verification

Claude Code can also verify explicitly, even for task types not in the auto-verify list:

```python
from ai_router import route, verify

# Route without auto-verification
result = route(content, task_type="documentation")

# Decide to verify anyway
if "security" in content.lower():
    check = verify(result, original_task=content, task_type="documentation")
    if not check.verified:
        print(f"Verifier found issues: {check.issues}")
        # Decide what to do — re-do, fix, or accept
```

---

## 7. Session Sets

A **session set** is a group of related Claude Code sessions that implement a single feature or enhancement — a mini-project or epic. Each session set lives in its own folder under `docs/session-sets/`.

### 7.1 Session Set File Structure

```
docs/session-sets/dabbler-filtergrid-enhancements/
├── spec.md                      ← the implementation spec (created by Claude Chat)
├── activity-log.json            ← step progress + API costs (created on session 1)
├── change-log.md                ← file/class inventory (created on last session)
├── session-reviews/
│   ├── session-001.md           ← raw verifier output for session 1
│   ├── session-002.md           ← raw verifier output for session 2
│   └── ...
└── issue-logs/
    ├── session-001.json         ← issues array for session 1
    ├── session-002.json         ← issues array for session 2 (only if issues found)
    └── ...
```

### 7.2 Session Set Naming

The session set name is derived from the folder name. It should be lowercase kebab-case, descriptive of the work, and unique within the project.

Example: `dabbler-filtergrid-enhancements`, `dabbler-combobox-enhancements`

### 7.3 Activity Log Format (`activity-log.json`)

Simple step-level progress and routed API costs. No verification details — those go in the session reviews and issue logs.

```json
{
  "sessionSetName": "dabbler-filtergrid-enhancements",
  "createdDate": "2026-04-07T14:00:00Z",
  "totalSessions": 7,
  "entries": [
    {
      "sessionNumber": 1,
      "stepNumber": 1,
      "stepKey": "session-1/scaffold",
      "dateTime": "2026-04-07T14:05:00Z",
      "description": "Created demo project scaffold",
      "status": "complete",
      "routedApiCalls": [
        {
          "model": "gemini-2.5-flash",
          "taskType": "code-review",
          "inputTokens": 1200,
          "outputTokens": 800,
          "costUsd": 0.0007
        }
      ]
    },
    {
      "sessionNumber": 1,
      "stepNumber": 2,
      "stepKey": "session-1/basic-demo",
      "dateTime": "2026-04-07T14:12:00Z",
      "description": "Created BasicDemo page",
      "status": "complete",
      "routedApiCalls": []
    }
  ]
}
```

**Notes:**
- Steps that are pure file creation (no router calls) have an empty `routedApiCalls` array.
- `status` values: `"complete"`, `"failed"`.
- Sonnet (Claude Code) token usage is **not tracked** — it's the orchestrator and cannot introspect its own consumption. Sonnet usage appears in Anthropic's billing dashboard. The log tracks only routed API calls where cost optimization matters.

### 7.4 Session Review Files (`session-reviews/session-NNN.md`)

The verifier's raw response, pasted in **untouched**. Sonnet does not edit these files.

If the verifier runs multiple rounds (because Sonnet fixed issues and re-verified), each round is appended with a separator:

```markdown
# Verification Round 1

**ISSUES FOUND**

**Issue 1:** FilterPreset.ColumnVisibility is declared but never populated...
...

---

# Verification Round 2

**VERIFIED** — All prior issues have been addressed. The implementation matches the spec.
```

This is the audit trail. When the human wants to know what the verifier actually said, they read this file.

### 7.5 Issue Log Files (`issue-logs/session-NNN.json`)

A JSON array where Sonnet tracks each issue and its resolution:

```json
[
  {
    "issue": "FilterPreset.ColumnVisibility is declared but never populated during ExportFilter()",
    "severity": "Major",
    "status": "fixed",
    "response": "Added ColumnVisibility population in ExportFilter() at line 142 of DabblerFilterGrid.razor.cs"
  },
  {
    "issue": "No null check on QuickFilterColumns when generating the OR composite",
    "severity": "Minor",
    "status": "deferred",
    "response": "Edge case when ShowQuickFilter=true but QuickFilterColumns is explicitly empty. Needs design decision on whether empty means 'none' or 'all'."
  }
]
```

- `status` is `"fixed"` or `"deferred"`.
- An issue log file is created for every session where the verifier found issues, **even if all issues were fixed**. This provides visibility into what was caught and resolved.
- If the verifier returns **VERIFIED** on the first round (no issues), no issue log file is created for that session.
- The human scans for `"deferred"` items to see what needs attention.

### 7.6 Change Log (`change-log.md`)

Generated by Claude Code on the **last session only**. Documents:
- All new files created across all sessions
- All existing files modified
- New classes, methods, and parameters
- Configuration changes
- Test coverage summary

This is the document uploaded to Claude Chat for Opus-level review.

### 7.7 Determining Current Session

Claude Code determines the next session number from the activity log:

1. Read `activity-log.json`
2. Find the highest `sessionNumber` that has all its steps logged as `"complete"`
3. Cross-check: does `session-reviews/session-NNN.md` exist for that session?
4. If both conditions are met, next session = that number + 1
5. If no sessions exist yet, start with session 1

```python
from ai_router.session_log import SessionLog

log = SessionLog("docs/session-sets/dabbler-filtergrid-enhancements")
next_session = log.get_next_session_number()  # e.g., 3
total_sessions = log.total_sessions            # e.g., 7
is_last = (next_session == total_sessions)
```

### 7.8 Session Log Module (`session_log.py`)

The `SessionLog` class manages the activity log and file structure for a session set. It replaces the SQLite-based state tracking with simple JSON files.

**Key methods:**

| Method | Purpose |
|--------|---------|
| `SessionLog(session_set_dir, total_sessions)` | Constructor. Loads existing log or creates new one with directory structure. |
| `get_next_session_number()` → `int` | Determines next session to execute based on completed sessions and review files. |
| `get_last_completed_session()` → `int` | Returns the highest completed session number, or 0. |
| `log_step(session_number, step_number, step_key, description, status, api_calls)` | Appends a step entry to the activity log. |
| `save_session_review(session_number, review_text, round_number)` | Saves or appends the verifier's raw output to `session-reviews/session-NNN.md`. |
| `save_issue_log(session_number, issues)` | Saves the structured issue log for a session. |
| `get_entries_for_session(session_number)` → `list[dict]` | Returns all activity log entries for a given session. |
| `get_cost_summary()` → `dict` | Aggregates cost data from all entries. |

See the `session_log.py` source file for the full implementation.

---

## 8. How Claude Code Uses the Router

Claude Code doesn't call the router for everything — it handles file operations and implementation directly. The router is for **offloading pure reasoning tasks to cheaper models**.

### 8.1 When to Use the Router

| Task | Use Router? | Auto-Verified? | Why |
|------|-------------|----------------|-----|
| Implement a class | **No** — do it directly | — | Needs file I/O, Sonnet can do this |
| Review code for bugs | **Yes** → route() | **Yes** | Pure reasoning, auto-verified cross-provider |
| Write documentation for a module | **Yes** → route() | No | Text generation, cheap model works |
| Run tests and fix failures | **No** — do it directly | — | Needs shell access and iteration |
| Analyze architecture tradeoffs | **Yes** → route() | No* | Complex reasoning; use verify() explicitly for critical decisions |
| Generate test cases | **Yes** → route() | No* | Enable in config if test accuracy matters |
| Rename variables across files | **No** — do it directly | — | Mechanical, Sonnet handles fine |
| Security review of auth module | **Yes** → route() | **Yes** | Specialized analysis, auto-verified cross-provider |

\* Enable auto-verification for any task type by adding it to `auto_verify_task_types` in `router-config.yaml`.

### 8.2 Example: Claude Code Session

Here's what a typical Claude Code session looks like with the router:

```python
from ai_router import route, verify
from ai_router.session_log import SessionLog

log = SessionLog("docs/session-sets/my-feature")
session = log.get_next_session_number()

# Step 1: Implement the ModelRegistration class (direct — file I/O)
# → Claude Code writes the file itself
log.log_step(session, 1, f"session-{session}/models",
             "Created ModelRegistration class", "complete")

# Step 2: Review the existing AppDbContext for patterns to follow
result = route(
    content=open("Data/AppDbContext.cs").read(),
    task_type="analysis",
    context="I need to add three new DbSets. What patterns does this "
            "file use for FK configuration, delete behavior, and "
            "composite keys? List them concisely."
)
# result.model_name is probably "gemini-flash" (low complexity)
# result.cost_usd is probably $0.0003
log.log_step(session, 2, f"session-{session}/pattern-review",
             "Reviewed AppDbContext patterns", "complete",
             api_calls=[{
                 "model": result.model_id,
                 "taskType": "analysis",
                 "inputTokens": result.input_tokens,
                 "outputTokens": result.output_tokens,
                 "costUsd": result.cost_usd
             }])

# Step 3: Security review of the auth middleware
result = route(
    content=open("Middleware/AuthMiddleware.cs").read(),
    task_type="security-review",
    context="Check for authentication bypass, token handling issues, "
            "and injection vulnerabilities."
)
# result.verification is populated (security-review IS auto-verified)
# result.verification.verified is True or False
# result.total_cost_usd includes both the review AND verification cost

if result.verification and not result.verification.verified:
    print("Verifier found issues — review the merged output carefully")

# Step 4: Print costs
from ai_router import print_cost_report
print_cost_report("docs/session-sets/my-feature")
```

---

## 9. The Session Set Workflow

### 9.1 Phase 1: Planning (Claude Chat, Opus)

**You do this in Claude Chat.**

1. Upload relevant project files to the Claude Chat project
2. Describe what you want built or changed
3. Claude Chat (Opus) produces a spec file with:
   - Feature design (types, parameters, behaviors)
   - Session breakdown (4–7 sessions, each self-contained)
   - Per-session: steps, creates, touches, ends-with criteria
4. Create the session set folder: `docs/session-sets/{session-set-name}/`
5. Save the spec as `docs/session-sets/{session-set-name}/spec.md`
6. Update CLAUDE.md to reference the session set folder

**That's it.** No separate session prompts file. CLAUDE.md contains the complete workflow.

#### Session Sizing Guidelines

| Factor | Guideline |
|--------|-----------|
| Steps per session | 4–6 implementation steps max |
| Files touched | 8–10 files max (creating + modifying combined) |
| Route calls | Each `route()` call consumes context — budget 3–5 per session |
| Tests | Every session must end with all tests passing |
| Independence | Each session starts from a clean state — reads progress, reviews files |

### 9.2 Phase 2: Implementation (Claude Code, Sonnet)

**You run one session at a time.** The human prompt is always:

```
Start the next session.
```

Claude Code (reading CLAUDE.md) will:

1. **Find the active session set** — read the folder path from CLAUDE.md
2. **Read the activity log** — open `activity-log.json` to find the next session number (or create it if this is session 1)
3. **Read the spec** — open `spec.md`, find the session plan for that number
4. **Verify prerequisites** — confirm prior session outputs exist
5. **Execute each step** — implement directly or route to cheap models
6. **Log each step** — append to `activity-log.json`
7. **Run build/tests** — confirm everything passes
8. **Run verification** — see §9.3
9. **Handle issues** — see §9.4
10. **Print cost report**

If it's the **last session**, also generate `change-log.md`.

### 9.3 End-of-Session Verification (Mandatory)

Every session ends with independent verification. This is not optional.

**What gets verified:** All files created or modified during the session are collected and sent to a verifier model from a different provider.

**How it works:**

1. Claude Code collects all files created or modified in the session
2. Builds a verification prompt using `prompt-templates/session-verification.md`:
   - The session's spec excerpt (what was supposed to be built)
   - The actual code produced
   - The build/test results
3. Routes cross-provider (if Sonnet did the work, verify with Gemini Pro)
4. **Saves the verifier's raw output** to `session-reviews/session-NNN.md` — untouched

### 9.4 Issue Handling

After saving the verifier's raw output to `session-reviews/session-NNN.md`:

**If VERIFIED:** Session is complete. No issue log is created for this session.

**If ISSUES FOUND:**

1. Parse the issues from the verifier's response
2. Create `issue-logs/session-NNN.json` with all issues, initially with `"status": "open"`
3. Attempt to fix each issue
4. Update each issue's `status` to `"fixed"` or `"deferred"` and add a `response` explaining what was done (or why it was deferred)
5. Re-run verification (append round 2 to the session review file)
6. Max 2 verification retries
7. Save the final issue log

### 9.5 Mid-Session Verification (via Router)

In addition to end-of-session verification, individual routed API calls can be auto-verified during the session (per Section 6). Task types in `auto_verify_task_types` are verified cross-provider automatically.

The two levels work together:
- **Task-level verification** catches issues in individual reasoning outputs
- **Session-level verification** catches issues in the aggregate implementation

### 9.6 Phase 3: Review (Claude Chat, Opus)

After the last session, upload `change-log.md` to Claude Chat for Opus-level review. Also review any `"deferred"` items across all issue logs.

### 9.7 The Loop Visualized

```
                     Human says:
                     "Start the next session"
                            │
┌──────────────┐            │           ┌──────────────────────────┐
│ Claude Chat   │  spec.md  │           │ Claude Code (Sonnet)     │
│ (Opus)        │ ────────► │           │                          │
│               │           │           │  1. Read CLAUDE.md       │
│ Plans work,   │           │           │  2. Read activity-log    │
│ creates spec  │           ▼           │     → determine session N│
│               │    ┌─────────────┐    │  3. Read spec → steps    │
│               │    │ CLAUDE.md   │    │  4. Execute steps        │
│ Reviews       │    │ (has full   │───►│  5. Log each step        │
│ change-log    │    │  workflow)  │    │  6. Build + test         │
│ after last    │    └─────────────┘    │  7. VERIFY (mandatory)   │
│ session       │                       │     → session-reviews/   │
│               │  change-log.md        │  8. Handle issues        │
│               │ ◄──────────────────── │     → issue-logs/        │
└──────────────┘                       │  9. Print cost report    │
                                       └──────────┬───────────────┘
                                                   │
                                  ┌────────────────┴────────────────┐
                                  │ docs/session-sets/{name}/       │
                                  │ ├── spec.md                     │
                                  │ ├── activity-log.json           │
                                  │ ├── change-log.md               │
                                  │ ├── session-reviews/            │
                                  │ │   └── session-NNN.md          │
                                  │ └── issue-logs/                 │
                                  │     └── session-NNN.json        │
                                  └─────────────────────────────────┘
```

---

## 10. Human Setup and Operation Guide

### 10.1 Prerequisites

- **VS Code** with the Claude Code extension installed
- **Python 3.11+** (check: `python3 --version`)
- **API keys** for Anthropic and Google AI

### 10.2 One-Time Setup

#### Step 1: Install Python dependencies

```bash
cd your-project
pip install -r requirements.txt
```

Or if you prefer an isolated environment:

```bash
python3 -m venv .venv
source .venv/bin/activate  # macOS/Linux
# or: .venv\Scripts\activate  # Windows
pip install -r requirements.txt
```

#### Step 2: Set API keys

Add to your shell profile (`~/.zshrc`, `~/.bashrc`, or `~/.profile`):

```bash
export ANTHROPIC_API_KEY="sk-ant-your-key-here"
export GEMINI_API_KEY="your-gemini-key-here"
```

Then reload: `source ~/.zshrc`

**To get API keys:**
- Anthropic: https://console.anthropic.com/settings/keys
- Google AI: https://aistudio.google.com/apikey

#### Step 3: Create the router files

Copy the following files into your project's `ai-router/` directory:
- `__init__.py` — Public API and orchestration
- `config.py` — Configuration loader
- `models.py` — Complexity estimation and model selection
- `providers.py` — Anthropic and Google API callers
- `verification.py` — Cross-provider IV&V logic
- `rate_limiter.py` — Per-provider rate limiting
- `session_log.py` — JSON session log tracking
- `prompting.py` — Model-specific prompt formatting
- `escalation.py` — Escalation detection and model upgrade logic
- `cli.py` — Command-line cost report

Copy `requirements.txt` to your project root.

Create `ai-router/router-config.yaml` inside the `ai-router/` directory — Section 3 of this document.

Create `prompt-templates/` with the files from Section 4.

#### Step 4: Create CLAUDE.md

Copy the CLAUDE.md template from Section 12 into your project root. Customize the project-specific sections.

#### Step 5: Verify setup

```bash
cd your-project
python3 -c "from ai_router import route; print('Router loaded OK')"
```

If you see `Router loaded OK`, you're set. If you get an error about missing API keys, check Step 2.

### 10.3 Per-Session Workflow

#### Planning (Claude Chat — once per session set)

1. Open your Claude Chat project at claude.ai
2. Upload relevant files
3. Describe the work; Claude Chat produces a spec with session breakdown
4. Create the folder: `docs/session-sets/{session-set-name}/`
5. Save the spec as `docs/session-sets/{session-set-name}/spec.md`
6. Update CLAUDE.md to point to the session set folder

#### Implementation (Claude Code — repeat for each session)

```
Start the next session.
```

That's the entire prompt. Claude Code handles everything: reading the log, finding the next session, executing, verifying, logging issues, and printing the cost report.

#### Checking progress

Open `docs/session-sets/{name}/activity-log.json` in any text or JSON viewer. The `entries` array shows every step completed with status and API costs.

To see what the verifier said: read `session-reviews/session-NNN.md`.

To see outstanding issues: scan `issue-logs/` for `"deferred"` items.

#### After the last session

Upload `docs/session-sets/{name}/change-log.md` to Claude Chat for Opus-level review. Also review any `"deferred"` items across all issue logs.

#### Checking costs anytime

```bash
python3 -m ai_router.cli --session-set docs/session-sets/{session-set-name}
```

Or in Claude Code: "Run `print_cost_report('docs/session-sets/{name}')` from the ai_router module."

### 10.4 Tips

- **Start with conservative thresholds.** The defaults (30/65) route most things to Tier 2. After a few sessions, check the cost report and lower `tier1_max_complexity` to route more to Flash.

- **Use `complexity_hint` sparingly.** Let the auto-estimation work. Override only when you know a task is deceptively simple or complex.

- **Check progress between sessions.** Open `docs/session-sets/{name}/activity-log.json` in any text or JSON viewer. Scan `issue-logs/` for `"deferred"` items.

- **The cost report tells you if thresholds need tuning.** If Flash is handling tasks well (no escalations from Tier 1), lower the threshold to send more to Flash. If you see frequent escalations, raise it.

- **Check verification stats.** If auto-verified tasks are always passing (100% VERIFIED), you might be over-verifying. If issues are found frequently, verification is earning its keep.

- **Don't over-plan in Claude Chat.** A spec that takes 3 hours to write costs more (in your time) than the dollars you'd save on API routing. Write specs at the level of "what to build," not "every line of code." Let Sonnet handle implementation details.

- **Session sizing is an art.** If Claude Code runs out of context in a session, make sessions smaller next time. If it finishes in 30 seconds, make them larger. The session plan is a starting estimate — adjust based on experience.

---

## 11. Example Walkthrough

Here's a complete example: adding a cost dashboard endpoint to a project.

### 11.1 Planning (Claude Chat)

You upload the relevant architecture docs and say:

> "I need a new API endpoint `GET /api/routing/costs` that returns cost summaries. Please design the endpoint, the service method, and the test cases."

Claude Chat produces a spec covering the response shape, service method, and 5 test cases. You save it as `docs/session-sets/cost-dashboard/spec.md`.

### 11.2 Implementation (Claude Code)

You tell Claude Code:

```
Start the next session.
```

Claude Code does this:

```
Step 1: Read the spec                          → direct
Step 2: Review existing controller patterns    → route("code-review")
        Router picks gemini-flash ($0.0002)
Step 3: Implement GetCostSummaryAsync()        → direct (writes file)
Step 4: Implement the controller endpoint      → direct (writes file)
Step 5: Generate test cases                    → route("test-generation")
        Router picks gemini-pro ($0.003)
Step 6: Write the test file                    → direct (writes file)
Step 7: Run tests                              → direct (shell command)
Step 8: Fix any test failures                  → direct
Step 9: End-of-session verification            → cross-provider (mandatory)
Step 10: Generate change-log.md                → route("documentation")
         Router picks gemini-flash ($0.0004)
```

Cost report:
```
AI ROUTER — COST REPORT
============================================================
Total routed API calls: 3
Total routed cost:      $0.0036
============================================================
```

### 11.3 Review (Claude Chat)

You upload `docs/session-sets/cost-dashboard/change-log.md`. Claude Chat reviews and says everything looks correct. Done.

**Total cost for the routed API calls: $0.004.** The same work at Opus rates would have been ~$0.09. And the implementation itself was handled by Claude Code at Sonnet rates, not Opus.

---

## 12. CLAUDE.md Instructions for Claude Code

Place this at your project root. Customize the bracketed sections.

```markdown
# CLAUDE.md — [Your Project Name]

## Project Overview
[Brief description of the project]

## AI Router

This project includes an AI Router module (`ai-router/`) that routes
reasoning tasks to cheaper AI models and verifies critical results
cross-provider. Use it to save costs and catch errors.

### When to use the router

Use `ai_router.route()` for tasks that are pure reasoning — no file
I/O needed. The router picks the cheapest model that can handle it.
For task types listed in `auto_verify_task_types` (code-review,
security-review, architecture), a second model from a different
provider automatically checks the result.

- Code review → `route(code, task_type="code-review")` ← auto-verified
- Security review → `route(code, task_type="security-review")` ← auto-verified
- Architecture → `route(desc, task_type="architecture")` ← auto-verified
- Documentation → `route(code, task_type="documentation")`
- Analysis → `route(question, task_type="analysis")`
- Test generation → `route(code, task_type="test-generation")`
- Planning subtasks → `route(description, task_type="planning")`

Do NOT use the router for:
- Creating or modifying files (do it directly)
- Running shell commands (do it directly)
- Anything that needs the filesystem (do it directly)

### How to use it

```python
from ai_router import route, verify, print_cost_report
from ai_router.session_log import SessionLog

# Initialize session log
log = SessionLog("docs/session-sets/{session-set-name}")
session = log.get_next_session_number()

# Auto-routed (router picks the model)
result = route(
    content="<the text to analyze or process>",
    task_type="code-review",
    context="<optional background>"
)
print(result.content)
print(result.model_name)
print(result.total_cost_usd)

# Log the step with API cost data
log.log_step(
    session_number=session,
    step_number=1,
    step_key=f"session-{session}/review",
    description="Code review of auth module",
    status="complete",
    api_calls=[{
        "model": result.model_id,
        "taskType": "code-review",
        "inputTokens": result.input_tokens,
        "outputTokens": result.output_tokens,
        "costUsd": result.total_cost_usd
    }]
)

# At session end
print_cost_report("docs/session-sets/{session-set-name}")
```

### Active Session Set

**Folder:** `docs/session-sets/[current-session-set-name]`

### Session Workflow (9-step process)

When the human says "Start the next session", follow these steps exactly:

1. **Read this file** (CLAUDE.md) and note the active session set folder
2. **Read the activity log** (`activity-log.json`) to determine the next
   session number. If no log exists, this is session 1 — create it.
3. **Read the spec** (`spec.md`) and find the session plan for that number
4. **Verify prerequisites** — confirm prior session outputs exist
5. **Execute each step:**
   - If it requires creating/modifying files → do it directly
   - If it's a reasoning task → use `ai_router.route()`
   - After each step, call `log.log_step(...)` to record progress
6. **Run build/tests** — all must pass
7. **Run end-of-session verification (MANDATORY):**
   - Collect all files created/modified in this session
   - Build verification prompt using `prompt-templates/session-verification.md`
   - Route cross-provider (use `ai_router.route()` with `task_type="session-verification"`)
   - Save raw verifier output: `log.save_session_review(session, review_text)`
8. **Handle issues:**
   - If VERIFIED → session is done, no issue log needed
   - If ISSUES FOUND → create issue log, attempt fixes, re-verify (max 2 retries)
   - Save issue log: `log.save_issue_log(session, issues)`
9. **Print cost report:** `print_cost_report("docs/session-sets/{name}")`
   - If this is the LAST session, also generate `change-log.md`

### Do NOT:
- Try to do more than your assigned session
- Skip end-of-session verification
- Edit the session review files (they are the audit trail)
- Continue to the next session without the human saying so

## Implementation Specs
[Link to current spec files, e.g.:]
- `docs/session-sets/[name]/spec.md` — current work

## Code Style
[Your project's conventions]

## Testing
[How to run tests, e.g.:]
```bash
dotnet test
```
```

---

## Appendix A: Quick Reference Card

```
SETUP:
  pip install -r requirements.txt
  export ANTHROPIC_API_KEY=sk-ant-...
  export GEMINI_API_KEY=AI...

USE IN CLAUDE CODE:
  from ai_router import route, verify, print_cost_report

  result = route(content, task_type="code-review")        # auto-verified
  result = route(content, task_type="documentation")       # not verified
  result = route(content, task_type="analysis", complexity_hint=80)

  check = verify(result, original_task=content)            # explicit verify
  check.verified    # True/False
  check.issues      # list of issues

  result.verification               # VerificationResult if auto-verified
  result.total_cost_usd             # includes verification cost

SESSION PROGRESS:
  from ai_router.session_log import SessionLog
  log = SessionLog("docs/session-sets/{session-set-name}")
  next = log.get_next_session_number()
  log.log_step(session_number=1, step_number=1, step_key="session-1/scaffold",
               description="Created scaffold", status="complete", api_calls=[])
  log.save_session_review(session_number=1, review_text="...", round_number=1)

CHECK COSTS:
  python3 -m ai_router.cli --session-set docs/session-sets/{session-set-name}

CONFIG:
  ai-router/router-config.yaml  ← models, thresholds, pricing, verification settings
  prompt-templates/*.md       ← system prompts, task templates, verification template
  docs/session-sets/{name}/   ← activity log, session reviews, issue logs, change log
```
