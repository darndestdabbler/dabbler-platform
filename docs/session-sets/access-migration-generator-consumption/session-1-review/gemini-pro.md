To the architecture review board:

This is my independent recommendation on the packaging and distribution of the `Dabbler.CrudSlice` and `Dabbler.SchemaApplicator` code generators.

### 1. Recommendation: Option A, with a Distant Runner-Up

My recommendation is **Option A: Publish as `dotnet tool`(s) on nuget.org**.

This is the only option that directly solves the consumer's acquisition friction while remaining perfectly consistent with the project's established distribution architecture. It leverages the existing precedent (`Dabbler.Security.DevIdp.Tool`), the single versioning scheme, and the public nuget.org feed without alteration. It provides the cleanest, most idiomatic acquisition mechanism for a .NET build-time tool.

**Runner-up:** Option B (Publish as ordinary library/CLI packages). This is a distant second. While it solves the "clone and build" problem, it violates the established architectural precedent of distinguishing build-time tools from runtime libraries. This would create two different ways for consumers to acquire tools, leading to confusion and architectural drift.

Option C is not a viable solution. It fails to address the core problem and punts the friction to hundreds of downstream consumers, which is contrary to the platform's goals.

### 2. Sub-decisions for Option A

*   **One tool or two?**
    Publish **one tool**: `Dabbler.CrudSlice`. `SchemaApplicator` is an implementation detail of `CrudSlice` and is bundled via its project reference. This decision minimizes the public surface area. A consumer needing to re-apply a schema can use `CrudSlice`'s `--skip-templates` flag. If a strong, independent use case for `SchemaApplicator` emerges later, we can publish it as a second tool. It is easier to add a tool than to retract one.
*   **Naming:**
    -   **PackageId:** `Dabbler.CrudSlice.Tool`
    -   **Tool Command Name:** `dabbler-crud-slice`
    This follows the exact precedent set by `Dabbler.Security.DevIdp.Tool` (`dabbler-devidp`), satisfying the cross-repo traceability constraint by mapping the package/command name directly to the `tools/Dabbler.CrudSlice` source project.
*   **Versioning:**
    The tool package will be versioned using the single, shared `DabblerVersion`, identical to all other runtime and template packages. A consumer pins a reproducible version via a local tool manifest (`dotnet-tools.json`) or by specifying the version flag during install: `dotnet tool install Dabbler.CrudSlice.Tool --version <MAJOR.MINOR.PATCH-preview.n>`. No new versioning scheme is needed.
*   **The `dotnet new` templates dependency:**
    `CrudSlice`'s dependency on the `Dabbler.Templates` package is an explicit pre-requisite. A clean-machine consumer (like a CI agent) must perform **two installation steps** in a deterministic order:
    1.  `dotnet new install Dabbler.Templates --version <pinned_version>`
    2.  `dotnet tool install Dabbler.CrudSlice.Tool --version <pinned_version>`
    The consumer playbook must enforce that the *exact same version* is used for both packages. The "clean-machine consumer-validation gate" for this feature must prove that a script executing these two commands, followed by an invocation of `dabbler-crud-slice`, succeeds.

### 3. Risks and Mitigations

1.  **Risk: Dependency Mismatch.** A consumer could install mismatched versions of the tool and the templates, causing `dotnet new` to fail or produce incorrect output.
    *   **Mitigation:** The single shared versioning scheme is the primary mitigation. The migration orchestrator's playbook ("Step 7") must be updated to pin *both* artifacts to the *same exact version string*. This should be a documented, non-negotiable requirement for tool consumers.
2.  **Risk: Increased Public Contract.** The tool's command-line arguments and behavior become a public, versioned contract. Changes are hard to reverse.
    *   **Mitigation:** Standard API governance. Keep the initial CLI surface minimal. Clearly document arguments and use the `-preview` suffix to signal that the interface is subject to change until a stable release.
3.  **Risk: Environment Brittleness.** The `dotnet new install` command modifies per-user state, which can be problematic on shared CI agents.
    *   **Mitigation:** This is inherent to `dotnet new`, not our tool. We will document this and advise consumers to use clean CI environments or to explicitly run `dotnet new uninstall Dabbler.Templates` as a post-build cleanup step.

### 4. Consistency with Locked Feed Architecture

**This choice is 100% consistent with the locked architecture.**

It does not strain it; it **reinforces** it. By choosing Option A, we are affirming the architectural decision to treat build-time tools as distinct artifacts from runtime libraries, using the exact mechanism (`PackAsTool=true`) and feed (nuget.org) already established and allowlisted for this purpose. The decision fits cleanly into the existing `pack.ps1` pipeline and versioning strategy.