using System;
using System.ComponentModel;
using FluentAssertions;
using Xunit;

namespace Dabbler.Components.Tests.Pages;

public class CompatibilitySurfaceTests
{
    [Fact]
    public void CatalogPage_IsMarkedCompatibilityOnly()
    {
#pragma warning disable CS0618
        var obsolete = typeof(DabblerCatalogPage).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false);
        var editorBrowsable = typeof(DabblerCatalogPage).GetCustomAttributes(typeof(EditorBrowsableAttribute), inherit: false);
#pragma warning restore CS0618

        obsolete.Should().ContainSingle();
        editorBrowsable.Should().ContainSingle();
        ((EditorBrowsableAttribute)editorBrowsable.Single()).State.Should().Be(EditorBrowsableState.Never);
    }
}
