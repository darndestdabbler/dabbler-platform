using System.Collections.Concurrent;
using Dabbler.Components.Data.Metadata;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Provides the artifact catalog for AcmeCoffeeOnline — lists all forms,
/// reports, and dashboards available in the application.
/// </summary>
public class AcmeCoffeeCatalogProvider : IArtifactCatalogProvider
{
    private static readonly List<ArtifactCatalogEntry> _artifacts = new()
    {
        // ── Forms ──────────────────────────────────────────────────────
        new ArtifactCatalogEntry
        {
            ArtifactId = "form-product",
            ArtifactType = ArtifactType.Form,
            Name = "Product",
            Description = "View and edit coffee product details",
            Category = "Inventory",
            Tags = new List<string> { "product", "coffee", "inventory" },
            IconName = "Inventory",
            EntityName = "Product"
        },
        new ArtifactCatalogEntry
        {
            ArtifactId = "form-customer",
            ArtifactType = ArtifactType.Form,
            Name = "Customer",
            Description = "View and edit customer information",
            Category = "People",
            Tags = new List<string> { "customer", "contact", "people" },
            IconName = "Person",
            EntityName = "Customer"
        },
        new ArtifactCatalogEntry
        {
            ArtifactId = "form-order",
            ArtifactType = ArtifactType.Form,
            Name = "Order",
            Description = "View and manage coffee orders",
            Category = "Sales",
            Tags = new List<string> { "order", "sales", "transaction" },
            IconName = "ShoppingCart",
            EntityName = "Order"
        },

        // ── Reports ────────────────────────────────────────────────────
        new ArtifactCatalogEntry
        {
            ArtifactId = "orders-by-customer",
            ArtifactType = ArtifactType.Report,
            Name = "Orders by Customer",
            Description = "Banded report grouping orders by customer with subtotals and grand total",
            Category = "Sales",
            Tags = new List<string> { "orders", "customer", "sales", "report" },
            IconName = "Assessment"
        },
        new ArtifactCatalogEntry
        {
            ArtifactId = "sales-by-product",
            ArtifactType = ArtifactType.Report,
            Name = "Sales by Product",
            Description = "Product-level sales report showing quantities sold and revenue",
            Category = "Sales",
            Tags = new List<string> { "sales", "product", "revenue", "report" },
            IconName = "BarChart"
        },

        // ── Dashboards ─────────────────────────────────────────────────
        new ArtifactCatalogEntry
        {
            ArtifactId = "acme-home",
            ArtifactType = ArtifactType.Dashboard,
            Name = "Home Dashboard",
            Description = "Main landing dashboard with navigation, quick links, and recent activity",
            Category = "Navigation",
            Tags = new List<string> { "dashboard", "home", "navigation" },
            IconName = "Dashboard"
        }
    };

    public Task<IReadOnlyList<ArtifactCatalogEntry>> GetArtifactsAsync(
        ArtifactType artifactType, string? userId = null)
    {
        var results = _artifacts
            .Where(a => a.ArtifactType == artifactType)
            .OrderBy(a => a.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>(results);
    }

    public Task<IReadOnlyList<ArtifactCatalogEntry>> SearchArtifactsAsync(
        ArtifactType artifactType, string? userId, string searchText)
    {
        var lower = searchText.ToLowerInvariant();

        var results = _artifacts
            .Where(a => a.ArtifactType == artifactType)
            .Where(a =>
                a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || (a.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.Category?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.Tags?.Any(t => t.Contains(lower, StringComparison.OrdinalIgnoreCase)) ?? false))
            .OrderBy(a => a.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArtifactCatalogEntry>>(results);
    }
}

/// <summary>
/// In-memory usage tracking provider for dashboard recent items and favorites.
/// Stores access history and pin state per user in a thread-safe dictionary.
/// </summary>
public class InMemoryUsageTrackingProvider : IUsageTrackingProvider
{
    private readonly ConcurrentDictionary<string, ArtifactUsageRecord> _records = new();

    public Task<IReadOnlyList<ArtifactUsageRecord>> GetPinnedAsync(
        string userId, ArtifactType artifactType)
    {
        var results = _records.Values
            .Where(r => r.UserId == userId && r.IsPinned && r.ArtifactType == artifactType)
            .OrderBy(r => r.ArtifactType)
            .ThenBy(r => r.ArtifactId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(results);
    }

    public Task<IReadOnlyList<ArtifactUsageRecord>> GetRecentAsync(
        string userId, ArtifactType artifactType, int maxItems = 7)
    {
        var results = _records.Values
            .Where(r => r.UserId == userId && r.ArtifactType == artifactType)
            .OrderByDescending(r => r.LastAccessedAt)
            .Take(maxItems)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(results);
    }

    public Task<IReadOnlyList<ArtifactUsageRecord>> GetFrequentAsync(
        string userId, ArtifactType artifactType, int maxItems = 7)
    {
        var results = _records.Values
            .Where(r => r.UserId == userId && r.ArtifactType == artifactType)
            .OrderByDescending(r => r.AccessCount)
            .ThenByDescending(r => r.LastAccessedAt)
            .Take(maxItems)
            .ToList();

        return Task.FromResult<IReadOnlyList<ArtifactUsageRecord>>(results);
    }

    public Task TrackAccessAsync(string userId, ArtifactType artifactType, string artifactId)
    {
        var key = $"{userId}:{artifactType}:{artifactId}";

        _records.AddOrUpdate(key,
            _ => new ArtifactUsageRecord
            {
                UserId = userId,
                ArtifactType = artifactType,
                ArtifactId = artifactId,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 1,
                IsPinned = false
            },
            (_, existing) =>
            {
                existing.LastAccessedAt = DateTime.UtcNow;
                existing.AccessCount++;
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task SetPinnedAsync(
        string userId, ArtifactType artifactType, string artifactId, bool isPinned)
    {
        var key = $"{userId}:{artifactType}:{artifactId}";

        _records.AddOrUpdate(key,
            _ => new ArtifactUsageRecord
            {
                UserId = userId,
                ArtifactType = artifactType,
                ArtifactId = artifactId,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 0,
                IsPinned = isPinned
            },
            (_, existing) =>
            {
                existing.IsPinned = isPinned;
                return existing;
            });

        return Task.CompletedTask;
    }
}
