using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AcmeCoffeeOnline.Web.Tests;

/// <summary>
/// Pins how a <see cref="WebApplicationFactory{TEntryPoint}"/> host serves a
/// referenced library's static web asset. The web host's defaults turn static
/// web assets on only in the Development environment; any other environment
/// needs <c>UseStaticWebAssets()</c> on the test host, or the request for
/// <c>_content/Dabbler.Components/dabbler-download.js</c> fails after the
/// response has started (a consumer's real-socket host saw 200 with an empty
/// body).
/// </summary>
public sealed class StaticWebAssetHostTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AssetPath = "/_content/Dabbler.Components/dabbler-download.js";

    [Theory]
    [InlineData("Test")]
    [InlineData("Production")]
    public async Task NonDevelopmentHost_WithoutUseStaticWebAssets_CannotFindTheAsset(string environment)
    {
        using var host = factory.WithWebHostBuilder(b => b.UseEnvironment(environment));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => client.GetAsync(AssetPath));

        Assert.EndsWith(Path.Combine("wwwroot", "_content", "Dabbler.Components", "dabbler-download.js"), ex.FileName);
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Test", true)]
    [InlineData("Production", true)]
    public async Task Host_WithStaticWebAssets_ServesTheAsset(string environment, bool useStaticWebAssets)
    {
        using var host = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(environment);
            if (useStaticWebAssets)
            {
                b.UseStaticWebAssets();
            }
        });
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(AssetPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("export function downloadFile", body);
    }
}
