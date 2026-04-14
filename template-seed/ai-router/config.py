"""Load and validate router-config.yaml."""

import os
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

    return config
