# Verification Round 1

### Issue 1 â†’ Invalid Target Framework Moniker

*   **Location:** `src/DabblerPdf/DabblerPdf.csproj`
*   **Fix:** The Target Framework Moniker (TFM) `net10.0` is invalid. .NET TFMs do not include a `.0` for major versions. Change it to the correct format, `net10`.

```xml
<PropertyGroup>
  <TargetFramework>net10</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
```

### Issue 2 â†’ Redundant test helper method

*   **Location:** `tests/DabblerPdf.Tests/Utilities/PdfDateTests.cs`
*   **Fix:** The private helper methods `FormatWithExplicitOffset` and `ExpectedFormatted` are identical. Remove `FormatWithExplicitOffset` and update its single call site to use `ExpectedFormatted` instead.

```csharp
// In PdfDateTests class

    [Theory]
    [InlineData(2, 30)]
    [InlineData(-5, 0)]
    [InlineData(5, 45)]
    [InlineData(0, 0)]
    public void Format_ApostropheSeparatedOffset(int offsetHours, int offsetMinutes)
    {
        // ...
        var offset = new TimeSpan(offsetHours, offsetMinutes, 0);
        var formatted = ExpectedFormatted( // Changed from FormatWithExplicitOffset
            new DateTime(2026, 4, 18, 12, 34, 56),
            offset);
        // ...
    }

    // REMOVE THIS METHOD
    // private static string FormatWithExplicitOffset(DateTime dt, TimeSpan offset)
    // { ... }

    private static string ExpectedFormatted(DateTime dt, TimeSpan offset)
    {
        // ... (keep this one)
    }
```
