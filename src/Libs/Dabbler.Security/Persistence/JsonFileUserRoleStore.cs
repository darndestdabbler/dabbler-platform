using System.Text.Json;
using Dabbler.Security.Services;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Persistence;

/// <summary>
/// Persists user-role assignments as a JSON dictionary { userId → [roleName, ...] }.
/// The file path is derived from the security-config file path by convention,
/// or supplied explicitly via <c>Dabbler.Security.DependencyInjection.ServiceCollectionExtensions.UseJsonFileStore</c>.
///
/// Uses a <see cref="SemaphoreSlim"/> to prevent concurrent write corruption.
/// Writes are atomic: the file is written to a .tmp path then moved to the real path.
/// </summary>
public sealed class JsonFileUserRoleStore : IUserRoleStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonFileUserRoleStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileUserRoleStore"/> class.
    /// </summary>
    /// <param name="filePath">The absolute path to the JSON file for storing user roles.</param>
    /// <param name="logger">The logger for this store.</param>
    public JsonFileUserRoleStore(string filePath, ILogger<JsonFileUserRoleStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.TryGetValue(userId, out var roles) ? roles : [];
    }

    /// <inheritdoc />
    public async Task SetRolesAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadLockedAsync(ct);
            data[userId] = [.. roles];
            await PersistAsync(data, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadLockedAsync(ct);
            if (!data.TryGetValue(userId, out var roles))
            {
                roles = [];
                data[userId] = roles;
            }

            if (!roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                roles.Add(roleName);

            await PersistAsync(data, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadLockedAsync(ct);
            if (data.TryGetValue(userId, out var roles))
            {
                roles.RemoveAll(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
                if (roles.Count == 0)
                    data.Remove(userId);
            }

            await PersistAsync(data, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllAsync(CancellationToken ct = default)
    {
        var data = await LoadAsync(ct);
        return data.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    // --- private helpers ---

    /// <summary>Loads the file without acquiring the lock (caller must hold it).</summary>
    private async Task<Dictionary<string, List<string>>> LoadLockedAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var raw = await JsonSerializer.DeserializeAsync<Dictionary<string, List<string>>>(stream, SerializerOptions, ct);
            return raw ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user-role assignments from {Path}.", _filePath);
            throw;
        }
    }

    /// <summary>Loads the file, acquiring the lock internally. For read-only operations.</summary>
    private async Task<Dictionary<string, List<string>>> LoadAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await LoadLockedAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task PersistAsync(Dictionary<string, List<string>> data, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = _filePath + ".tmp";
        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, ct);
            }

            File.Move(tempPath, _filePath, overwrite: true);
            _logger.LogInformation("User-role assignments saved to {Path}.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user-role assignments to {Path}.", _filePath);
            throw;
        }
    }
}
