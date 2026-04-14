"""Escalation detection and model upgrade logic."""

from .providers import APIResult


def should_escalate(result: APIResult, config: dict) -> bool:
    """Check if a response indicates the model couldn't handle the task."""
    triggers = config["triggers"]

    if triggers.get("empty_response") and not result.content.strip():
        return True

    if triggers.get("max_tokens_hit") and result.stop_reason == "max_tokens":
        return True

    min_tokens = triggers.get("min_output_tokens", 30)
    if result.output_tokens < min_tokens:
        return True

    if triggers.get("refusal_detection"):
        lower = result.content.lower()
        for phrase in config.get("refusal_phrases", []):
            if phrase in lower:
                return True

    return False


def get_escalation_model(
    current_model: str,
    config: dict,
    escalation_count: int
) -> str | None:
    """Return the next-tier model, or None if max escalations reached."""
    max_esc = config["escalation"]["max_escalations"]
    if escalation_count >= max_esc:
        return None

    current_tier = config["models"][current_model]["tier"]
    next_tier = current_tier + 1

    assignments = config["routing"]["tier_assignments"]
    if next_tier in assignments:
        return assignments[next_tier]

    return None
