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
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AdminUserScopeDto>(cancellationToken);
    }

    public async Task<AdminUserScopeDto?> RevokeScopedUserAdminAsync(int organizationMembershipId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"api/admin/users/memberships/{organizationMembershipId}/grant-user-admin",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AdminUserScopeDto>(cancellationToken);
    }

    public async Task<BackofficeUserCreationResponseDto?> CreateBackofficeUserAsync(
        CreateBackofficeUserDto dto,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/admin/users",
            dto,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BackofficeUserCreationResponseDto>(cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                throw new InvalidOperationException(error.Error);
            }
        }
        catch (NotSupportedException)
        {
        }
        catch (System.Text.Json.JsonException)
        {
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed class ErrorResponse
    {
        public string? Error { get; set; }
    }
}
