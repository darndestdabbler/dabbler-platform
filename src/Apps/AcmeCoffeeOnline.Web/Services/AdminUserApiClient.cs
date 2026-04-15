using System.Net.Http.Json;
using AcmeCoffeeOnline.Shared.DTOs;

namespace AcmeCoffeeOnline.Web.Services;

public sealed class AdminUserApiClient
{
    private readonly HttpClient _httpClient;

    public AdminUserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AdminUserScopeDto?> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<AdminUserScopeDto>(
            "api/admin/users/scope",
            cancellationToken);
    }

    public async Task<AdminUserScopeDto?> GrantScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"api/admin/users/memberships/{organizationMembershipId}/grant-user-admin",
            content: null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminUserScopeDto>(cancellationToken);
    }

    public async Task<AdminUserScopeDto?> RevokeScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/admin/users/memberships/{organizationMembershipId}/grant-user-admin",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminUserScopeDto>(cancellationToken);
    }
}
