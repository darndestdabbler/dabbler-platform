using Dabbler.Reports.Core.Layout;

namespace Dabbler.Reports.Core.Export;

/// <summary>
/// Abstraction for exporting composed report pages to an output format (PDF, etc.).
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// Exports the composed pages to the output stream.
    /// </summary>
    /// <param name="pages">The composed report pages.</param>
    /// <param name="outputStream">The output stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportAsync(
        IReadOnlyList<PageLayout> pages,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}
