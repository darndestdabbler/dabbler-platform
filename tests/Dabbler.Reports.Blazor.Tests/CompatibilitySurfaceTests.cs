using System.ComponentModel;
using Dabbler.Components;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class CompatibilitySurfaceTests
{
    [Fact]
#pragma warning disable CS0618
    public void QueryReportPage_RemainsCompatibilityOnly()
    {
        var obsolete = typeof(DabblerQueryReportPage<>).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false);
        obsolete.Should().ContainSingle();

        var editorBrowsable = (EditorBrowsableAttribute?)Attribute.GetCustomAttribute(
            typeof(DabblerQueryReportPage<>),
            typeof(EditorBrowsableAttribute));

        editorBrowsable.Should().NotBeNull();
        editorBrowsable!.State.Should().Be(EditorBrowsableState.Never);
    }
#pragma warning restore CS0618
}
