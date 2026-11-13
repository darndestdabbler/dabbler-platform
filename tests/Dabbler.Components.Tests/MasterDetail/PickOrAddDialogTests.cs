using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Dabbler.Components.Tests.MasterDetail;

/// <summary>
/// bUnit tests for <see cref="PickOrAddDialog{TRow,TEntity,TLink}"/>.
///
/// MudBlazor renders dialog content inside <see cref="MudDialogProvider"/>,
/// not inside the <see cref="MudDialog"/> component itself — the provider is
/// the correct search/invocation root after calling <c>IDialogService.ShowAsync</c>.
/// </summary>
public class PickOrAddDialogTests
{
    private sealed record TestRow { public int Id { get; init; } }
    private sealed record TestEntity { public string Name { get; init; } = ""; }

    private static IRenderedComponent<MudDialogProvider> SetupContext(BunitContext ctx)
    {
        var authCtx = ctx.AddAuthorization();
        authCtx.SetAuthorized("testuser@test.local");

        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Render<MudPopoverProvider>();
        var dialogProvider = ctx.Render<MudDialogProvider>();
        ctx.Render<MudSnackbarProvider>();
        return dialogProvider;
    }

    private static MasterDetailExpansionShell<TestRow, TestEntity, object>? GetShell(
        IRenderedComponent<PickOrAddDialog<TestRow, TestEntity, object>> dialogComponent)
    {
        var shellField = typeof(PickOrAddDialog<TestRow, TestEntity, object>)
            .GetField("_shell", BindingFlags.NonPublic | BindingFlags.Instance);
        return (MasterDetailExpansionShell<TestRow, TestEntity, object>?)
            shellField?.GetValue(dialogComponent.Instance);
    }

    [Fact]
    public async Task OnUsePicked_ReturningNonNull_ClosesDialogWithResult()
    {
        await using var ctx = new BunitContext();
        var provider = SetupContext(ctx);
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();
        var rowToPick = new TestRow { Id = 123 };
        const string expectedResult = "Result-123";

        var parameters = new DialogParameters<PickOrAddDialog<TestRow, TestEntity, object>>
        {
            { p => p.OnUsePicked, (TestRow _) => Task.FromResult<object?>(expectedResult) },
        };

        IDialogReference? dialogReference = null;
        await provider.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<PickOrAddDialog<TestRow, TestEntity, object>>(
                "Test Dialog", parameters);
        });

        var dialogComponent = provider.FindComponent<PickOrAddDialog<TestRow, TestEntity, object>>();
        var shellInstance = GetShell(dialogComponent);
        shellInstance.Should().NotBeNull();

        await provider.InvokeAsync(() => shellInstance!.RaiseUseRequestedAsync(rowToPick));

        var result = await dialogReference!.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(expectedResult);
    }

    [Fact]
    public async Task OnUsePicked_ReturningNull_KeepsDialogOpen()
    {
        await using var ctx = new BunitContext();
        var provider = SetupContext(ctx);
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();
        var rowToPick = new TestRow { Id = 123 };

        var parameters = new DialogParameters<PickOrAddDialog<TestRow, TestEntity, object>>
        {
            { p => p.OnUsePicked, (TestRow _) => Task.FromResult<object?>(null) },
        };

        IDialogReference? dialogReference = null;
        await provider.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<PickOrAddDialog<TestRow, TestEntity, object>>(
                "Test Dialog", parameters);
        });

        var dialogComponent = provider.FindComponent<PickOrAddDialog<TestRow, TestEntity, object>>();
        var shellInstance = GetShell(dialogComponent);
        shellInstance.Should().NotBeNull();

        await provider.InvokeAsync(() => shellInstance!.RaiseUseRequestedAsync(rowToPick));

        dialogReference!.Result.IsCompleted.Should().BeFalse();
        provider.FindComponents<PickOrAddDialog<TestRow, TestEntity, object>>().Count.Should().Be(1);
    }

    [Fact]
    public async Task OnSaveAndUse_ReturningNonNull_ClosesDialogWithResult()
    {
        await using var ctx = new BunitContext();
        var provider = SetupContext(ctx);
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();
        const string expectedResult = "Saved-Result-456";

        // The shell's default primary-action button only renders when
        // DetailsContent is non-null. Pass an empty fragment so the action
        // row appears and can be clicked.
        var parameters = new DialogParameters<PickOrAddDialog<TestRow, TestEntity, object>>
        {
            { p => p.OnSaveAndUse, (SaveRequest<TestEntity, object> _) =>
                Task.FromResult<object?>(expectedResult) },
            { p => p.TestId, "dialog-shell" },
            { p => p.DetailsContent, (RenderFragment)(_ => { }) },
        };

        IDialogReference? dialogReference = null;
        await provider.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<PickOrAddDialog<TestRow, TestEntity, object>>(
                "Test Dialog", parameters);
        });

        var dialogComponent = provider.FindComponent<PickOrAddDialog<TestRow, TestEntity, object>>();
        var shellInstance = GetShell(dialogComponent);
        shellInstance.Should().NotBeNull();

        var button = provider.Find("[data-testid='dialog-shell-primary-button']");
        await provider.InvokeAsync(() => button.Click());

        var result = await dialogReference!.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(expectedResult);
    }

    [Fact]
    public async Task OnSaveAndUse_ReturningNull_KeepsDialogOpen()
    {
        await using var ctx = new BunitContext();
        var provider = SetupContext(ctx);
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<PickOrAddDialog<TestRow, TestEntity, object>>
        {
            { p => p.OnSaveAndUse, (SaveRequest<TestEntity, object> _) =>
                Task.FromResult<object?>(null) },
            { p => p.TestId, "dialog-shell" },
            { p => p.DetailsContent, (RenderFragment)(_ => { }) },
        };

        IDialogReference? dialogReference = null;
        await provider.InvokeAsync(async () =>
        {
            dialogReference = await dialogService.ShowAsync<PickOrAddDialog<TestRow, TestEntity, object>>(
                "Test Dialog", parameters);
        });

        var button = provider.Find("[data-testid='dialog-shell-primary-button']");
        await provider.InvokeAsync(() => button.Click());

        dialogReference!.Result.IsCompleted.Should().BeFalse();
        provider.FindComponents<PickOrAddDialog<TestRow, TestEntity, object>>().Count.Should().Be(1);
    }

    [Fact]
    public async Task OnAdd_RaisedWhenAddButtonClicked()
    {
        await using var ctx = new BunitContext();
        var provider = SetupContext(ctx);
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();
        var addCount = 0;

        var parameters = new DialogParameters<PickOrAddDialog<TestRow, TestEntity, object>>
        {
            { p => p.TestId, "dialog-shell" },
            { p => p.ShowAddAction, true },
            { p => p.OnAdd, EventCallback.Factory.Create(this, () => addCount++) },
        };

        await provider.InvokeAsync(async () =>
        {
            await dialogService.ShowAsync<PickOrAddDialog<TestRow, TestEntity, object>>(
                "Test Dialog", parameters);
        });

        var button = provider.Find("[data-testid='dialog-shell-add-button']");
        await provider.InvokeAsync(() => button.Click());

        addCount.Should().Be(1);
    }
}
