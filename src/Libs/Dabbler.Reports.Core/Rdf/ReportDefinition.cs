namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Root model representing a complete Report Definition File (RDF).
/// Deserialized from JSON by <see cref="RdfParser"/>.
/// </summary>
/// <example>
/// <code>
/// var parseResult = RdfParser.Parse(rdfJson);
/// ReportDefinition report = parseResult.Report;
///
/// // Access report structure
/// Console.WriteLine($"Report: {report.Metadata.Name}");
/// Console.WriteLine($"Bands: {report.Bands.Count}");
/// Console.WriteLine($"Parameters: {report.Parameters.Count}");
/// </code>
/// </example>
public class ReportDefinition
{
    /// <summary>Gets or sets the report metadata (title, page size, margins).</summary>
    public ReportMetadata Metadata { get; set; } = new();

    /// <summary>Gets or sets the data source definitions. The first entry is the primary data source.</summary>
    public List<DataSourceDefinition> DataSources { get; set; } = [];

    /// <summary>Gets or sets the parameter declarations for this report.</summary>
    public List<ParameterDefinition> Parameters { get; set; } = [];

    /// <summary>Gets or sets the field definitions mapping query columns to report fields.</summary>
    public List<FieldDefinition> Fields { get; set; } = [];

    /// <summary>Gets or sets the sort definitions for report data ordering.</summary>
    public List<SortDefinition> Sorting { get; set; } = [];

    /// <summary>Gets or sets the group definitions for data grouping.</summary>
    public List<GroupDefinition> Grouping { get; set; } = [];

    /// <summary>Gets or sets the band definitions that compose the report layout.</summary>
    public List<BandDefinition> Bands { get; set; } = [];

    /// <summary>Gets or sets the named style definitions.</summary>
    public List<StyleDefinition> Styles { get; set; } = [];

    /// <summary>Gets or sets the named expression definitions for calculated fields.</summary>
    public List<ExpressionDefinition> Expressions { get; set; } = [];
}
