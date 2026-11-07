Excellent. This is a critical inflection point for the Dabbler platform. Moving from manual distribution to a public feed is a one-way door, and getting the fundamentals right is paramount. As the reviewing architect, my recommendations are designed for long-term stability, developer experience (DevEx), and operational safety, especially given the scale of "hundreds of consumer repos."

Here are my decisions on the four open questions.

---

### 1. Versioning scheme + consumer upgrade story

**Recommendation:**

Adopt **Semantic Versioning 2.0.0**, using a tag-driven `Major.Minor.Patch-preview.N` scheme for prereleases.

1.  **Preview Version Scheme:** `X.Y.Z-preview.N`
    *   Example: `0.1.0-preview.1`, `0.1.0-preview.2`, `0.2.0-preview.1`.
    *   The `.` between `preview` and `N` is critical. NuGet correctly orders `preview.10` as greater than `preview.9`. Without the dot (`preview10` vs `preview9`), it would perform a string comparison, which is incorrect.
    *   This scheme is clean, universally understood, and correctly sorted by all modern NuGet clients. Avoid date stamps or build counters in the public version string; they are noisy and convey little useful information about feature progression to the consumer.

2.  **Source of `N` (Determinism):**
    *   The version number, including the preview number `N`, must be **derived directly from a git tag**. The CI pipeline will not invent or increment the version; it will only consume it.
    *   **Trigger:** The publish workflow will trigger *only* on a tag push matching the pattern `v*.*.*` (e.g., `v0.1.0-preview.1`).
    *   **Mechanism:** The CI job will extract `0.1.0-preview.1` from the tag `v0.1.0-preview.1` and pass it to the build: `/p:DabblerVersion=0.1.0-preview.1`. This is fully deterministic and auditable. Re-running the pipeline on the same tag produces the exact same version.

3.  **Consumer Upgrade Story:**
    *   A consumer pins to a specific version, e.g., `<PackageReference Include="Dabbler.Components" Version="0.1.0-preview.5" />`.
    *   They can discover new prereleases by browsing the feed or checking the project's releases page. The monotonic increment of `N` makes it obvious which version is newer.
    *   Wildcard pinning (`0.1.0-preview.*`) is possible but not recommended for consumer production CI, as it introduces non-determinism. The template should generate projects with pinned versions.

4.  **Path to Stable `1.0.0`:**
    *   When the platform is deemed stable, the maintainer will push a tag **without a prerelease suffix**, e.g., `v1.0.0`. The CI pipeline will build and publish version `1.0.0`. Subsequent patches or features follow the same tagging pattern (`v1.0.1`, `v1.1.0`).

**Rationale:**
This tag-driven approach places the act of versioning on the human developer, which is where the intent lies. It makes releases deliberate acts, not side effects of a CI run. It is the most robust and widely adopted pattern for library publishing and completely avoids the ambiguity of your current versioning strings.

**Risks:**
- **Human Error:** A developer could tag `v0.1.0-preview.2` twice on different commits. Git prevents this on the same branch, but it's a discipline point. The CI pipeline's use of `--skip-duplicate` (see section 4) will prevent a re-publish, but the tag itself might need manual correction. This is an acceptable risk.

---

### 2. Public-vs-internal package split

**Recommendation:**

**Publish all 15 packages to nuget.org.** There will be no split.

Here is the explicit allowlist and justification:

| Package ID                           | Publish? | Justification                                                                                                                              |
| ------------------------------------ | :------: | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Runtime Libraries (8)**            | **Yes**  | The core value. Consumers absolutely need these to build their apps.                                                                       |
| `Dabbler.Components.Compat`          | **Yes**  | Critical for the consumer upgrade path. Must be available on the same feed as the packages it provides compatibility for.                  |
| **Stubs & Testing (4)**              | **Yes**  | Consumers need these to build their own tests (e.g., with bUnit) and to run their app's CI. Hiding them creates the exact friction we are removing. Publishing them encourages a robust testing culture among consumers. |
| `Dabbler.Security.DevIdp.Tool`       | **Yes**  | This is a `dotnet tool` essential for the developer loop. The canonical distribution mechanism for public tools is nuget.org. The 20 MB size is a one-time download cost for a developer, not a recurring cost for the application runtime. The DevEx win is enormous. |
| `Dabbler.Templates`                  | **Yes**  | This is the **front door** to the entire platform. It must be on nuget.org to be discoverable and installable via `dotnet new install`. |

**Rationale:**
The primary goal is a frictionless, self-contained developer experience. Any package a developer needs to install, reference, or use to build, test, or run their generated application must be on the public feed. Creating a split would force consumers to configure a second, private feed or resort to hand-copying packages, reintroducing the exact problem we are solving. At the scale of hundreds of repos, simplicity and a single source of truth are non-negotiable.

**Risks:**
- **Public Surface Area:** Publishing the `.Stubs` and `.Testing` packages makes their APIs part of the public contract. This is a manageable responsibility; their scope is clearly for testing, and breaking changes can be communicated in release notes.
- **Perceived Clutter:** Some may see a 20 MB tool or stub packages as "clutter." This is a minor aesthetic concern that is dwarfed by the major functional benefit of having a complete, ready-to-use platform available from a single source.

---

### 3. Local `packages/` fallback in the template

**Recommendation:**

**Remove the local `packages/` source and the corresponding `nuget.config` entry from the template entirely.**

The generated `nuget.config` should be minimal, containing only what is necessary to restore from the public feed and perhaps a `clear` directive to prevent environmental contamination.

```xml
<!-- nuget.config generated by the Dabbler.Templates -->
<configuration>
  <packageSources>
    <!-- Remove any sources inherited from user/machine-level configs -->
    <clear />
    <!-- Define nuget.org as the single, authoritative source -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <!-- Explicitly map all Dabbler.* packages to nuget.org -->
    <packageSource key="nuget.org">
      <package pattern="Dabbler.*" />
      <package pattern="Microsoft.*" />
      <package pattern="System.*" />
      <!-- Add other common top-level dependencies -->
    </packageSource>
  </packageSourceMapping>
</configuration>
```

**Rationale:**
The local `packages/` folder is a massive footgun. At the scale of hundreds of automated CI jobs, the risk of a stale local package silently overriding a security patch from the public feed is unacceptable. It creates a class of "works on my machine" bugs that are impossible to debug remotely. The primary goal is reliability and predictability. A single, authoritative online source provides that.

For the advanced use case of a platform developer testing a locally built package, the solution is documentation: instruct them on how to add a temporary local source to their *own* `nuget.config`, outside of what the template provides. We must not burden 100% of consumers with a complex and risky configuration to serve a 1% use case.

**Risks:**
- **Perceived Inconvenience for Contributors:** Contributors to the Dabbler platform itself lose the out-of-the-box convenience of testing local builds. This is a small, one-time setup cost for a sophisticated user, and a risk worth mitigating for the entire consumer base.

---

### 4. Publish-pipeline mechanics

**Recommendation:**

Implement a GitHub Actions workflow triggered by version tags, with strong guardrails and an emphasis on idempotency.

1.  **Idempotency / Re-run Safety:** The `dotnet nuget push` command **must** use the `--skip-duplicate` flag. This ensures that a re-run of the workflow for an existing tag will succeed with a "skipped" message instead of failing, which is crucial for operational stability.

2.  **Gating and Approval:** The publish job must be gated by a **GitHub Environment** with a "Required reviewers" protection rule. The nuget.org API key will be stored as an environment secret. This forces a manual approval step by a core maintainer before any package is pushed to the public, preventing accidental releases. This gate can be relaxed after the first few successful, stable releases.

3.  **Symbol Packages (`.snupkg`):** Publishing symbols is non-negotiable for a public library. They are essential for debugging.
    *   The build step must use `/p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg`.
    *   The `dotnet nuget push` command will automatically discover and push `.snupkg` files to NuGet's symbol server when it finds them alongside the corresponding `.nupkg`. No separate push step is required.

4.  **Pre-First-Push Guardrails (CRITICAL):** The following must be performed *before* the first `v*.*.*` tag is ever pushed to the main branch.
    *   **Dry Run:** Create a temporary workflow that pushes to a private feed like GitHub Packages. Use a throwaway version like `0.1.0-ghpr-dryrun`. This validates the entire packing and pushing mechanic without burning the `Dabbler.*` IDs on nuget.org.
    *   **ID Reservation:** Go to nuget.org **today** and create placeholder packages for the chosen IDs (`Dabbler.Api.Querying`, etc.) using a minimal `.nuspec` and version `0.0.1-reserved`. This is the only way to guarantee you own the namespace and protect against typosquatting. You can unlist them immediately, but the ID is now yours.
    *   **Automated Package Validation:** Integrate the `Microsoft.DotNet.PackageValidation` tool into the CI build. This tool runs checks for things like breaking API changes, correct assembly targeting, and other common packaging errors. It should run on every PR build to catch issues early.
    *   **Final Pre-Push Checklist:** Create a PR template or issue template for releases that includes a manual checklist: "I have verified the version number," "I have checked the release notes," "I have reviewed the packaged contents."

**Owner Under-weighted Consideration:**
The owner may be under-weighting the permanent, irreversible nature of a public package push. A bad package can be unlisted, but the version number (`1.2.3`) and the package ID (`Dabbler.Foo`) are claimed forever. The **ID Reservation** and **Dry Run** steps are the most important risk mitigation actions to take before flipping the switch. They are a small, one-time cost to prevent a permanent mistake.