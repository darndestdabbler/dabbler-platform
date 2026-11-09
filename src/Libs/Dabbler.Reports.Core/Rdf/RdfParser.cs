using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Parses and validates RDF JSON files into <see cref="ReportDefinition"/> instances.
/// </summary>
public class RdfParser
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Deserializes an RDF JSON string into a <see cref="ReportDefinition"/>.
    /// </summary>
    /// <param name="json">The RDF JSON content to parse.</param>
    /// <returns>A validated <see cref="ReportDefinition"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="RdfParseException">Thrown when the JSON is malformed or fails validation.</exception>
    public ReportDefinition Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        ReportDefinition definition;
        try
        {
            definition = JsonSerializer.Deserialize<ReportDefinition>(json, s_options)
                ?? throw new RdfParseException("Deserialization returned null. The JSON may be empty or contain only 'null'.");
        }
        catch (JsonException ex)
        {
            throw new RdfParseException($"Invalid JSON: {ex.Message}", ex);
        }

        var errors = RdfValidator.Validate(definition);
        if (errors.Count > 0)
        {
            throw new RdfValidationException(errors);
        }

        return definition;
    }

    /// <summary>
    /// Attempts to deserialize an RDF JSON string into a <see cref="ReportDefinition"/>,
    /// returning a result that includes any validation errors without throwing.
    /// </summary>
    /// <param name="json">The RDF JSON content to parse.</param>
    /// <returns>An <see cref="RdfParseResult"/> containing the definition and/or errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public RdfParseResult TryParse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        ReportDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<ReportDefinition>(json, s_options);
            if (definition is null)
            {
                return new RdfParseResult(null, [new RdfValidationError("Root", "Deserialization returned null.")]);
            }
        }
        catch (JsonException ex)
        {
            return new RdfParseResult(null, [new RdfValidationError("Json", ex.Message)]);
        }

        var errors = RdfValidator.Validate(definition);
        return new RdfParseResult(errors.Count == 0 ? definition : null, errors);
    }
}
