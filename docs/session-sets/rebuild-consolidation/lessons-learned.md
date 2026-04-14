# Lessons Learned

## Escaped Issues And Prevention Ownership

| Escaped issue | Root cause summary | Primary prevention owner | What should prevent it next time |
| --- | --- | --- | --- |
| Unstyled shell/sidebar | Required Dabbler CSS assets were never linked from the app host | Scaffolding | App template must include the supported Dabbler asset entrypoint by default |
| Auth pages did not render | The main layout omitted `@Body`, so routed content was discarded | Scaffolding | Layout template plus smoke tests must verify routed content renders |
| Magic-link tests failed in dev | Tests assumed MailHog, but dev email behavior was log-based | Library | Dev/test auth delivery modes must be explicit, correctly named, and documented |
| Non-auth browser tests were brittle | There was no deterministic session-establishment path for non-auth tests | Library | Provide a safe Development/Test-only seeded-session mechanism |
| Permission tests drifted from real policy | Test expectations diverged from `security-config.json` | Tests | Authorization checks must be derived from or validated against the authoritative policy config |
| Admin navigation required app-specific glue | Built-in security navigation assumptions did not align with app role naming | Library | Security-aware shell/nav integration needs clearer extension points or built-in adaptability |
| Broader E2E coverage gave false confidence | Many tests were page/smoke oriented rather than backend-verified behavior checks | Tests | Meaningful E2E standards must require backend truth for important mutating flows |

## Prioritized Lessons

### 1. Templates must encode the known-good baseline

If an app can be created without the required shell assets, the right layouts, and the right auth/test hooks, then we are relying on luck instead of scaffolding.

### 2. Development modes must behave exactly as they are named

If a mode is called MailHog but actually logs to the console, both humans and tests will make the wrong assumptions. Dev/test modes must be explicit, predictable, and documented.

### 3. Security policy must have one authoritative home

`security-config.json` is currently that authority. Tests, navigation, and app behavior all need to stay aligned with it. Rebuild work must not silently reinterpret policy intent.

### 4. Deterministic test infrastructure is not optional

Non-auth tests should not depend on UI login or external delivery systems. Auth-specific tests should exercise the real auth flow. Those are different needs and should use different helpers.

### 5. Page load is not proof of functionality

Meaningful E2E must verify outcomes, not just visibility. For data-changing workflows, the rebuilt suite needs backend confirmation, not just successful clicks.

### 6. App/library seams need stronger defaults

The current app had to compensate for missing library-level protections and awkward integration assumptions. The rebuild should reduce app-specific glue by making the right path easier and the wrong path harder.

## Lessons To Carry Into The Rebuild

- build the templates and test harness before rebuilding feature slices
- encode auth/test modes as explicit platform behavior, not undocumented conventions
- validate policy configuration in CI
- separate anonymous auth layouts from authenticated shell layouts
- preserve proven business behavior, but rebuild fragile composition seams from scratch
