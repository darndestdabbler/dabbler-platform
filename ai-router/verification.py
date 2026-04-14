"""Cross-provider verification logic for IV&V."""

import re
from typing import Optional


def pick_verifier_model(
    generator_model: str,
    config: dict
) -> Optional[str]:
    """
    Pick a verification model from a different provider.
    Returns model name from cross_provider_map, or None if unavailable.
    """
    v_config = config.get("verification", {})
    cross_map = v_config.get("cross_provider_map", {})

    if generator_model in cross_map:
        verifier = cross_map[generator_model]
        if verifier in config["models"]:
            # Confirm it's actually a different provider
            gen_provider = config["models"][generator_model]["provider"]
            ver_provider = config["models"][verifier]["provider"]
            if gen_provider != ver_provider:
                return verifier

    # Fallback: find any enabled model from a different provider
    gen_provider = config["models"][generator_model]["provider"]
    gen_tier = config["models"][generator_model]["tier"]

    # Prefer same tier, then adjacent tiers
    candidates = []
    for name, cfg in config["models"].items():
        if cfg["provider"] != gen_provider and cfg.get("is_enabled", True):
            tier_distance = abs(cfg["tier"] - gen_tier)
            candidates.append((tier_distance, cfg["tier"], name))

    if candidates:
        candidates.sort()  # closest tier first
        return candidates[0][2]

    return None


def build_verification_prompt(
    original_task: str,
    original_response: str,
    task_type: str,
    template: str = ""
) -> str:
    """Build the verification prompt from template or default."""
    if template:
        return (template
                .replace("{original_task}", original_task or "(not provided)")
                .replace("{original_response}", original_response)
                .replace("{task_type}", task_type))

    # Default template if no file was configured
    return (
        "## Independent Verification\n\n"
        "A different AI model completed the task below. "
        "Check its work for errors, omissions, and incorrect reasoning.\n\n"
        f"### Original Task\n\n{original_task or '(not provided)'}\n\n"
        f"### Task Type: {task_type}\n\n"
        f"### Response Under Review\n\n{original_response}\n\n"
        "### Instructions\n\n"
        "Evaluate for:\n"
        "1. **Correctness** — errors, flaws, wrong conclusions\n"
        "2. **Completeness** — missing items the task required\n"
        "3. **False Positives** — issues flagged that aren't real\n\n"
        "Start with **VERIFIED** or **ISSUES FOUND**, then explain.\n"
        "Do NOT re-do the task. Only evaluate what was produced."
    )


def parse_verification_response(response: str) -> tuple[str, list]:
    """
    Parse the verifier's response into a verdict and issue list.

    Returns:
        (verdict, issues) where verdict is "VERIFIED" or "ISSUES_FOUND"
        and issues is a list of dicts with keys:
        description, category, severity
    """
    upper = response.upper().strip()

    if upper.startswith("VERIFIED") or upper.startswith("**VERIFIED**"):
        return "VERIFIED", []

    # Default to ISSUES_FOUND for anything that isn't a clear VERIFIED
    verdict = "ISSUES_FOUND"

    # Parse individual issues
    issues = []
    # Look for patterns like "Issue 1:", "**Issue 1:**", "- Issue:"
    issue_pattern = re.compile(
        r'\*?\*?Issue\s*\d*\*?\*?\s*[:.]?\s*(.*?)(?=\*?\*?Issue\s*\d|\Z)',
        re.IGNORECASE | re.DOTALL
    )

    matches = issue_pattern.findall(response)
    for match in matches:
        issue = {"description": match.strip()}

        # Try to extract category
        cat_match = re.search(
            r'\*?\*?Category\*?\*?\s*[:.]?\s*([\w\s]+)',
            match, re.IGNORECASE
        )
        if cat_match:
            issue["category"] = cat_match.group(1).strip()

        # Try to extract severity
        sev_match = re.search(
            r'\*?\*?Severity\*?\*?\s*[:.]?\s*(Critical|Major|Minor)',
            match, re.IGNORECASE
        )
        if sev_match:
            issue["severity"] = sev_match.group(1).strip()

        if issue["description"]:
            issues.append(issue)

    # If no structured issues found but verdict is ISSUES_FOUND,
    # treat the whole response as one issue
    if not issues and verdict == "ISSUES_FOUND":
        # Strip the "ISSUES FOUND" header
        body = re.sub(
            r'^\*?\*?ISSUES?\s*FOUND\*?\*?\s*[-:.]?\s*',
            '', response, flags=re.IGNORECASE
        ).strip()
        if body:
            issues.append({
                "description": body,
                "category": "unknown",
                "severity": "unknown"
            })

    return verdict, issues
