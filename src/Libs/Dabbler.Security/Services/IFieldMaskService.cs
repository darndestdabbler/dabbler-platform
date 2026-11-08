using Dabbler.Security.Configuration;

namespace Dabbler.Security.Services;

/// <summary>
/// Renders a masked display value from a <see cref="DefaultMaskRule"/> format string and
/// the raw stored value. Token substitution rules:
/// <list type="bullet">
///   <item><c>{last4}</c> — last 4 characters, or <c>****</c> when the value has fewer than 4 characters or is null.</item>
///   <item><c>{firstChar}</c> — first character, or <c>*</c> when the value is null or empty.</item>
///   <item>Any other <c>{…}</c> token — rendered literally (no throw).</item>
///   <item>A null stored value still produces a non-empty masked string so field presence/absence never leaks.</item>
/// </list>
/// </summary>
public interface IFieldMaskService
{
    /// <summary>
    /// Renders the <paramref name="maskFormat"/> string with tokens replaced by
    /// values derived from <paramref name="storedValue"/>.
    /// </summary>
    string RenderMasked(string? maskFormat, string? storedValue);
}
