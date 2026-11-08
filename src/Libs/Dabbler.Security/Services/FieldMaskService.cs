using System.Text.RegularExpressions;

namespace Dabbler.Security.Services;

/// <summary>
/// Default stateless implementation of <see cref="IFieldMaskService"/>.
/// </summary>
public sealed class FieldMaskService : IFieldMaskService
{
    private static readonly Regex TokenPattern = new(@"\{[^{}]+\}", RegexOptions.Compiled);

    /// <inheritdoc />
    public string RenderMasked(string? maskFormat, string? storedValue)
    {
        if (string.IsNullOrEmpty(maskFormat))
            return "********";

        return TokenPattern.Replace(maskFormat, m => ReplaceToken(m.Value, storedValue));
    }

    private static string ReplaceToken(string token, string? value) => token switch
    {
        "{last4}" => value is { Length: >= 4 } v ? v[^4..] : "****",
        "{firstChar}" => value is { Length: >= 1 } v ? v[..1] : "*",
        _ => token, // unknown token renders literally
    };
}
