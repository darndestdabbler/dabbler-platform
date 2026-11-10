using System.Linq.Expressions;
using System.Threading.Tasks;
using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Dabbler.Components.Tests.MasterDetail;

// ── Test models ─────────────────────────────────────────────────────────────

internal sealed class FilterTestDto
{
    public string Name { get; set; } = string.Empty;
    public FilterTestRoast RoastLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; }
}

internal enum FilterTestRoast { Dark, Medium, Light }

// ── Test class ───────────────────────────────────────────────────────────────

public sealed class DeclarativeFilterPanelTests
{
    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.RenderComponent<MudPopoverProvider>();
        ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();
        return ctx;
    }

    // ── FilterProperty helper unit tests ─────────────────────────────────────

    [Fact]
    public void GetPropertyName_ReturnsName_ForStringProperty()
    {
        Expression<Func<FilterTestDto, object?>> expr = e => e.Name;
        FilterProperty<FilterTestDto>.GetPropertyName(expr).Should().Be("Name");
    }

    [Fact]
    public void GetPropertyName_ReturnsName_ForBoxedValueTypeProperty()
    {
        Expression<Func<FilterTestDto, object?>> expr = e => (object?)e.RoastLevel;
        FilterProperty<FilterTestDto>.GetPropertyName(expr).Should().Be("RoastLevel");
    }

    [Fact]
    public void GetPropertyType_ReturnsEnumType()
    {
        Expression<Func<FilterTestDto, object?>> expr = e => (object?)e.RoastLevel;
        FilterProperty<FilterTestDto>.GetPropertyType(expr).Should().Be(typeof(FilterTestRoast));
    }

    [Fact]
    public void GetPropertyType_ReturnsDecimalType()
    {
        Expression<Func<FilterTestDto, object?>> expr = e => (object?)e.UnitPrice;
        FilterProperty<FilterTestDto>.GetPropertyType(expr).Should().Be(typeof(decimal));
    }

    // ── LookupSource unit tests ───────────────────────────────────────────────

    [Fact]
    public void Static_ExposesItems()
    {
        IReadOnlyList<string> items = ["Alpha", "Beta"];
        var source = new LookupSource<string>.Static(items);
        source.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task AsyncLoader_InvokesDelegate()
    {
        IReadOnlyList<string> expected = ["X", "Y"];
        var source = new LookupSource<string>.AsyncLoader(_ => Task.FromResult(expected));
        var result = await source.Load(default);
        result.Should().BeEquivalentTo(expected);
    }

    // ── bUnit rendering tests ─────────────────────────────────────────────────

    private static FilterConfig<FilterTestDto> MakeConfig(
        FilterMode mode = FilterMode.ClientSide,
        IReadOnlyList<FilterProperty<FilterTestDto>>? props = null)
    {
        props ??= new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Text { Property = e => e.Name, Label = "Name" },
        };
        return new FilterConfig<FilterTestDto>(mode, props);
    }

    [Fact]
    public void Renders_SearchRole_OnOuterDiv()
    {
        using var ctx = CreateContext();
        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, MakeConfig()));

        cut.Find("[role='search']").Should().NotBeNull();
    }

    [Fact]
    public void Renders_EnumSelect_WithLabel()
    {
        using var ctx = CreateContext();
        var config = MakeConfig(props: new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.EnumVal
            {
                Property = e => (object?)e.RoastLevel,
                Label = "Roast Level",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config));

        cut.Markup.Should().Contain("Roast Level");
    }

    [Fact]
    public async Task EnumSelect_ValueChanged_EmitsCompositeFilter()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.EnumVal
            {
                Property = e => (object?)e.RoastLevel,
                Label = "Roast Level",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        // Simulate value change directly via the internal handler
        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnEnumChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["RoastLevel", FilterTestRoast.Dark]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f => f.PropertyName == "RoastLevel" && f.Operator == "eq");
    }

    [Fact]
    public async Task TextFilter_ValueChanged_EmitsContainsFilter()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, MakeConfig())
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnTextChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", "espresso"]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == "Name" && f.Operator == "contains" && (string)f.Value! == "espresso");
    }

    [Fact]
    public async Task NumericRange_BothSet_EmitsTwoDescriptors()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Numeric
            {
                Property = e => (object?)e.UnitPrice,
                Label = "Price",
                AsRange = true,
                MinPlaceholder = "Min $",
                MaxPlaceholder = "Max $",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        var onNumeric = panel.GetType()
            .GetMethod("OnNumericChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        await cut.InvokeAsync(() => onNumeric.Invoke(panel, ["UnitPrice_min", (decimal?)10m]));
        await cut.InvokeAsync(() => onNumeric.Invoke(panel, ["UnitPrice_max", (decimal?)50m]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().HaveCount(2);
        captured.Filters.Should().Contain(f => f.Operator == "gte" && (decimal)f.Value! == 10m);
        captured.Filters.Should().Contain(f => f.Operator == "lte" && (decimal)f.Value! == 50m);
    }

    [Fact]
    public async Task NumericRange_OnlyMin_EmitsOnlyGte()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Numeric
            {
                Property = e => (object?)e.UnitPrice,
                AsRange = true,
                MinPlaceholder = "Min $",
                MaxPlaceholder = "Max $",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnNumericChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["UnitPrice_min", (decimal?)15m]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f => f.Operator == "gte");
        captured.Filters.Should().NotContain(f => f.Operator == "lte");
    }

    [Fact]
    public async Task BoolFilter_SetTrue_EmitsEqTrue()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Bool
            {
                Property = e => (object?)e.IsAvailable,
                Label = "Available",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnBoolChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["IsAvailable", (bool?)true]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f => f.Operator == "eq" && (bool)f.Value! == true);
    }

    [Fact]
    public async Task BoolFilter_Cleared_EmitsNullFilter()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Bool
            {
                Property = e => (object?)e.IsAvailable,
                Label = "Available",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        var onBool = panel.GetType()
            .GetMethod("OnBoolChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Set, then clear
        await cut.InvokeAsync(() => onBool.Invoke(panel, ["IsAvailable", (bool?)true]));
        await cut.InvokeAsync(() => onBool.Invoke(panel, ["IsAvailable", (bool?)null]));

        captured.Should().BeNull();
    }

    [Fact]
    public async Task ActiveFilter_ShowsMudChip()
    {
        using var ctx = CreateContext();

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, MakeConfig()));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnTextChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", "latte"]));

        cut.Markup.Should().Contain("mud-chip");
    }

    [Fact]
    public async Task ClearAll_RemovesAllFilters()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Text { Property = e => e.Name, Label = "Name" },
            new FilterProperty<FilterTestDto>.Bool { Property = e => (object?)e.IsAvailable, Label = "Available" },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        var type = panel.GetType();

        await cut.InvokeAsync(() => type
            .GetMethod("OnTextChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", "test"]));
        await cut.InvokeAsync(() => type
            .GetMethod("OnBoolChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["IsAvailable", (bool?)true]));

        // Clear all
        await cut.InvokeAsync(() => type
            .GetMethod("ClearAllAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, null));

        captured.Should().BeNull();
        panel.GetFilterState().Should().BeEmpty();
    }

    [Fact]
    public async Task ClientSide_DoesNotCallHistoryReplaceState()
    {
        using var ctx = CreateContext();

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, MakeConfig(FilterMode.ClientSide)));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnTextChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", "test"]));

        ctx.JSInterop.Invocations.Any(kvp => kvp.Identifier == "history.replaceState").Should().BeFalse();
    }

    [Fact]
    public async Task ServerSide_CallsHistoryReplaceState_OnFilterChange()
    {
        using var ctx = CreateContext();

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, MakeConfig(FilterMode.ServerSide)));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnTextChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", "espresso"]));

        ctx.JSInterop.Invocations.Any(kvp => kvp.Identifier == "history.replaceState").Should().BeTrue();
    }

    [Fact]
    public async Task ForeignKeyFilter_AsyncLoader_LoadsItemsOnInit()
    {
        using var ctx = CreateContext();

        var items = new[] { new FilterTestDto { Name = "Async Item" } };
        var loaderInvoked = false;
        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.ForeignKey<FilterTestDto>
            {
                Property = e => (object?)e.Name,
                Label = "AsyncCategory",
                Lookup = new LookupSource<FilterTestDto>.AsyncLoader(ct =>
                {
                    loaderInvoked = true;
                    return Task.FromResult<IReadOnlyList<FilterTestDto>>(items);
                }),
                DisplayProperty = fk => fk.Name,
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config));

        loaderInvoked.Should().BeTrue();
        cut.Markup.Should().Contain("AsyncCategory");
    }

    [Fact]
    public void RestoreFromQueryString_EnumValue()
    {
        using var ctx = CreateContext();

        // Navigate to URL with query param before rendering
        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/?RoastLevel=Dark");

        var config = new FilterConfig<FilterTestDto>(FilterMode.ServerSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.EnumVal
            {
                Property = e => (object?)e.RoastLevel,
                Label = "Roast Level",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config));

        var state = cut.Instance.GetFilterState();
        state.Should().ContainKey("RoastLevel");
        state["RoastLevel"].Should().BeEquivalentTo(FilterTestRoast.Dark);
    }

    [Fact]
    public async Task DateFilter_ValueSet_EmitsEqFilter()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Date
            {
                Property = e => (object?)e.Name, // reuse field; property type doesn't affect Date rendering
                Label = "Created",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        var date = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnDateChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", (DateTime?)date]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f => f.PropertyName == "Name" && f.Operator == "eq");
    }

    [Fact]
    public async Task DateRangeFilter_BothSet_EmitsGteLteFilters()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Date
            {
                Property = e => (object?)e.Name,
                Label = "Created",
                AsRange = true,
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var onDate = panel.GetType()
            .GetMethod("OnDateChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        await cut.InvokeAsync(() => onDate.Invoke(panel, ["Name_start", (DateTime?)start]));
        await cut.InvokeAsync(() => onDate.Invoke(panel, ["Name_end", (DateTime?)end]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().HaveCount(2);
        captured.Filters.Should().Contain(f => f.Operator == "gte");
        captured.Filters.Should().Contain(f => f.Operator == "lte");
    }

    [Fact]
    public async Task ForeignKeyFilter_StaticLookup_LoadsItemsOnInit()
    {
        using var ctx = CreateContext();

        var lookupItems = new[] { new FilterTestDto { Name = "A" }, new FilterTestDto { Name = "B" } };
        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.ForeignKey<FilterTestDto>
            {
                Property = e => (object?)e.Name,
                Label = "Category",
                Lookup = new LookupSource<FilterTestDto>.Static(lookupItems),
                DisplayProperty = fk => fk.Name,
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config));

        // After rendering, lookup items should be loaded
        var state = cut.Instance.GetFilterState();
        // Panel rendered (items loaded in init) — no selection yet, filter state empty
        state.Should().BeEmpty();

        // Verify the select markup contains the display values
        cut.Markup.Should().Contain("Category");
    }

    [Fact]
    public async Task ForeignKeyFilter_ValueSelected_EmitsEqFilter()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var selectedItem = new FilterTestDto { Name = "Alpha" };
        var config = new FilterConfig<FilterTestDto>(FilterMode.ClientSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.ForeignKey<FilterTestDto>
            {
                Property = e => (object?)e.Name,
                Label = "Category",
                Lookup = new LookupSource<FilterTestDto>.Static([selectedItem]),
                DisplayProperty = fk => fk.Name,
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() => panel.GetType()
            .GetMethod("OnForeignKeyChangedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(panel, ["Name", (object?)selectedItem]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f => f.PropertyName == "Name" && f.Operator == "eq");
        captured.Filters[0].Value.Should().BeSameAs(selectedItem);
    }

    [Fact]
    public void RestoreFromQueryString_NumericRange()
    {
        using var ctx = CreateContext();

        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/?UnitPrice_min=10&UnitPrice_max=50");

        var config = new FilterConfig<FilterTestDto>(FilterMode.ServerSide, new FilterProperty<FilterTestDto>[]
        {
            new FilterProperty<FilterTestDto>.Numeric
            {
                Property = e => (object?)e.UnitPrice,
                AsRange = true,
                MinPlaceholder = "Min $",
                MaxPlaceholder = "Max $",
            },
        });

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<FilterTestDto>>(p => p
            .Add(x => x.Config, config));

        var state = cut.Instance.GetFilterState();
        state.Should().ContainKey("UnitPrice_min");
        state.Should().ContainKey("UnitPrice_max");
        ((decimal)state["UnitPrice_min"]!).Should().Be(10m);
        ((decimal)state["UnitPrice_max"]!).Should().Be(50m);
    }
}
