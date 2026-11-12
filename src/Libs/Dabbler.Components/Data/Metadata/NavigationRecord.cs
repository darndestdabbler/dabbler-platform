// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components.Data.Metadata;

/// <summary>
/// A record pill entry returned by <see cref="INavigationDataProvider"/>,
/// projected as a primary key value and display label pair.
/// </summary>
/// <param name="PrimaryKeyValue">The primary key value for the record.</param>
/// <param name="RecordLabel">The display label for the record (evaluated from the entity's RecordLabelExpression).</param>
public record NavigationRecord(object PrimaryKeyValue, string RecordLabel);
