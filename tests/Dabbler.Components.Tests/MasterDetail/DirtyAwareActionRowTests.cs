using System.Linq;
using Bunit;
using Dabbler.Components.MasterDetail;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Dabbler.Components.Tests.MasterDetail;

public class DirtyAwareActionRowTests
{
    /// <summary>
    /// Custom bUnit setup that captures the <see cref="MudDialogProvider"/>
    /// ref so dialog-confirmation tests can search the provider's rendered
    /// markup. The project's <c>SetupForDabblerComponents</c> helper renders
    /// the provider but does not return it.
    /// </summary>
    private static (TestContext Ctx, IRenderedComponent<MudDialogProvider> DialogProvider) CreateContextWithDialogProvider()
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.RenderComponent<MudPopoverProvider>();
        var dialogProvider = ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();

        return (ctx, dialogProvider);
    }

    [Fact]
    public void Renders_SaveButton_WithDefaultTestId()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>();
            cut.Find("[data-testid='save-button']").Should().NotBeNull();
        }
    }

    [Fact]
    public void SaveButton_IsDisabled_WhenNotDirty()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, false));

            cut.Find("[data-testid='save-button']").HasAttribute("disabled").Should().BeTrue();
        }
    }

    [Fact]
    public void SaveButton_IsEnabled_WhenDirtyAndNotSaving()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.Saving, false));

            cut.Find("[data-testid='save-button']").HasAttribute("disabled").Should().BeFalse();
        }
    }

    [Fact]
    public void SaveButton_IsDisabled_WhenSaving()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.Saving, true));

            cut.Find("[data-testid='save-button']").HasAttribute("disabled").Should().BeTrue();
        }
    }

    [Fact]
    public void SaveButton_OnClick_InvokesOnSaveCallback()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onSaveCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.OnSave, EventCallback.Factory.Create(this, () => onSaveCalled = true)));

            cut.Find("[data-testid='save-button']").Click();

            onSaveCalled.Should().BeTrue();
        }
    }

    [Fact]
    public void CancelButton_RendersByDefault_WithDefaultTestId()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>();
            cut.Find("[data-testid='cancel-button']").Should().NotBeNull();
        }
    }

    [Fact]
    public void CancelButton_IsNotRendered_WhenShowCancelIsFalse()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.ShowCancel, false));
            cut.FindAll("[data-testid='cancel-button']").Should().BeEmpty();
        }
    }

    [Fact]
    public void CancelButton_LabelIsClose_WhenNotDirty()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, false)
                .Add(x => x.CloseLabel, "Test Close"));
            cut.Find("[data-testid='cancel-button']").TextContent.Trim().Should().Be("Test Close");
        }
    }

    [Fact]
    public void CancelButton_LabelIsCancel_WhenDirty()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.CancelLabel, "Test Cancel"));
            cut.Find("[data-testid='cancel-button']").TextContent.Trim().Should().Be("Test Cancel");
        }
    }

    [Fact]
    public void CancelButton_OnClick_InvokesOnCancelDirectly_WhenNotDirty()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onCancelCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, false)
                .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => onCancelCalled = true)));

            cut.Find("[data-testid='cancel-button']").Click();

            onCancelCalled.Should().BeTrue();
        }
    }

    [Fact]
    public void CancelButton_OnClick_ShowsDialogAndInvokesOnCancel_WhenDirtyAndConfirmed()
    {
        var (ctx, dialogProvider) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onCancelCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => onCancelCalled = true)));

            cut.Find("[data-testid='cancel-button']").Click();

            dialogProvider.WaitForState(() => dialogProvider.FindAll("button")
                .Any(b => b.TextContent.Trim() == "Discard"));

            dialogProvider.FindAll("button")
                .First(b => b.TextContent.Trim() == "Discard")
                .Click();

            onCancelCalled.Should().BeTrue();
        }
    }

    [Fact]
    public void CancelButton_OnClick_ShowsDialogAndDoesNotInvokeOnCancel_WhenDirtyAndDismissed()
    {
        var (ctx, dialogProvider) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onCancelCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.IsDirty, true)
                .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => onCancelCalled = true)));

            cut.Find("[data-testid='cancel-button']").Click();

            dialogProvider.WaitForState(() => dialogProvider.FindAll("button")
                .Any(b => b.TextContent.Trim() == "Keep editing"));

            dialogProvider.FindAll("button")
                .First(b => b.TextContent.Trim() == "Keep editing")
                .Click();

            onCancelCalled.Should().BeFalse();
        }
    }

    [Fact]
    public void DeleteButton_IsNotRendered_WhenOnDeleteHasNoDelegate()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>();
            cut.FindAll("[data-testid='delete-button']").Should().BeEmpty();
        }
    }

    [Fact]
    public void DeleteButton_IsRendered_WhenOnDeleteHasDelegate()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => { })));
            cut.Find("[data-testid='delete-button']").Should().NotBeNull();
        }
    }

    [Fact]
    public void DeleteButton_IsDisabled_WhenCanDeleteIsFalse()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => { }))
                .Add(x => x.CanDelete, false));
            cut.Find("[data-testid='delete-button']").HasAttribute("disabled").Should().BeTrue();
        }
    }

    [Fact]
    public void DeleteButton_OnClick_ShowsDialogAndInvokesOnDelete_WhenConfirmed()
    {
        var (ctx, dialogProvider) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onDeleteCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => onDeleteCalled = true)));

            cut.Find("[data-testid='delete-button']").Click();

            dialogProvider.WaitForState(() => dialogProvider.FindAll("button")
                .Any(b => b.TextContent.Trim() == "Delete"));

            dialogProvider.FindAll("button")
                .First(b => b.TextContent.Trim() == "Delete")
                .Click();

            onDeleteCalled.Should().BeTrue();
        }
    }

    [Fact]
    public void DeleteButton_OnClick_DoesNotInvokeOnDelete_WhenConfirmationDismissed()
    {
        var (ctx, dialogProvider) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var onDeleteCalled = false;
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => onDeleteCalled = true)));

            cut.Find("[data-testid='delete-button']").Click();

            dialogProvider.WaitForState(() => dialogProvider.FindAll("button")
                .Any(b => b.TextContent.Trim() == "Cancel"));

            dialogProvider.FindAll("button")
                .First(b => b.TextContent.Trim() == "Cancel")
                .Click();

            onDeleteCalled.Should().BeFalse();
        }
    }

    [Fact]
    public void CustomActions_AreRendered()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.CustomActions,
                    builder => builder.AddMarkupContent(0, "<div data-testid='custom-action'></div>")));
            cut.Find("[data-testid='custom-action']").Should().NotBeNull();
        }
    }

    [Fact]
    public void CustomActions_RenderAtEnd_ByDefault()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => { }))
                .Add(x => x.CustomActions,
                    builder => builder.AddMarkupContent(0, "<button data-testid='custom-action'></button>")));

            // Compare positions in the rendered markup via raw HTML index
            var html = cut.Markup;
            var customIdx = html.IndexOf("data-testid='custom-action'", System.StringComparison.Ordinal);
            if (customIdx < 0)
            {
                customIdx = html.IndexOf("data-testid=\"custom-action\"", System.StringComparison.Ordinal);
            }

            var saveIdx = html.IndexOf("data-testid='save-button'", System.StringComparison.Ordinal);
            if (saveIdx < 0)
            {
                saveIdx = html.IndexOf("data-testid=\"save-button\"", System.StringComparison.Ordinal);
            }

            saveIdx.Should().BeGreaterThan(-1);
            customIdx.Should().BeGreaterThan(-1);
            customIdx.Should().BeGreaterThan(saveIdx);
        }
    }

    [Fact]
    public void CustomActions_RenderAtStart_WhenPositionIsStart()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => { }))
                .Add(x => x.CustomActionsPosition, ActionRowSlot.Start)
                .Add(x => x.CustomActions,
                    builder => builder.AddMarkupContent(0, "<button data-testid='custom-action'></button>")));

            var html = cut.Markup;
            var customIdx = html.IndexOf("data-testid='custom-action'", System.StringComparison.Ordinal);
            if (customIdx < 0)
            {
                customIdx = html.IndexOf("data-testid=\"custom-action\"", System.StringComparison.Ordinal);
            }

            var saveIdx = html.IndexOf("data-testid='save-button'", System.StringComparison.Ordinal);
            if (saveIdx < 0)
            {
                saveIdx = html.IndexOf("data-testid=\"save-button\"", System.StringComparison.Ordinal);
            }

            saveIdx.Should().BeGreaterThan(-1);
            customIdx.Should().BeGreaterThan(-1);
            customIdx.Should().BeLessThan(saveIdx);
        }
    }

    [Fact]
    public void CustomTestIds_AreAppliedToButtons()
    {
        var (ctx, _) = CreateContextWithDialogProvider();
        using (ctx)
        {
            var cut = ctx.RenderComponent<DirtyAwareActionRow>(p => p
                .Add(x => x.OnDelete, EventCallback.Factory.Create(this, () => { }))
                .Add(x => x.SaveTestId, "my-save")
                .Add(x => x.CancelTestId, "my-cancel")
                .Add(x => x.DeleteTestId, "my-delete"));

            cut.Find("[data-testid='my-save']").Should().NotBeNull();
            cut.Find("[data-testid='my-cancel']").Should().NotBeNull();
            cut.Find("[data-testid='my-delete']").Should().NotBeNull();

            cut.FindAll("[data-testid='save-button']").Should().BeEmpty();
        }
    }
}
