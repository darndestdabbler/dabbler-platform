// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Reports.Core.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Dabbler.Components;

/// <summary>
/// DI registration of the TrueType font families <see cref="DabblerReportPage"/> measures
/// and exports report text with.
/// </summary>
public static class ReportFontsServiceCollectionExtensions
{
    /// <summary>
    /// Registers one <see cref="ReportFonts"/>, configured by <paramref name="configure"/>,
    /// as a singleton. <see cref="DabblerReportPage"/> uses it when composing pages and
    /// exporting the PDF; without it every family maps to the Base 14 fonts.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddDabblerReportFonts(fonts =>
    ///     fonts.Register("Noto Sans", "fonts/NotoSans-Regular.ttf", boldPath: "fonts/NotoSans-Bold.ttf"));
    /// </code>
    /// </example>
    public static IServiceCollection AddDabblerReportFonts(
        this IServiceCollection services,
        Action<ReportFonts> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var fonts = new ReportFonts();
        configure(fonts);
        return services.AddSingleton(fonts);
    }
}
