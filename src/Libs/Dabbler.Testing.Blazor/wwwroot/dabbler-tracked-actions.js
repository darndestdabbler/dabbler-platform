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
