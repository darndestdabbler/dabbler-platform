```json
{
  "verdict": "ISSUES_FOUND",
  "issues": [
    {
      "severity": "Major",
      "description": "Playwright tests `BatchEntry_Save_IncrementsCounterAndClearsFormAsync` and `BatchEntry_TwoRecords_BothPersistedInDbAsync` both generate the email `batchentry-{RunId}-1@test.local` using the same static `RunId`. When both tests run in the same process (xUnit runs class methods sequentially by default), the second test to execute will attempt to create a customer with a duplicate email. If the API/DB enforces a unique email constraint, the second test will fail.",
      "location": "CustomerBatchEntryWorkflowTests.cs — static RunId + email patterns in tests 2 and 3",
      "suggestion": "Give each test method its own unique email prefix, e.g. use a per-method `Guid.NewGuid()` slug or use distinct numeric suffixes (`-s2-1` vs `-s3-1`) so no email is shared across test methods."
    },
    {
      "severity": "Minor",
      "description": "`HandleCancel` in `CustomerBatchEntryPage` fire-and-forgets `_form.ResetAsync()` via `_ = _form.ResetAsync()`. Any exception thrown by the async reset is silently swallowed, and the form reset is not guaranteed to complete before the next render cycle.",
      "location": "CustomerBatchEntryPage.razor — HandleCancel method",
      "suggestion": "Make `HandleCancel` async (`private async Task HandleCancel()`) and `await _form.ResetAsync()`. `EventCallback.InvokeAsync` will correctly await an `async Task` delegate."
    }
  ]
}
```