using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

public class ProductService : IProductService
{
    private readonly AcmeCoffeeDbContext _db;

    public ProductService(AcmeCoffeeDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<ProductDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        ClampPaging(request);
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            RoastLevel = dto.RoastLevel,
            CaffeineContent = dto.CaffeineContent,
            GrindType = dto.GrindType,
            UnitPrice = dto.UnitPrice,
            IsAvailable = dto.IsAvailable,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product is null)
            return null;

        product.Name = dto.Name;
        product.RoastLevel = dto.RoastLevel;
        product.CaffeineContent = dto.CaffeineContent;
        product.GrindType = dto.GrindType;
        product.UnitPrice = dto.UnitPrice;
        product.IsAvailable = dto.IsAvailable;
        product.Description = dto.Description;
        product.ImageUrl = dto.ImageUrl;
        product.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    public async Task<bool> ToggleAvailabilityAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product is null)
            return false;

        product.IsAvailable = !product.IsAvailable;
        product.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ClampPaging(PagedRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "unitprice" or "price" => descending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            "roastlevel" => descending ? query.OrderByDescending(p => p.RoastLevel) : query.OrderBy(p => p.RoastLevel),
            "createddate" => descending ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
            "modifieddate" => descending ? query.OrderByDescending(p => p.ModifiedDate) : query.OrderBy(p => p.ModifiedDate),
            _ => query.OrderBy(p => p.Name)
        };
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        RoastLevel = p.RoastLevel,
        CaffeineContent = p.CaffeineContent,
        GrindType = p.GrindType,
        UnitPrice = p.UnitPrice,
        IsAvailable = p.IsAvailable,
        Description = p.Description,
        ImageUrl = p.ImageUrl,
        CreatedDate = p.CreatedDate,
        ModifiedDate = p.ModifiedDate
    };
}
