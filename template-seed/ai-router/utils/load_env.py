"""
load_env.py — Load API keys from Windows OS user environment variables into the current process.

Usage (from bash/Claude Code terminal):
    export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
    export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')

Or run this script directly to verify keys are loaded:
    .venv/Scripts/python.exe ai-router/utils/load_env.py
"""

import subprocess
import os
import sys


def load_key(name: str) -> str | None:
    """Read a Windows user-scope environment variable via PowerShell."""
    result = subprocess.run(
        ["powershell", "-Command",
         f"[System.Environment]::GetEnvironmentVariable('{name}', 'User')"],
        capture_output=True, text=True
    )
    value = result.stdout.strip()
    return value if value else None


def load_api_keys() -> bool:
    """Load ANTHROPIC_API_KEY and GEMINI_API_KEY from Windows user env into os.environ.
    Returns True if both keys are present after loading.
    """
    for key in ("ANTHROPIC_API_KEY", "GEMINI_API_KEY"):
        if not os.environ.get(key):
            value = load_key(key)
            if value:
                os.environ[key] = value

    missing = [k for k in ("ANTHROPIC_API_KEY", "GEMINI_API_KEY") if not os.environ.get(k)]
    if missing:
        print(f"ERROR: Still missing environment variables: {missing}", file=sys.stderr)
        return False

    print("API keys OK")
    return True


if __name__ == "__main__":
    if not load_api_keys():
        sys.exit(1)
