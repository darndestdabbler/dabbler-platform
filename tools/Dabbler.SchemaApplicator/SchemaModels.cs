using System.Text.Json.Serialization;

namespace Dabbler.SchemaApplicator;

/// <summary>
/// Deserialized shape of a <c>*.dabbler-entity.json</c> schema file.
/// Mirrors what <c>Apply-{Entity}Schema.ps1</c> reads via
/// <c>ConvertFrom-Json</c>.
/// </summary>
public sealed class EntitySchema
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonPropertyName("primaryKey")]
    public PrimaryKey? PrimaryKey { get; set; }

    [JsonPropertyName("properties")]
    public List<EntityProperty> Properties { get; set; } = new();
}

public sealed class PrimaryKey
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class EntityProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("isInGrid")]
    public bool IsInGrid { get; set; }

    [JsonPropertyName("isInForm")]
    public bool IsInForm { get; set; }

    [JsonPropertyName("isFilterable")]
    public bool IsFilterable { get; set; }

    [JsonPropertyName("isReadOnly")]
    public bool IsReadOnly { get; set; }

    [JsonPropertyName("isSortable")]
    public bool? IsSortable { get; set; }
}
