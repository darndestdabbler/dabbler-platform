#!/usr/bin/env python3
"""
Command-line cost report for a session set.

Usage:
    python -m ai_router.cli --session-set docs/session-sets/{session-set-name}
"""

import sys
from .session_log import SessionLog


def main():
    session_set_dir = None
    if "--session-set" in sys.argv:
        idx = sys.argv.index("--session-set")
        session_set_dir = sys.argv[idx + 1]
    else:
        print("Usage: python -m ai_router.cli "
              "--session-set docs/session-sets/{session-set-name}")
        sys.exit(1)

    log = SessionLog(session_set_dir)
    costs = log.get_cost_summary()

    print("\n" + "=" * 60)
    print("AI ROUTER — COST REPORT")
    print(f"Session Set: {log._data['sessionSetName']}")
    print("=" * 60)
    print(f"Sessions completed: {costs['sessions_completed']} "
          f"of {log.total_sessions}")
    print(f"Sessions remaining: {costs['sessions_remaining']}")
    print(f"Total routed API calls: {costs['total_calls']}")
    print(f"Total routed cost:      ${costs['total_cost']:.4f}")
    print()

    if costs["by_model"]:
        print("By model:")
        for model, data in costs["by_model"].items():
            print(f"  {model:20s}  {data['calls']:3d} calls"
                  f"  ${data['cost']:.4f}")
    print("=" * 60 + "\n")


if __name__ == "__main__":
    main()
