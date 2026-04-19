"""Load and validate router-config.yaml, and merge the optional
router-tuning.json overlay. Also exposes a helper to resolve the
effective generation params for a (model, task_type) pair."""

import os
import json
import copy
import yaml
from pathlib import Path

# Default config location is router-config.yaml in the same directory as this file.
# This keeps the default working regardless of where Python is invoked from.
_THIS_DIR = Path(__file__).parent


def load_config(path: str | None = None) -> dict:
    if path is None:
        path = str(_THIS_DIR / "router-config.yaml")
    config_path = Path(path)
    if not config_path.exists():
        raise FileNotFoundError(
            f"Router config not found: {path}\n"
            f"Create it from the template in the AI Router specification."
        )

    with open(config_path) as f:
        config = yaml.safe_load(f)

    # Validate API keys exist in environment
    for name, provider in config["providers"].items():
        env_var = provider["api_key_env"]
        if not os.environ.get(env_var):
            raise EnvironmentError(
                f"Missing environment variable {env_var} "
                f"for provider '{name}'. "
                f"Set it with: export {env_var}=your-key-here"
            )

    # Validate model references
    for tier, model_name in config["routing"]["tier_assignments"].items():
        if model_name not in config["models"]:
            raise ValueError(
                f"Tier {tier} references unknown model '{model_name}'. "
                f"Available: {list(config['models'].keys())}"
            )

    # Resolve prompt template file paths relative to config file location.
    # The integrated repo stores templates under ai-router/prompt-templates,
    # but we keep a sibling fallback so older layouts still load cleanly.
    config_dir = config_path.parent
    prompt_roots = [
        config_dir / "prompt-templates",
        config_dir.parent / "prompt-templates",
    ]

    def resolve_relative_path(relative_path: str) -> Path | None:
        candidate = config_dir / relative_path
        if candidate.exists():
            return candidate

        for root in prompt_roots:
            if not root.exists():
                continue

            root_name = root.name
            normalized = relative_path.replace("\\", "/")
            if normalized.startswith(f"{root_name}/"):
                nested = root / normalized[len(root_name) + 1:]
                if nested.exists():
                    return nested

        return None

    for model_name, model_cfg in config["models"].items():
        prompt_file = model_cfg.get("system_prompt_file")
        if prompt_file:
            full_path = resolve_relative_path(prompt_file)
            if full_path is not None:
                model_cfg["_system_prompt"] = full_path.read_text().strip()
            else:
                model_cfg["_system_prompt"] = (
                    "You are an expert software engineer. "
                    "Be direct and precise."
                )

    config["_task_templates"] = {}
    for _pt_root in prompt_roots:
        template_dir = _pt_root / "task-types"
        if not template_dir.exists():
            continue

        for f in template_dir.glob("*.md"):
            task_type = f.stem
            config["_task_templates"][task_type] = f.read_text().strip()

        if config["_task_templates"]:
            break

    # Resolve verification template
    v_config = config.get("verification", {})
    v_template_file = (v_config.get("settings", {})
                       .get("prompt_template_file"))
    if v_template_file:
        v_path = resolve_relative_path(v_template_file)
        if v_path is not None:
            config["_verification_template"] = v_path.read_text().strip()
        else:
            config["_verification_template"] = ""
    else:
        config["_verification_template"] = ""

    # --- Tuning overlay (router-tuning.json) ---------------------------------
    # Optional per-run overlay. Keys present here override the YAML.
    tuning_path = os.environ.get("AI_ROUTER_TUNING")
    if tuning_path is None:
        tuning_path = str(config_dir / "router-tuning.json")

    tuning: dict = {}
    if Path(tuning_path).exists():
        with open(tuning_path) as f:
            try:
                tuning = json.load(f)
            except json.JSONDecodeError as e:
                raise ValueError(
                    f"router-tuning.json is not valid JSON: {e}"
                )

    config["_tuning"] = tuning
    config["_tuning_applied"] = _summarize_tuning(tuning) if tuning else []

    # Apply tuning: max_tier cap
    if isinstance(tuning.get("max_tier"), dict):
        cap = tuning["max_tier"].get("value")
        if isinstance(cap, int) and 1 <= cap <= 3:
            config["routing"]["_max_tier_cap"] = cap

    return config


def _summarize_tuning(tuning: dict) -> list[str]:
    """Produce a short list of 'what got overridden' strings, for logging."""
    notes = []
    for key in (
        "max_tier",
        "effort_by_task_type",
        "adaptive_thinking_by_task_type",
        "thinking_budget_by_task_type_gemini25",
        "thinking_level_by_task_type_gemini3",
        "reasoning_effort_by_task_type_openai",
    ):
        if key in tuning:
            block = tuning[key]
            if isinstance(block, dict):
                n = sum(1 for k in block if not k.startswith("_"))
                notes.append(f"{key}({n} entries)")
            else:
                notes.append(key)
    return notes


def resolve_generation_params(
    model_name: str,
    task_type: str,
    config: dict,
) -> dict:
    """Resolve effective generation_params for a (model, task_type) pair.

    Precedence (lowest to highest):
      1. Model-level defaults: config['models'][model_name]['generation_params']
      2. Per-task-type override: config['task_type_params'][task_type][model_name]
      3. Tuning overlay (router-tuning.json) — applied last so the human wins.

    Returns a dict with any subset of:
      effort, thinking (dict), thinking_budget (int), thinking_level (str).
    """
    model_cfg = config["models"].get(model_name, {})
    params = copy.deepcopy(model_cfg.get("generation_params", {}) or {})

    # Per-task-type YAML override
    tt_block = config.get("task_type_params", {}).get(task_type, {}) or {}
    overrides = tt_block.get(model_name, {}) or {}
    params = _deep_merge(params, overrides)

    # Tuning overlay (runtime, human-editable)
    tuning = config.get("_tuning", {}) or {}
    provider = model_cfg.get("provider", "")

    if provider == "anthropic":
        effort_map = tuning.get("effort_by_task_type", {}) or {}
        if task_type in effort_map:
            params["effort"] = effort_map[task_type]

        adapt_map = tuning.get("adaptive_thinking_by_task_type", {}) or {}
        if task_type in adapt_map:
            want = bool(adapt_map[task_type])
            thinking = params.get("thinking", {}) or {}
            thinking = {**thinking, "enabled": want}
            if want and "type" not in thinking:
                thinking["type"] = "adaptive"
            params["thinking"] = thinking

    elif provider == "google":
        model_id = model_cfg.get("model_id", "")
        if model_id.startswith("gemini-3"):
            lvl_map = tuning.get(
                "thinking_level_by_task_type_gemini3", {}
            ) or {}
            if task_type in lvl_map:
                params["thinking_level"] = lvl_map[task_type]
                # thinking_level and thinking_budget are mutually exclusive
                params.pop("thinking_budget", None)
        else:
            budget_map = tuning.get(
                "thinking_budget_by_task_type_gemini25", {}
            ) or {}
            if task_type in budget_map:
                params["thinking_budget"] = budget_map[task_type]

    elif provider == "openai":
        effort_map = tuning.get(
            "reasoning_effort_by_task_type_openai", {}
        ) or {}
        if task_type in effort_map:
            params["reasoning_effort"] = effort_map[task_type]

    return params


def _deep_merge(base: dict, override: dict) -> dict:
    """Return a new dict where override keys win. Nested dicts merge."""
    out = copy.deepcopy(base)
    for k, v in (override or {}).items():
        if (
            k in out
            and isinstance(out[k], dict)
            and isinstance(v, dict)
        ):
            out[k] = _deep_merge(out[k], v)
        else:
            out[k] = copy.deepcopy(v)
    return out
