using Dabbler.Security.Services;
using Xunit;

namespace Dabbler.Security.Tests.Services;

public class FieldMaskServiceTests
{
    private readonly FieldMaskService _svc = new();

    [Fact]
    public void SsnFormat_RendersLast4()
    {
        var result = _svc.RenderMasked("XXX-XX-{last4}", "123-45-6789");
        Assert.Equal("XXX-XX-6789", result);
    }

    [Fact]
    public void Last4_NullInput_RendersFourStars()
    {
        var result = _svc.RenderMasked("{last4}", null);
        Assert.Equal("****", result);
    }

    [Fact]
    public void Last4_ShortInput_RendersFourStars()
    {
        var result = _svc.RenderMasked("{last4}", "123");
        Assert.Equal("****", result);
    }

    [Fact]
    public void FirstChar_NullInput_RendersSingleStar()
    {
        var result = _svc.RenderMasked("{firstChar}", null);
        Assert.Equal("*", result);
    }

    [Fact]
    public void FirstChar_NonEmptyInput_RendersFirstChar()
    {
        var result = _svc.RenderMasked("{firstChar}***", "Hello");
        Assert.Equal("H***", result);
    }

    [Fact]
    public void UnknownToken_RendersLiterally()
    {
        var result = _svc.RenderMasked("value: {unknown}", "abc");
        Assert.Equal("value: {unknown}", result);
    }

    [Fact]
    public void MultiToken_RendersAll()
    {
        var result = _svc.RenderMasked("{firstChar}***-**-{last4}", "123-45-6789");
        Assert.Equal("1***-**-6789", result);
    }

    [Fact]
    public void EmptyMaskFormat_RendersEightStars()
    {
        var result = _svc.RenderMasked(string.Empty, "any");
        Assert.Equal("********", result);
    }

    [Fact]
    public void NullStoredValue_NonEmptyResult()
    {
        // Null stored value must still produce a non-empty masked string so
        // field presence/absence never leaks through an empty control.
        var result = _svc.RenderMasked("XXX-XX-{last4}", null);
        Assert.False(string.IsNullOrEmpty(result));
        Assert.Equal("XXX-XX-****", result);
    }
}
