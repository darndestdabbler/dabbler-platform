namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Thrown when an RDF document deserializes successfully but fails semantic validation.
/// </summary>
public class RdfValidationException : Exception
{
    /// <summary>Gets the validation errors.</summary>
    public IReadOnlyList<RdfValidationError> Errors { get; }

    /// <summary>Initializes a new instance of <see cref="RdfValidationException"/>.</summary>
    public RdfValidationException(IReadOnlyList<RdfValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    private static string FormatMessage(IReadOnlyList<RdfValidationError> errors)
    {
        return errors.Count == 1
            ? $"RDF validation failed: {errors[0]}"
            : $"RDF validation failed with {errors.Count} errors:{Environment.NewLine}" +
              string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
    }
}
