using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Api.Services;

public interface ICustomerService
{
    Task<PagedResponse<CustomerDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerDto dto, CancellationToken cancellationToken = default);
    Task<CustomerDto?> UpdateAsync(int id, CustomerDto dto, CancellationToken cancellationToken = default);
    Task<PagedResponse<CustomerDto>> SearchAsync(string searchTerm, PagedRequest request, CancellationToken cancellationToken = default);
}
