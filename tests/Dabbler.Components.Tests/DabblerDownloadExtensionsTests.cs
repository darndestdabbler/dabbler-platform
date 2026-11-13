using System.Text;
using Bunit;
using FluentAssertions;
using Microsoft.JSInterop;
using Xunit;

namespace Dabbler.Components.Tests;

public sealed class DabblerDownloadExtensionsTests
{
    private const string ModulePath = "./_content/Dabbler.Components/dabbler-download.js";

    [Fact]
    public async Task DownloadFileAsync_ImportsTheShippedDownloadModule()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("downloadFile", _ => true).SetVoidResult();

        await ctx.JSInterop.JSRuntime.DownloadFileAsync("a.txt", [1, 2, 3], "text/plain");

        ctx.JSInterop.VerifyInvoke("import")
            .Arguments.Should().Equal(ModulePath);
    }

    [Fact]
    public async Task DownloadFileAsync_PassesFileNameBase64ContentAndMimeType()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var download = module.SetupVoid("downloadFile", _ => true);
        download.SetVoidResult();
        var content = Encoding.UTF8.GetBytes("Name,Amount\r\nAcme,1.50\r\n");

        await ctx.JSInterop.JSRuntime.DownloadFileAsync("orders.csv", content, "text/csv");

        download.VerifyInvoke("downloadFile").Arguments.Should().Equal(
            "orders.csv", Convert.ToBase64String(content), "text/csv");
    }

    [Fact]
    public async Task DownloadFileAsync_NullContent_Throws()
    {
        await using var ctx = new BunitContext();

        var act = () => ctx.JSInterop.JSRuntime.DownloadFileAsync("a.txt", null!, "text/plain");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
