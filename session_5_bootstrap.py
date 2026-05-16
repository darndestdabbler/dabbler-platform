import os
import json
from pathlib import Path

# Set router environment
os.environ['AI_ROUTER_CONFIG'] = 'ai_router/router-config.yaml'
os.environ['AI_ROUTER_METRICS_PATH'] = 'ai_router/router-metrics.jsonl'

# Import session utilities
from ai_router.session_log import SessionLog
from ai_router import register_session_start

# Step 1: Register session start
SESSION_SET = Path("docs/session-sets/crud-scaffolding")
log = SessionLog(SESSION_SET)
next_session = log.get_next_session_number()

print(f"Session set: {SESSION_SET}")
print(f"Next session number: {next_session}")

register_session_start(
    session_set=SESSION_SET,
    current_session=next_session,
    total_sessions=log.total_sessions,
    orchestrator={
        "engine": "claude-code",
        "provider": "anthropic",
        "model": "claude-haiku-4-5-20251001",
        "effort": "high",
    },
)

print("Session state written")

# Verify the state file was created
state_file = SESSION_SET / "session-state.json"
if state_file.exists():
    with open(state_file) as f:
        state = json.load(f)
    print(f"Status: {state['status']}")
    print(f"LifecycleState: {state['lifecycleState']}")
    print(f"CurrentSession: {state['currentSession']}")
