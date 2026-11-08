# Verification Round 1

# Session 2 Review — dabbler-nuget-feed-publication

Pipeline built (build.yml, OIDC trusted publishing) and **first version published**: all 15
Dabbler.* packages live on nuget.org at 0.1.0-preview.1.

## Operator disposition of the cross-provider verdict (raw verdict preserved below, unedited)
- **Major #2 — downgrade SDK 10.0.x → 8.0.x: REJECTED (false positive).** Gemini's knowledge
  cutoff predates .NET 10 GA. The platform targets `net10.0`; SDK 8.0.x would fail the build.
- **Major #1 — pin Action `uses:` to commit SHAs: ACCEPTED as deferred hardening follow-up.**
  Valid supply-chain best practice; not a defect in the delivered pipeline. Tracked for follow-up.
- **Minor #2 — workflow `name:` vs filename: APPLIED** (`name: Build And Publish`).
- **Minor #1 — single wildcard push vs loop: DECLINED.** The loop gives clearer per-package
  logging and works correctly with `--skip-duplicate`.
- **Net: no blocking defects.** The pipeline already published successfully (empirical proof).

---

## Raw cross-provider verdict — Gemini 2.5 Pro (unedited)

VERDICT: ISSUES_FOUND

The pipeline is well-structured and successfully implements the core requirements from Session 1, including the crucial transition to OIDC trusted publishing. The build-once/promote-artifact pattern, concurrency controls, and robust validation steps are excellent. However, several latent robustness and security hardening issues were identified.

### Major Findings

1.  **Security: Action versions are not pinned to immutable SHAs.** The workflow uses floating major-version tags for all actions (e.g., `actions/checkout@v4`, `NuGet/login@v1`). This exposes the pipeline to a supply-chain risk where a compromised key could push a malicious update to a tag, which would then be automatically pulled into this critical publish workflow.
    -   **Recommendation:** Pin all `uses:` clauses to a specific commit SHA (e.g., `actions/checkout@de4439c01340b0642a8b9e6931b26c045b413009` instead of `actions/checkout@v4`).

2.  **Robustness: .NET SDK version is pinned to a non-existent/future version.** The workflow specifies `dotnet-version: 10.0.x`. While this may have resolved to a nightly/preview build available on the runner, a production publication pipeline must be pinned to a specific, stable, and supported SDK version (preferably LTS, like `8.0.x`). Using a preview SDK introduces risks of instability, breaking changes, and non-reproducible builds.
    -   **Recommendation:** Change `dotnet-version` to a stable LTS or STS version, such as `8.0.x`.

### Minor Findings

1.  **Efficiency: NuGet packages are pushed individually in a loop.** The `publish-nuget-org` and `dry-run-github-packages` jobs iterate over the found packages and issue a `dotnet nuget push` command for each one. This is less efficient than a single command. While functionally correct, a single command is more atomic and reduces network overhead.
    -   **Recommendation:** Replace the `foreach` loop with a single push command that uses a wildcard, e.g., `dotnet nuget push "$publishDir\*.nupkg" --source ... --api-key ...`.

2.  **Clarity: Inconsistent workflow name vs. filename.** The workflow is defined in `.github/workflows/build.yml` but has the display `name: NuGet Publish`. This is a minor discrepancy that can cause confusion when referencing the workflow.
    -   **Recommendation:** For clarity, either rename the file to `nuget-publish.yml` or change the `name` property to something that reflects the file's broader potential scope (if any), like `Build and Publish`.
