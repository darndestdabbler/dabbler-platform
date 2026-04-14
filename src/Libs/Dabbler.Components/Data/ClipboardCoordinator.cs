// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.Globalization;
using Dabbler.Components.Utilities;

namespace Dabbler.Components.Data;

/// <summary>
/// Manages clipboard operations (copy, cut, paste) for the data grid.
/// Pure C# — no framework dependencies. All platform interaction (actual clipboard
/// read/write) is handled by the caller via JS interop.
/// </summary>
public static class ClipboardCoordinator
{
    /// <summary>
    /// Gets the display value for a single cell, respecting computed columns and format strings.
    /// </summary>
    public static string GetCellDisplayValue(object item, GridColumnInfo column)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(column);

        object? value;

        if (column.ComputeValue is not null)
        {
            value = column.ComputeValue(item);
        }
        else if (column.PropertyName is not null)
        {
            try
            {
                value = PropertyAccessor.GetValue(item, column.PropertyName);
            }
            catch
            {
                return "";
            }
        }
        else
        {
            return "";
        }

        if (value is null) return "";

        if (column.Format is not null)
            return string.Format(CultureInfo.CurrentCulture, $"{{0:{column.Format}}}", value);

        return value.ToString() ?? "";
    }

    /// <summary>
    /// Gets the raw value of a single cell as a string for clipboard copy.
    /// Uses <see cref="GetCellDisplayValue"/> for formatting.
    /// </summary>
    public static string GetCellValueAsString(object item, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            var value = PropertyAccessor.GetValue(item, propertyName);
            return value?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Clears a cell value to its type default (null for reference types, default(T) for value types).
    /// </summary>
    /// <returns><c>true</c> if the value was cleared; <c>false</c> if the property was not found or not writable.</returns>
    public static bool ClearCellValue(object item, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(item);

        var prop = PropertyAccessor.GetWritableProperty(item.GetType(), propertyName);
        if (prop is null) return false;

        var defaultValue = prop.PropertyType.IsValueType
            ? Activator.CreateInstance(prop.PropertyType)
            : null;

        PropertyAccessor.SetValue(item, propertyName, defaultValue);
        return true;
    }

    /// <summary>
    /// Converts clipboard text to the target property type and sets it on the item.
    /// </summary>
    /// <returns><c>true</c> if the value was set; <c>false</c> if conversion failed or property not found.</returns>
    public static bool PasteCellValue(object item, string propertyName, string text)
    {
        ArgumentNullException.ThrowIfNull(item);

        var prop = PropertyAccessor.GetWritableProperty(item.GetType(), propertyName);
        if (prop is null) return false;

        try
        {
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            object? convertedValue;
            if (string.IsNullOrEmpty(text) && Nullable.GetUnderlyingType(prop.PropertyType) is not null)
            {
                convertedValue = null;
            }
            else
            {
                convertedValue = Convert.ChangeType(text, targetType, CultureInfo.CurrentCulture);
            }

            PropertyAccessor.SetValue(item, propertyName, convertedValue);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }
}
