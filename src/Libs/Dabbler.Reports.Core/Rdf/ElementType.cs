namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// The type of element within a report band.
/// </summary>
public enum ElementType
{
    /// <summary>Dynamic text field bound to a data field or expression.</summary>
    TextField,

    /// <summary>Static text label.</summary>
    StaticText,

    /// <summary>Horizontal or vertical line.</summary>
    Line,

    /// <summary>Rectangle (border, background fill).</summary>
    Rectangle,

    /// <summary>Image (embedded or referenced).</summary>
    Image,

    /// <summary>Embedded subreport rendered inline.</summary>
    Subreport
}
