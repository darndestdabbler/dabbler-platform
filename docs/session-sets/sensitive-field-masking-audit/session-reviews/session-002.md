# Verification Round 1

```json
{
  "verdict": "VERIFIED",
  "issues": [
    {
      "severity": "Minor",
      "title": "ClearSwitch audit row records pre-revert role, inconsistent with entity documentation",
      "description": "The entity documentation on `RoleName` states: \"For RoleSwitch events this is the role the user switched to.\" On `ClearSwitch`, the decorator captures and records the role the user was operating under *before* the clear (the switched-from role), not the base role they are reverting *to*. The integration test confirms this (`rows[1].RoleName.Should().Be(\"CrossOrgMonitor\")` with Details=\"revert\"). While the `Details` field disambiguates \"switch\" vs \"revert\" for a reviewer, the RoleName semantics diverge from the documented contract for the revert sub-case.",
      "file": "src/Libs/Dabbler.Security/Services/AuditingRoleSwitcher.cs",
      "suggested_fix": "Either update the entity XML doc on `RoleName` to clarify it means \"the role the user was operating under at event time\" (covering both cases), or resolve the actual principal in `ClearSwitch` to record the base role as the destination. The former is simpler and matches the implementation's practical value."
    },
    {
      "severity": "Minor",
      "title": "ClearSwitch probes inner switcher with an empty ClaimsPrincipal",
      "description": "`AuditingRoleSwitcher.ClearSwitch` calls `_inner.EffectiveRoles(new ClaimsPrincipal())` to discover the pre-clear role. This relies on `SessionRoleSwitcher` returning the switched role regardless of the principal argument when a switch is active. If a future `IRoleSwitcher` implementation inspects the principal's claims to determine effective roles (e.g., union of natural + switched), the empty principal would yield an incorrect or empty result. The coupling is implicit rather than contractual.",
      "file": "src/Libs/Dabbler.Security/Services/AuditingRoleSwitcher.cs",
      "suggested_fix": "Add a `string? CurrentSwitchedRole { get; }` property to `IRoleSwitcher` (or a dedicated read-only accessor) so the decorator can query the active switched role without fabricating a dummy principal. Alternatively, document this assumption with a comment referencing `SessionRoleSwitcher`'s contract."
    },
    {
      "severity": "Nitpick",
      "title": "AppSensitiveAccessLog lives in Models/ while all other EF entities live in Entities/",
      "description": "Disclosed as deviation #8. The entity is EF-mapped with a `DbSet`, temporal configuration, and a migration — functionally identical to `AppUser`, `AppRole`, etc., which all reside in `Entities/`. Placing it in `Models/` breaks the existing project convention and will likely confuse contributors looking for the entity class. The spec asked for this location, but the spec also asked for a separate `Configuration/` EF config file which was (correctly) overridden by codebase convention in deviation #1.",
      "file": "src/Libs/Dabbler.Security/Models/AppSensitiveAccessLog.cs",
      "suggested_fix": "Move `AppSensitiveAccessLog.cs` and `SensitiveAccessEventKind` to `src/Libs/Dabbler.Security/Entities/` to match codebase convention, updating the namespace accordingly. This can be done in a later session as a follow-up."
    }
  ]
}
```
