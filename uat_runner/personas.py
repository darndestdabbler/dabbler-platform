"""Persona inference for UAT checklists.

The W0 SemanticValidator filters the catalog by ``activeRole``; when the
runner does not pass one the validator falls back to the AnyRole wildcard
``"*"``, which loses every entry stored under a concrete role and produces
silent zero-hit drift on real UAT routes.

Catalog-grounded checklists already encode the active persona in the
sign-in step (every checklist authored after the watermark integration
landed has ``CLICK INPUT with label "Email address" and enter "<email>"``
within the first ~5 steps). This module reads that step, resolves the
email to a role via :data:`personas.json`, and surfaces the result so the
runner can pass ``--active-role <name>`` to the W0 CLI.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

PERSONAS_PATH = Path(__file__).resolve().parent / "personas.json"

_EMAIL_STEP_RE = re.compile(
    r"""CLICK\s+INPUT\b.*?
        with\s+label\s+"(?:Email|Email\s+address)"\s+
        and\s+enter\s+"([^"]+)" """,
    re.IGNORECASE | re.VERBOSE | re.DOTALL,
)

_LOOKAHEAD_STEPS = 5


@dataclass(frozen=True)
class PersonaInferenceResult:
    """Outcome of inferring an active role from a checklist's first
    sign-in step.

    Exactly one of ``role`` or ``error`` is populated when ``email`` is
    set; both are ``None`` (and ``email`` is ``None`` too) when no
    sign-in step was found in the lookahead window — the caller should
    treat that as "no role context, fall back to AnyRole".
    """

    email: str | None
    role: str | None
    error: str | None


def load_persona_map(path: Path | str | None = None) -> dict[str, str]:
    """Load the email -> role map from disk.

    Returns a case-insensitive dictionary keyed on lowercase email.
    """
    target = Path(path) if path is not None else PERSONAS_PATH
    raw = json.loads(target.read_text(encoding="utf-8"))
    personas = raw.get("personas", {}) if isinstance(raw, dict) else {}
    return {str(email).lower(): str(role) for email, role in personas.items()}


def _iter_step_actions(checklist: dict) -> Iterable[str]:
    reviews = checklist.get("Review") or []
    for review in reviews:
        for step in review.get("Steps") or []:
            action = step.get("Action")
            if isinstance(action, str) and action.strip():
                yield action


def _extract_email_from_steps(checklist: dict) -> str | None:
    """Return the email from the first ``CLICK INPUT ... Email ...`` step,
    or ``None`` if none of the first :data:`_LOOKAHEAD_STEPS` actions match.
    """
    for index, action in enumerate(_iter_step_actions(checklist)):
        if index >= _LOOKAHEAD_STEPS:
            return None
        match = _EMAIL_STEP_RE.search(action)
        if match:
            return match.group(1).strip()
    return None


def infer_active_role(
    checklist_path: str | Path,
    persona_map: dict[str, str] | None = None,
) -> PersonaInferenceResult:
    """Infer the active role for a checklist by scanning the first
    sign-in step for an email and looking it up in :data:`personas.json`.

    Returns:
        - ``email=<addr>, role=<name>, error=None`` on a clean match.
        - ``email=<addr>, role=None, error=<msg>`` when an email was
          extracted but the persona map has no entry — the caller should
          surface this rather than silently falling back to AnyRole, so a
          stale persona file is not papered over.
        - ``email=None, role=None, error=None`` when no sign-in step was
          found in the lookahead window — the caller falls back to
          AnyRole and proceeds.
    """
    path = Path(checklist_path)
    checklist = json.loads(path.read_text(encoding="utf-8"))

    email = _extract_email_from_steps(checklist)
    if email is None:
        return PersonaInferenceResult(email=None, role=None, error=None)

    if persona_map is None:
        persona_map = load_persona_map()

    role = persona_map.get(email.lower())
    if role is None:
        return PersonaInferenceResult(
            email=email,
            role=None,
            error=(
                f"Persona '{email}' has no entry in personas.json. "
                f"Add it (with the role recorded in SeedData.cs "
                f"GetUserRoleAssignments) or pass --active-role explicitly."
            ),
        )
    return PersonaInferenceResult(email=email, role=role, error=None)
