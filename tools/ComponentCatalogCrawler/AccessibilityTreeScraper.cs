using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace ComponentCatalogCrawler;

/// <summary>
/// Extracts the user-visible interactive elements from a rendered page and
/// emits <see cref="CatalogElement"/> records that match the W0
/// <c>component-catalog.json</c> schema.
/// </summary>
/// <remarks>
/// We extract via an in-page JS evaluation rather than Playwright's
/// <c>IAccessibility.SnapshotAsync</c> because the latter is marked
/// obsolete and additionally trips the Playwright stdio transport's
/// <c>System.Text.Json</c> default 64-deep parse limit on richly nested
/// MudBlazor screens (e.g. the security dashboard tabs). Walking the DOM
/// inside the page returns a flat array — no nested-tree depth concerns —
/// and gives us the same ARIA role, aria-label, innerText, and test-id
/// the DSL validator pairs against.
/// </remarks>
public sealed class AccessibilityTreeScraper
{
    private const string ScrapeScript = """
        () => {
          const SELECTOR = [
            'a[href]', 'button', 'input', 'select', 'textarea',
            '[role="button"]', '[role="link"]', '[role="tab"]',
            '[role="menuitem"]', '[role="checkbox"]', '[role="switch"]',
            '[role="combobox"]', '[role="listbox"]', '[role="textbox"]',
            '[role="searchbox"]', '[role="spinbutton"]',
            '[role="row"]', '[role="grid"]', '[role="treegrid"]',
            '[role="columnheader"]', '[role="rowheader"]',
            '[role="alert"]', '[role="status"]',
            '[role="dialog"]', '[role="alertdialog"]',
            '[role="heading"]',
            '[role="navigation"]','[role="main"]','[role="banner"]',
            '[role="contentinfo"]','[role="complementary"]',
            '[role="region"]','[role="form"]','[role="search"]',
            '[role="tabpanel"]',
            'h1','h2','h3','h4','h5','h6',
            'th',
            '[data-testid]'
          ].join(',');
          const isVisible = (el) => {
            const r = el.getBoundingClientRect();
            const s = window.getComputedStyle(el);
            return r.width > 0 && r.height > 0
              && s.visibility !== 'hidden' && s.display !== 'none';
          };
          const computedRole = (el) => {
            const explicit = el.getAttribute('role');
            if (explicit) return explicit;
            const tag = el.tagName.toLowerCase();
            if (tag === 'a' && el.hasAttribute('href')) return 'link';
            if (tag === 'button') return 'button';
            if (tag === 'select') return 'combobox';
            if (tag === 'textarea') return 'textbox';
            if (tag === 'input') {
              const t = (el.getAttribute('type') || 'text').toLowerCase();
              if (t === 'checkbox') return 'checkbox';
              if (t === 'radio') return 'radio';
              if (t === 'submit' || t === 'button' || t === 'reset') return 'button';
              if (t === 'search') return 'searchbox';
              if (t === 'number') return 'spinbutton';
              return 'textbox';
            }
            if (/^h[1-6]$/.test(tag)) return 'heading';
            if (tag === 'th') return 'columnheader';
            return tag;
          };
          // Accessible-name approximation: aria-label > aria-labelledby text > label[for] > innerText.
          const computeName = (el) => {
            const ariaLabel = el.getAttribute('aria-label');
            if (ariaLabel) return ariaLabel.trim();
            const labelledBy = el.getAttribute('aria-labelledby');
            if (labelledBy) {
              const ids = labelledBy.split(/\s+/);
              const txt = ids
                .map(id => document.getElementById(id))
                .filter(Boolean)
                .map(n => (n.innerText || n.textContent || '').trim())
                .filter(Boolean)
                .join(' ');
              if (txt) return txt;
            }
            // For form fields, prefer associated <label>.
            const id = el.id;
            if (id) {
              const lbl = document.querySelector(`label[for="${CSS.escape(id)}"]`);
              if (lbl) {
                const text = (lbl.innerText || lbl.textContent || '').trim();
                if (text) return text;
              }
            }
            // Fallback: trimmed innerText (single-line preferred).
            const raw = (el.innerText || el.textContent || '').trim();
            return raw.replace(/\s+/g, ' ');
          };
          const closestScope = (el) => {
            const dlg = el.closest('[role="dialog"], [role="alertdialog"]');
            if (dlg) {
              const aria = dlg.getAttribute('aria-label');
              if (aria) return aria.trim();
              const labelId = dlg.getAttribute('aria-labelledby');
              if (labelId) {
                const lbl = document.getElementById(labelId);
                if (lbl) return (lbl.innerText || lbl.textContent || '').trim();
              }
              const heading = dlg.querySelector('h1,h2,h3,h4,h5,h6');
              if (heading) return (heading.innerText || heading.textContent || '').trim();
            }
            return null;
          };
          const out = [];
          const seen = new WeakSet();
          for (const el of document.querySelectorAll(SELECTOR)) {
            if (seen.has(el)) continue;
            seen.add(el);
            if (!isVisible(el)) continue;
            const role = computedRole(el);
            const name = computeName(el);
            const ariaLabel = el.getAttribute('aria-label');
            // Skip if no name AND no aria-label — nothing the DSL can locate.
            if (!name && !ariaLabel) continue;
            out.push({
              role,
              name: (name || '').slice(0, 200),
              ariaLabel,
              testId: el.getAttribute('data-testid'),
              htmlTag: el.tagName.toLowerCase(),
              scope: closestScope(el)
            });
          }
          return out;
        }
        """;

    /// <summary>
    /// Maps DOM/ARIA roles to the closed enum of <c>elementRole</c>
    /// values defined by the W0 component-catalog schema. Roles not in
    /// this table fall through to the <see cref="ElementCatchallRoles"/>
    /// set; if matched there, the entry is recorded as <c>Element</c>.
    /// </summary>
    private static readonly Dictionary<string, string> AriaRoleToElementRole = new(StringComparer.OrdinalIgnoreCase)
    {
        ["button"] = "Button",
        ["link"] = "Link",
        ["tab"] = "Tab",
        ["menuitem"] = "MenuItem",
        ["menuitemcheckbox"] = "MenuItem",
        ["menuitemradio"] = "MenuItem",
        ["textbox"] = "Input",
        ["spinbutton"] = "Input",
        ["searchbox"] = "Input",
        ["checkbox"] = "Checkbox",
        ["switch"] = "Switch",
        ["combobox"] = "Dropdown",
        ["listbox"] = "Dropdown",
        ["row"] = "Row",
        ["grid"] = "Grid",
        ["treegrid"] = "Grid",
        ["table"] = "Grid",
        ["columnheader"] = "Column",
        ["rowheader"] = "Column",
        ["alert"] = "Snackbar",
        ["status"] = "Snackbar",
        ["heading"] = "Text",
    };

    /// <summary>
    /// Named landmark / structural ARIA roles that carry an
    /// <c>aria-label</c> or accessible name worth keeping in the catalog
    /// even though they are not in the primary interactive enum. The
    /// W0 schema's <c>Element</c> catchall covers this case so the DSL
    /// can still locate them by label.
    /// </summary>
    private static readonly HashSet<string> ElementCatchallRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "navigation", "main", "banner", "contentinfo", "complementary",
        "region", "form", "search", "dialog", "alertdialog", "tabpanel",
    };

    public async Task<IReadOnlyList<CatalogElement>> ScrapeAsync(IPage page, string routePath, string role, bool isWatermarked = false)
    {
        // Playwright .NET's EvaluateAsync<T> serializer-roundtrips through
        // System.Text.Json with options that don't always cooperate with
        // record types whose properties are kebab/camel-cased. Returning
        // a JsonElement avoids the serializer mismatch entirely.
        var raw = await page.EvaluateAsync<JsonElement>(ScrapeScript);
        if (raw.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<CatalogElement>();
        }

        var elements = new List<CatalogElement>(raw.GetArrayLength());
        foreach (var r in raw.EnumerateArray())
        {
            var roleStr = GetStringOrNull(r, "role") ?? string.Empty;
            string elementRole;
            if (AriaRoleToElementRole.TryGetValue(roleStr, out var mapped))
            {
                elementRole = mapped;
            }
            else if (ElementCatchallRoles.Contains(roleStr))
            {
                elementRole = "Element";
            }
            else
            {
                continue;
            }

            var name = (GetStringOrNull(r, "name") ?? string.Empty).Trim();
            var ariaLabel = GetStringOrNull(r, "ariaLabel");
            var testId = GetStringOrNull(r, "testId");
            var htmlTag = GetStringOrNull(r, "htmlTag");
            var scope = GetStringOrNull(r, "scope");

            var hasName = !string.IsNullOrEmpty(name);
            var hasAria = !string.IsNullOrEmpty(ariaLabel);

            // IconButton: a button whose visible name came from aria-label
            // (i.e., the button has an explicit aria-label) is recorded as
            // IconButton — that is exactly what the DSL `CLICK ICON BUTTON
            // with aria-label` verb is designed to address.
            if (string.Equals(elementRole, "Button", StringComparison.Ordinal)
                && hasAria
                && (!hasName || string.Equals(name, ariaLabel, StringComparison.Ordinal)))
            {
                elementRole = "IconButton";
                name = ariaLabel!.Trim();
            }

            if (!hasName && !hasAria)
            {
                continue;
            }

            elements.Add(new CatalogElement(
                RoutePath: routePath,
                Role: role,
                ElementRole: elementRole,
                UserVisibleLabel: name,
                AriaLabel: hasAria ? ariaLabel : null,
                TestId: string.IsNullOrWhiteSpace(testId) ? null : testId,
                HtmlTag: string.IsNullOrWhiteSpace(htmlTag) ? null : htmlTag,
                Scope: string.IsNullOrWhiteSpace(scope) ? null : scope,
                IsWatermarked: isWatermarked));
        }

        // Deduplicate by (routePath, role, elementRole, userVisibleLabel, scope).
        return elements
            .GroupBy(e => (e.RoutePath, e.Role, e.ElementRole, e.UserVisibleLabel, e.Scope))
            .Select(g => g.First())
            .ToList();
    }

    private static string? GetStringOrNull(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
}
