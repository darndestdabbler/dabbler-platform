namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// The type of band within a report layout, following the band-based
/// layout model inspired by JasperReports.
/// </summary>
public enum BandType
{
    /// <summary>Report header band, rendered once at the beginning of the report.</summary>
    ReportHeader,

    /// <summary>Page header band, rendered at the top of every page.</summary>
    PageHeader,

    /// <summary>Group header band, rendered when a group value changes.</summary>
    GroupHeader,

    /// <summary>Detail band, rendered once per data row.</summary>
    Detail,

    /// <summary>Group footer band, rendered after all rows in a group.</summary>
    GroupFooter,

    /// <summary>Page footer band, rendered at the bottom of every page.</summary>
    PageFooter,

    /// <summary>Report footer band, rendered once after all data.</summary>
    ReportFooter,

    /// <summary>No-data band, rendered instead of the normal band sequence when the dataset is empty.</summary>
    NoData
}
