namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Thrown when an RDF JSON string cannot be parsed into a <see cref="ReportDefinition"/>.
/// </summary>
public class RdfParseException : Exception
{
    /// <summary>Initializes a new instance of <see cref="RdfParseException"/>.</summary>
    public RdfParseException(string message) : base(message) { }

    /// <summary>Initializes a new instance of <see cref="RdfParseException"/> with an inner exception.</summary>
    public RdfParseException(string message, Exception innerException) : base(message, innerException) { }
}
