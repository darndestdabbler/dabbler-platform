using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Models;
using Dabbler.Security.Authorization;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AcmeCoffeeDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly OrganizationScopeFilter _organizationScopeFilter;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;

    public CustomerService(
        AcmeCoffeeDbContext db,
        ICurrentUserService currentUser,
        OrganizationScopeFilter organizationScopeFilter,
        IBakedInRoleProvider bakedInRoleProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _organizationScopeFilter = organizationScopeFilter;
        _bakedInRoleProvider = bakedInRoleProvider;
    }

    public async Task<PagedResponse<CustomerDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        ClampPaging(request);
        var query = ApplyOrganizationScope(_db.Customers.AsNoTracking().AsQueryable());

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Email.Contains(term) ||
                (c.City != null && c.City.Contains(term)) ||
                (c.State != null && c.State.Contains(term)));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await ApplyOrganizationScope(_db.Customers.AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = dto.ModifiedBy
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, CustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await ApplyOrganizationScope(_db.Customers)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (customer is null)
            return null;

        customer.Email = dto.Email;
        customer.FirstName = dto.FirstName;
        customer.LastName = dto.LastName;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.State = dto.State;
        customer.ZipCode = dto.ZipCode;
        customer.IsActive = dto.IsActive;
        customer.ModifiedDate = DateTime.UtcNow;
        customer.ModifiedBy = dto.ModifiedBy;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<PagedResponse<CustomerDto>> SearchAsync(string searchTerm, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var searchRequest = new PagedRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending,
            SearchTerm = searchTerm
        };
        return await GetAllAsync(searchRequest, cancellationToken);
    }

    private static void ClampPaging(PagedRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }

    private static IQueryable<Customer> ApplySorting(IQueryable<Customer> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "firstname" => descending ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName),
            "lastname" => descending ? query.OrderByDescending(c => c.LastName) : query.OrderBy(c => c.LastName),
            "email" => descending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "city" => descending ? query.OrderByDescending(c => c.City) : query.OrderBy(c => c.City),
            "state" => descending ? query.OrderByDescending(c => c.State) : query.OrderBy(c => c.State),
            "createddate" => descending ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
            "modifieddate" => descending ? query.OrderByDescending(c => c.ModifiedDate) : query.OrderBy(c => c.ModifiedDate),
            _ => query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
        };
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Email = c.Email,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Phone = c.Phone,
        Address = c.Address,
        City = c.City,
        State = c.State,
        ZipCode = c.ZipCode,
        SocialSecurityNumber = c.SocialSecurityNumber,
        IsActive = c.IsActive,
        CreatedDate = c.CreatedDate,
        ModifiedDate = c.ModifiedDate,
        ModifiedBy = c.ModifiedBy
    };

    private IQueryable<Customer> ApplyOrganizationScope(IQueryable<Customer> query)
    {
        // Any baked-in role with CanSeeAllOrganizations=true bypasses the
        // per-organization scope (SystemAdmin, CrossOrgUserAdmin,
        // CrossOrgMonitor). Previously this gate compared against the
        // AcmeCoffee UserRole enum, which does not include CrossOrgUserAdmin
        // — so that role silently fell through to the deny path below.
        if (_organizationScopeFilter.CanBypassOrganizationScope(
                _currentUser.RoleNames, _bakedInRoleProvider))
        {
            return query;
        }

        // Customer-role users see only their own customer record, identified
        // by the CustomerId claim that the InternalUserMiddleware / session
        // context attaches when a login is linked to a Customer row.
        if (_currentUser.CustomerId.HasValue
            && _currentUser.Role == AcmeCoffeeOnline.Shared.Enums.UserRole.Customer)
        {
            return query.Where(customer => customer.Id == _currentUser.CustomerId.Value);
        }

        // Internal user-type roles (BackofficeUser, BackofficeUserAdmin,
        // WarehouseUser, …) see every customer until Set C adds
        // Customer.OrganizationId and enables per-org scoping in EF.
        // OrganizationScopeFilter.DeferredReason documents this deferral.
        return query;
    }
}
