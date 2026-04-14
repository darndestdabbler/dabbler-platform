"""
kill_conhost.py — Terminate all conhost.exe process trees.

Run this after a session set completes to clean up orphaned Console Window Host
processes left behind by .NET test/build runners.

Usage:
    python ai-router/utils/kill_conhost.py
    python ai-router/utils/kill_conhost.py --dry-run      # print what would be killed without killing

Requires Windows. Uses tasklist + taskkill (both ship with Windows).
"""

import subprocess
import sys


def find_pids(process_name: str) -> list[int]:
    """Return a list of PIDs for all running instances of process_name."""
    result = subprocess.run(
        ["tasklist", "/FI", f"IMAGENAME eq {process_name}", "/FO", "CSV", "/NH"],
        capture_output=True,
        text=True,
    )
    pids: list[int] = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line or line.startswith("INFO:"):
            continue
        # CSV format: "conhost.exe","1234","Console","1","5,316 K"
        parts = [p.strip('"') for p in line.split('","')]
        if len(parts) >= 2:
            try:
                pids.append(int(parts[1]))
            except ValueError:
                pass
    return pids


def kill_tree(pid: int, dry_run: bool = False) -> tuple[bool, str]:
    """
    Kill a process and its entire child tree using taskkill /F /T.
    Returns (success, message).
    """
    if dry_run:
        return True, f"[dry-run] would kill tree rooted at PID {pid}"

    result = subprocess.run(
        ["taskkill", "/F", "/T", "/PID", str(pid)],
        capture_output=True,
        text=True,
    )
    succeeded = result.returncode == 0
    output = (result.stdout + result.stderr).strip()
    return succeeded, output


def main() -> None:
    dry_run = "--dry-run" in sys.argv

    if sys.platform != "win32":
        print("ERROR: kill_conhost.py is Windows-only (uses tasklist/taskkill).")
        sys.exit(1)

    pids = find_pids("conhost.exe")

    if not pids:
        print("No conhost.exe processes found.")
        return

    print(f"Found {len(pids)} conhost.exe process(es): {pids}")
    if dry_run:
        print("Dry-run mode — no processes will be killed.\n")

    killed = 0
    failed = 0
    for pid in pids:
        ok, msg = kill_tree(pid, dry_run=dry_run)
        status = "OK" if ok else "FAILED"
        print(f"  PID {pid}: [{status}] {msg}")
        if ok:
            killed += 1
        else:
            failed += 1

    print(f"\nDone. {killed} tree(s) terminated, {failed} failed.")
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
