using Dabbler.Components;
using Dabbler.Reports.Core.Layout;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ReportFontsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDabblerReportFonts_RegistersTheConfiguredFontsAsASingleton()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");
        var services = new ServiceCollection();

        services.AddDabblerReportFonts(fonts => fonts.Register("Liberation Sans", path));

        using var provider = services.BuildServiceProvider();
        var fonts = provider.GetRequiredService<ReportFonts>();
        fonts.GetFontFile("Liberation Sans", bold: false, italic: false).Should().Be(path);
        provider.GetRequiredService<ReportFonts>().Should().BeSameAs(fonts);
    }

    [Fact]
    public void AddDabblerReportFonts_RejectsANullConfigure()
    {
        var services = new ServiceCollection();

        services.Invoking(s => s.AddDabblerReportFonts(null!)).Should().Throw<ArgumentNullException>();
    }
}
