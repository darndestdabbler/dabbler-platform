window.dabbler = window.dabbler || {};

const html = () => document.documentElement;
const inFlight = () => JSON.parse(html().dataset.actionsInFlight || "[]");

/// <summary>
/// Signals the start of an action, adding its ID to the in-flight list.
/// </summary>
dabbler.beginAction = id => {
  console.log('[dabbler] beginAction', id, performance.now().toFixed(1));
  html().dataset.actionsInFlight = JSON.stringify([...inFlight(), id]);
};

/// <summary>
/// Signals the end of an action, removing its ID from the in-flight list.
/// </summary>
dabbler.endAction = id => {
  console.log('[dabbler] endAction', id, performance.now().toFixed(1));
  html().dataset.actionsInFlight = JSON.stringify(inFlight().filter(x => x !== id));
  html().dataset.lastAction = id;
};

/// <summary>
/// Signals test readiness. Called by TestReadinessProbe on first render.
/// </summary>
dabbler.setReady = () => {
  console.log('[dabbler] setReady', performance.now().toFixed(1));
  html().dataset.blazorReady = "true";
  html().dataset.testMode = "true";
};

/// <summary>
/// Auto-bootstrap: set data-blazor-ready as soon as the Blazor interactive
/// runtime has attached a component to the DOM.
///
/// Why this exists: TestReadinessProbe.OnAfterRenderAsync only runs from the
/// layout that hosts it (originally MainLayout). Pages on AuthLayout (e.g.
/// /login) and other auxiliary layouts don't carry the probe, so
/// data-blazor-ready never gets set on those routes — and a DSL-compiled test
/// that races straight from GotoAsync to FillAsync hits the form before
/// SignalR has registered the @bind-Value subscriber, leaving the bound
/// field empty and downstream form-validation gates closed.
///
/// What it does: a MutationObserver scans every added element for an
/// attribute whose name starts with `_bl_` — the per-component identifier
/// Blazor injects on each element once the interactive circuit has rendered
/// it. (CSS attribute selectors can't match "attribute name starts with",
/// so we iterate the element's `attributes` collection in JS.) When seen,
/// the observer disconnects and calls setReady(). This works regardless of
/// which layout hosts the page.
///
/// The setReady call is idempotent — if a layout-hosted TestReadinessProbe
/// also fires later, it's a harmless re-set.
/// </summary>
(function autoBootstrapReadiness() {
  if (document.documentElement.dataset.blazorReady === "true") return;

  const hasBlazorMarker = (el) => {
    if (!el || !el.attributes) return false;
    for (let i = 0; i < el.attributes.length; i++) {
      if (el.attributes[i].name.startsWith("_bl_")) return true;
    }
    return false;
  };

  const treeHasMarker = (root) => {
    if (hasBlazorMarker(root)) return true;
    const all = root.querySelectorAll ? root.querySelectorAll("*") : [];
    for (let i = 0; i < all.length; i++) {
      if (hasBlazorMarker(all[i])) return true;
    }
    return false;
  };

  if (treeHasMarker(document.documentElement)) {
    dabbler.setReady();
    return;
  }

  const observer = new MutationObserver((mutations) => {
    for (const m of mutations) {
      for (const node of m.addedNodes) {
        if (node.nodeType === 1 /* ELEMENT_NODE */ && treeHasMarker(node)) {
          observer.disconnect();
          dabbler.setReady();
          return;
        }
      }
    }
  });

  const start = () => observer.observe(document.documentElement, {
    childList: true,
    subtree: true
  });

  if (document.body) {
    start();
  } else {
    document.addEventListener("DOMContentLoaded", start, { once: true });
  }
})();
