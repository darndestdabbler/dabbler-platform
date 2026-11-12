namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Represents a single validation error found in an RDF document.
/// </summary>
/// <param name="Path">The dot-path to the invalid element (e.g., "Bands[2].Elements[0].StyleName").</param>
/// <param name="Message">A human-readable description of the error.</param>
public record RdfValidationError(string Path, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Path}] {Message}";
}
