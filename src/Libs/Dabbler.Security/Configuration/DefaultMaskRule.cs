namespace Dabbler.Security.Configuration;

/// <summary>
/// Declares a column as platform-level sensitive. Every role sees the column's
/// value rendered through <see cref="MaskFormat"/> unless an active
/// <see cref="MaskExemption"/> targets the same entity + column for the user's
/// active role.
/// </summary>
/// <remarks>
/// <para>
/// Masking is modelled as <b>platform-wide default + per-role exemption</b> — the
/// inverse of <see cref="ColumnRestriction"/>, which is <b>per-role rule + platform
/// default of "allowed"</b>. The inversion preserves security-by-default: declare a
/// column sensitive once, and every role sees masked until explicitly exempted.
/// </para>
/// <para>
/// The rendered mask applies <b>regardless of whether the stored value is null</b>.
/// Showing "empty" for a sensitive-when-present column would itself leak information
/// (e.g., whether a diagnosis date exists). The rendering surface is wired in
/// Session Set D.
/// </para>
/// </remarks>
public sealed class DefaultMaskRule
{
    /// <summary>
    /// The entity (table) the sensitive column belongs to.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>
    /// The sensitive column on the entity.
    /// </summary>
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>
    /// The mask template. Tokens supported by the mask service include
    /// <c>{last4}</c> (last four characters of the stored value) and
    /// <c>{firstChar}</c> (first character of the stored value). A blank
    /// <see cref="MaskFormat"/> renders as <c>********</c>. Token evaluation
    /// against a null stored value yields the empty string so the mask still
    /// renders a stable shape — see Session Set D.
    /// </summary>
    public string MaskFormat { get; set; } = string.Empty;
}
