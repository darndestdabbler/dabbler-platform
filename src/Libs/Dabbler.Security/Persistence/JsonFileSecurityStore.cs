using System.Text.Json;
using System.Text.Json.Serialization;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Persistence;

/// <summary>
/// Persists SecurityConfiguration as a single JSON file on disk.
/// Suitable for development, single-server deployments, and demos.
/// Uses a SemaphoreSlim to prevent concurrent write corruption.
/// </summary>
public sealed class JsonFileSecurityStore : ISecurityConfigurationStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonFileSecurityStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileSecurityStore"/> class.
    /// </summary>
    /// <param name="filePath">The absolute path to the JSON file for storing the security configuration.</param>
    /// <param name="logger">The logger for this store.</param>
    public JsonFileSecurityStore(string filePath, ILogger<JsonFileSecurityStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("Security configuration file not found at {Path}. Returning empty configuration.", _filePath);
            return new SecurityConfiguration();
        }

        await _lock.WaitAsync(ct);
        try
        {
            await using var stream = File.OpenRead(_filePath);
            var config = await JsonSerializer.DeserializeAsync<SecurityConfiguration>(stream, SerializerOptions, ct);
            return config ?? new SecurityConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load security configuration from {Path}.", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await _lock.WaitAsync(ct);
        try
        {
            var tempPath = _filePath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, configuration, SerializerOptions, ct);
            }
            File.Move(tempPath, _filePath, overwrite: true);
            _logger.LogInformation("Security configuration saved to {Path}.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save security configuration to {Path}.", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
