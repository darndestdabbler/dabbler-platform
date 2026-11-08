using DabblerApp.Web.Data;
using Dabbler.Components.Data;
using Microsoft.EntityFrameworkCore;

namespace DabblerApp.Web.Services;

public class SampleItemProvider(AppDbContext db) : IGridDataProvider<SampleItem>
{
    public async Task<GridDataResponse<SampleItem>> FetchAsync(
        GridDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = db.SampleItems.AsQueryable();

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.Name)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new GridDataResponse<SampleItem> { Items = items, TotalCount = total };
    }
}
