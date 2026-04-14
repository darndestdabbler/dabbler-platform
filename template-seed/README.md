# Workflow Template Seed

This folder is the smallest reusable starter for the AI-led workflow used in
`dabbler-platform`.

It includes:

- a ready-to-copy `ai-router/` folder
- root instruction-file templates for Codex, Claude, and Gemini
- the shared workflow doc
- a starter session-set spec skeleton
- a placeholder-replacement script for bootstrapping a new repo

## Placeholders

Replace these tokens after copying the seed into a new repo:

- `{{PROJECT_NAME}}`
- `{{REPO_ROOT}}`
- `{{APP_NAME}}`
- `{{SESSION_SET_NAME}}`

Run:

```powershell
.\scripts\replace-placeholders.ps1 -ProjectName "My Project" -AppName "MyApp" -SessionSetName "initial-bootstrap"
```

## Next Steps

1. Create a Python virtual environment and install `ai-router/requirements.txt`.
2. Set `ANTHROPIC_API_KEY` and `GEMINI_API_KEY` as user environment variables.
3. Rename the starter session-set folder if needed.
4. Update the root instruction files with repo-specific build/test commands.
5. Ask the orchestrator to "Start the next session".
