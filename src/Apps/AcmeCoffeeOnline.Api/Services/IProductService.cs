using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Api.Services;

public interface IProductService
{
    Task<PagedResponse<ProductDto>> GetAllAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(int id, ProductDto dto, CancellationToken cancellationToken = default);
    Task<bool> ToggleAvailabilityAsync(int id, CancellationToken cancellationToken = default);
}
