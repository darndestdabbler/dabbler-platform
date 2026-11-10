namespace Dabbler.Components.Tests.MasterDetail;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

// ── Local test model mirroring the fields used in OrderList.razor ────────────

internal sealed class OrderFilterTestDto
{
    public OrderFilterTestStatus Status { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}

internal enum OrderFilterTestStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }

// ── Tests ────────────────────────────────────────────────────────────────────

public class OrdersFilterConfigTests
{
    // Mirrors the FilterConfig<OrderDto> declared in OrderList.razor.
    private static readonly FilterConfig<OrderFilterTestDto> _config = new(
        FilterMode.ServerSide,
        new FilterProperty<OrderFilterTestDto>[]
        {
            new FilterProperty<OrderFilterTestDto>.EnumVal
            {
                Property = e => (object?)e.Status,
                Label = "Status",
            },
            new FilterProperty<OrderFilterTestDto>.Text
            {
                Property = e => e.CustomerName,
                Label = "Retail Business",
            },
            new FilterProperty<OrderFilterTestDto>.Date
            {
                Property = e => (object?)e.OrderDate,
                Label = "Order Date",
                AsRange = true,
            },
            new FilterProperty<OrderFilterTestDto>.Numeric
            {
                Property = e => (object?)e.Total,
                Label = "Total",
                AsRange = true,
                MinPlaceholder = "Min $",
                MaxPlaceholder = "Max $",
            },
        });

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

    // ── Config shape tests ───────────────────────────────────────────────────

    [Fact]
    public void Config_Has_Four_Properties()
    {
        _config.Properties.Should().HaveCount(4);
    }

    [Fact]
    public void Status_Property_Is_EnumVal()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<OrderFilterTestDto>.GetPropertyName(p.Property) == nameof(OrderFilterTestDto.Status));

        prop.Should().BeOfType<FilterProperty<OrderFilterTestDto>.EnumVal>();
    }

    [Fact]
    public void CustomerName_Property_Is_Text_With_Correct_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<OrderFilterTestDto>.GetPropertyName(p.Property) == nameof(OrderFilterTestDto.CustomerName));

        prop.Should().BeOfType<FilterProperty<OrderFilterTestDto>.Text>();
        prop.Label.Should().Be("Retail Business");
    }

    [Fact]
    public void OrderDate_Property_Is_Date_AsRange()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<OrderFilterTestDto>.GetPropertyName(p.Property) == nameof(OrderFilterTestDto.OrderDate));

        var dateProp = prop.Should().BeOfType<FilterProperty<OrderFilterTestDto>.Date>().Subject;
        dateProp.AsRange.Should().BeTrue();
    }

    [Fact]
    public void Total_Property_Is_Numeric_AsRange_With_Placeholders()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<OrderFilterTestDto>.GetPropertyName(p.Property) == nameof(OrderFilterTestDto.Total));

        var numericProp = prop.Should().BeOfType<FilterProperty<OrderFilterTestDto>.Numeric>().Subject;
        numericProp.AsRange.Should().BeTrue();
        numericProp.MinPlaceholder.Should().Be("Min $");
        numericProp.MaxPlaceholder.Should().Be("Max $");
    }

    // ── bUnit filter-wiring tests (render DeclarativeFilterPanel with orders config) ─

    [Fact]
    public async Task StatusFilter_EnumChanged_EmitsEqDescriptor()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<OrderFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<OrderFilterTestDto>)
                .GetMethod("OnEnumChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, ["Status", OrderFilterTestStatus.Pending]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == "Status" && f.Operator == "eq");
    }

    [Fact]
    public async Task CustomerNameFilter_TextChanged_EmitsContainsDescriptor()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<OrderFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<OrderFilterTestDto>)
                .GetMethod("OnTextChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, ["CustomerName", "coffee co"]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == "CustomerName" && f.Operator == "contains");
    }

    [Fact]
    public async Task TotalRangeFilter_MinSet_EmitsGteDescriptor()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<OrderFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<OrderFilterTestDto>)
                .GetMethod("OnNumericChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, ["Total_min", (decimal?)50m]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == "Total" && f.Operator == "gte");
    }
}
