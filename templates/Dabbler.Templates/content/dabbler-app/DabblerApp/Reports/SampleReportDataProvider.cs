using System.Data;
using DabblerApp.Data;
using Dabbler.Reports.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DabblerApp.Reports;

/// <summary>
/// Returns SampleItem data as a DataTable for the sample report.
/// The query string is ignored — data comes from the in-memory EF Core context.
/// </summary>
public class SampleReportDataProvider(AppDbContext db) : IReportDataProvider
{
    public async Task<DataTable> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var items = await db.SampleItems
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var table = new DataTable("SampleItems");
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Value", typeof(decimal));
        table.Columns.Add("Description", typeof(string));

        foreach (var item in items)
        {
            table.Rows.Add(item.Name, item.Value, item.Description);
        }

        return table;
    }

    public Task<DataTable> ExecuteLookupAsync(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DataTable("Lookup"));
    }
}
