namespace Dabbler.Components.Tests.MasterDetail;

using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Dabbler.Components.Data;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

// ── Local test model mirroring the fields used in CustomerList.razor ─────────

internal sealed class CustomerFilterTestDto
{
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; }
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class CustomersFilterConfigTests
{
    // Mirrors the FilterConfig<CustomerDto> declared in CustomerList.razor.
    private static readonly FilterConfig<CustomerFilterTestDto> _config = new(
        FilterMode.ServerSide,
        new FilterProperty<CustomerFilterTestDto>[]
        {
            new FilterProperty<CustomerFilterTestDto>.Text
            {
                Property = e => (object?)e.LastName,
                Label = "Last Name",
            },
            new FilterProperty<CustomerFilterTestDto>.Text
            {
                Property = e => (object?)e.Email,
                Label = "Email",
            },
            new FilterProperty<CustomerFilterTestDto>.Text
            {
                Property = e => (object?)e.City,
                Label = "City",
            },
            new FilterProperty<CustomerFilterTestDto>.Text
            {
                Property = e => (object?)e.State,
                Label = "State",
            },
            new FilterProperty<CustomerFilterTestDto>.Bool
            {
                Property = e => (object?)e.IsActive,
                Label = "Active",
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
    public void Config_Has_Five_Properties()
    {
        _config.Properties.Should().HaveCount(5);
    }

    [Fact]
    public void FilterMode_Is_ServerSide()
    {
        _config.Mode.Should().Be(FilterMode.ServerSide);
    }

    [Fact]
    public void LastName_Is_Text_With_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<CustomerFilterTestDto>.GetPropertyName(p.Property) == nameof(CustomerFilterTestDto.LastName));
        prop.Should().BeOfType<FilterProperty<CustomerFilterTestDto>.Text>();
        prop.Label.Should().Be("Last Name");
    }

    [Fact]
    public void Email_Is_Text_With_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<CustomerFilterTestDto>.GetPropertyName(p.Property) == nameof(CustomerFilterTestDto.Email));
        prop.Should().BeOfType<FilterProperty<CustomerFilterTestDto>.Text>();
        prop.Label.Should().Be("Email");
    }

    [Fact]
    public void City_Is_Text_With_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<CustomerFilterTestDto>.GetPropertyName(p.Property) == nameof(CustomerFilterTestDto.City));
        prop.Should().BeOfType<FilterProperty<CustomerFilterTestDto>.Text>();
        prop.Label.Should().Be("City");
    }

    [Fact]
    public void State_Is_Text_With_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<CustomerFilterTestDto>.GetPropertyName(p.Property) == nameof(CustomerFilterTestDto.State));
        prop.Should().BeOfType<FilterProperty<CustomerFilterTestDto>.Text>();
        prop.Label.Should().Be("State");
    }

    [Fact]
    public void IsActive_Is_Bool_With_Label()
    {
        var prop = _config.Properties
            .Single(p => FilterProperty<CustomerFilterTestDto>.GetPropertyName(p.Property) == nameof(CustomerFilterTestDto.IsActive));
        prop.Should().BeOfType<FilterProperty<CustomerFilterTestDto>.Bool>();
        prop.Label.Should().Be("Active");
    }

    // ── Filter wiring tests ──────────────────────────────────────────────────

    [Fact]
    public async Task LastNameFilter_TextChanged_EmitsContainsDescriptor()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<CustomerFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<CustomerFilterTestDto>)
                .GetMethod("OnTextChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, [nameof(CustomerFilterTestDto.LastName), "Smith"]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == nameof(CustomerFilterTestDto.LastName) && f.Operator == FilterOperators.Contains);
    }

    [Fact]
    public async Task IsActiveFilter_BoolChanged_EmitsEqDescriptor()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = null;

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<CustomerFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<CustomerFilterTestDto>)
                .GetMethod("OnBoolChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, [nameof(CustomerFilterTestDto.IsActive), (bool?)true]));

        captured.Should().NotBeNull();
        captured!.Filters.Should().ContainSingle(f =>
            f.PropertyName == nameof(CustomerFilterTestDto.IsActive) && f.Operator == FilterOperators.Equals);
    }

    [Fact]
    public async Task ClearFilter_EmitsNull()
    {
        using var ctx = CreateContext();
        CompositeFilterDescriptor? captured = new CompositeFilterDescriptor();

        var cut = ctx.RenderComponent<DeclarativeFilterPanel<CustomerFilterTestDto>>(p => p
            .Add(x => x.Config, _config)
            .Add(x => x.FilterChanged, f => captured = f));

        var panel = cut.Instance;
        // Set a filter first, then clear it
        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<CustomerFilterTestDto>)
                .GetMethod("OnTextChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, [nameof(CustomerFilterTestDto.LastName), "Smith"]));

        captured.Should().NotBeNull("should have a filter after setting");

        await cut.InvokeAsync(() =>
            typeof(DeclarativeFilterPanel<CustomerFilterTestDto>)
                .GetMethod("OnTextChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(panel, [nameof(CustomerFilterTestDto.LastName), null]));

        captured.Should().BeNull("should be null when all filters cleared");
    }
}
