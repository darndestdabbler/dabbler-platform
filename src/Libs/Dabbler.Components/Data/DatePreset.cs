// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data;

/// <summary>
/// Represents a date preset for quick filtering on date columns.
/// </summary>
public enum DatePreset
{
    /// <summary>Filters to today's date only.</summary>
    Today,
    /// <summary>Filters to the current week (Monday through Sunday).</summary>
    ThisWeek,
    /// <summary>Filters to the current calendar month.</summary>
    ThisMonth,
    /// <summary>Filters to the current calendar year.</summary>
    ThisYear,
}

/// <summary>
/// Provides date range calculation for <see cref="DatePreset"/> values.
/// </summary>
public static class DatePresetHelper
{
    /// <summary>
    /// Display labels for date presets.
    /// </summary>
    public static readonly IReadOnlyList<(DatePreset Preset, string Label)> All =
    [
        (DatePreset.Today, "Today"),
        (DatePreset.ThisWeek, "This Week"),
        (DatePreset.ThisMonth, "This Month"),
        (DatePreset.ThisYear, "This Year"),
    ];

    /// <summary>
    /// Gets the display label for a date preset.
    /// </summary>
    public static string GetLabel(DatePreset preset) => preset switch
    {
        DatePreset.Today => "Today",
        DatePreset.ThisWeek => "This Week",
        DatePreset.ThisMonth => "This Month",
        DatePreset.ThisYear => "This Year",
        _ => preset.ToString(),
    };

    /// <summary>
    /// Calculates the inclusive date range for a preset relative to the given date.
    /// </summary>
    /// <param name="preset">The date preset.</param>
    /// <param name="referenceDate">The reference date (defaults to today if null).</param>
    /// <returns>A tuple of (start, end) dates representing the inclusive range.</returns>
    public static (DateTime Start, DateTime End) GetRange(DatePreset preset, DateTime? referenceDate = null)
    {
        var today = referenceDate?.Date ?? DateTime.Today;

        return preset switch
        {
            DatePreset.Today => (today, today),
            DatePreset.ThisWeek => GetWeekRange(today),
            DatePreset.ThisMonth => (new DateTime(today.Year, today.Month, 1),
                                     new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
            DatePreset.ThisYear => (new DateTime(today.Year, 1, 1),
                                    new DateTime(today.Year, 12, 31)),
            _ => (today, today),
        };
    }

    private static (DateTime Start, DateTime End) GetWeekRange(DateTime date)
    {
        // Monday-based week
        var dayOfWeek = date.DayOfWeek;
        var daysFromMonday = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
        var monday = date.AddDays(-daysFromMonday);
        var sunday = monday.AddDays(6);
        return (monday, sunday);
    }
}
