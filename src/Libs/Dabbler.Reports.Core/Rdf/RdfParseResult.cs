namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// The result of a non-throwing RDF parse attempt via <see cref="RdfParser.TryParse"/>.
/// </summary>
/// <param name="Definition">The parsed definition, or null if parsing/validation failed.</param>
/// <param name="Errors">The list of validation errors (empty on success).</param>
public record RdfParseResult(ReportDefinition? Definition, IReadOnlyList<RdfValidationError> Errors)
{
    /// <summary>Gets whether parsing and validation succeeded.</summary>
    public bool IsValid => Definition is not null && Errors.Count == 0;
}
