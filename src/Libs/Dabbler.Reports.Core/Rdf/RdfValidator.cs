namespace Dabbler.Reports.Core.Rdf;

/// <summary>
/// Performs semantic validation on a deserialized <see cref="ReportDefinition"/>.
/// Checks cross-reference integrity, uniqueness constraints, singleton band rules,
/// and element bounds.
/// </summary>
internal static class RdfValidator
{
    /// <summary>
    /// Validates the given definition and returns a list of errors (empty if valid).
    /// </summary>
    internal static List<RdfValidationError> Validate(ReportDefinition definition)
    {
        var errors = new List<RdfValidationError>();

        ValidateMetadata(definition.Metadata, errors);
        ValidateDataSources(definition.DataSources, errors);
        ValidateFields(definition.Fields, errors);
        ValidateParameters(definition.Parameters, errors);
        ValidateSorting(definition.Sorting, definition.Fields, errors);
        ValidateGrouping(definition.Grouping, definition.Bands, errors);
        ValidateBands(definition.Bands, definition.Styles, definition.Fields, definition.Metadata, errors);
        ValidateStyles(definition.Styles, errors);
        ValidateExpressions(definition.Expressions, errors);

        return errors;
    }

    private static void ValidateMetadata(ReportMetadata metadata, List<RdfValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(metadata.Name))
            errors.Add(new("Metadata.Name", "Report name is required."));

        if (metadata.PageWidth <= 0)
            errors.Add(new("Metadata.PageWidth", "Page width must be greater than zero."));

        if (metadata.PageHeight <= 0)
            errors.Add(new("Metadata.PageHeight", "Page height must be greater than zero."));

        if (metadata.MarginLeft + metadata.MarginRight >= metadata.PageWidth)
            errors.Add(new("Metadata.Margins", "Left and right margins exceed page width."));

        if (metadata.MarginTop + metadata.MarginBottom >= metadata.PageHeight)
            errors.Add(new("Metadata.Margins", "Top and bottom margins exceed page height."));
    }

    private static void ValidateDataSources(List<DataSourceDefinition> dataSources, List<RdfValidationError> errors)
    {
        if (dataSources.Count == 0)
        {
            errors.Add(new("DataSources", "At least one data source is required."));
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < dataSources.Count; i++)
        {
            var ds = dataSources[i];
            var path = $"DataSources[{i}]";

            if (string.IsNullOrWhiteSpace(ds.Name))
                errors.Add(new(path + ".Name", "Data source name is required."));
            else if (!names.Add(ds.Name))
                errors.Add(new(path + ".Name", $"Duplicate data source name '{ds.Name}'."));

            if (string.IsNullOrWhiteSpace(ds.Query))
                errors.Add(new(path + ".Query", "Data source query is required."));
        }
    }

    private static void ValidateFields(List<FieldDefinition> fields, List<RdfValidationError> errors)
    {
        if (fields.Count == 0)
        {
            errors.Add(new("Fields", "At least one field is required."));
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var path = $"Fields[{i}]";

            if (string.IsNullOrWhiteSpace(field.Name))
                errors.Add(new(path + ".Name", "Field name is required."));
            else if (!names.Add(field.Name))
                errors.Add(new(path + ".Name", $"Duplicate field name '{field.Name}'."));

            if (string.IsNullOrWhiteSpace(field.DataType))
                errors.Add(new(path + ".DataType", "Field data type is required."));
        }
    }

    private static void ValidateParameters(List<ParameterDefinition> parameters, List<RdfValidationError> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var path = $"Parameters[{i}]";

            if (string.IsNullOrWhiteSpace(param.Name))
                errors.Add(new(path + ".Name", "Parameter name is required."));
            else if (!names.Add(param.Name))
                errors.Add(new(path + ".Name", $"Duplicate parameter name '{param.Name}'."));

            if (string.IsNullOrWhiteSpace(param.DataType))
                errors.Add(new(path + ".DataType", "Parameter data type is required."));

            // Lookup fields required for ComboBox/MultiSelectList
            if (param.ControlType is ParameterControlType.ComboBox or ParameterControlType.MultiSelectList)
            {
                if (string.IsNullOrWhiteSpace(param.LookupQuery))
                    errors.Add(new(path + ".LookupQuery", $"Lookup query is required for {param.ControlType} parameters."));
            }
        }
    }

    private static void ValidateSorting(List<SortDefinition> sorting, List<FieldDefinition> fields, List<RdfValidationError> errors)
    {
        var fieldNames = new HashSet<string>(fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < sorting.Count; i++)
        {
            var sort = sorting[i];
            var path = $"Sorting[{i}]";

            if (string.IsNullOrWhiteSpace(sort.FieldName))
                errors.Add(new(path + ".FieldName", "Sort field name is required."));
            else if (!fieldNames.Contains(sort.FieldName))
                errors.Add(new(path + ".FieldName", $"Sort references unknown field '{sort.FieldName}'."));
        }
    }

    private static void ValidateGrouping(List<GroupDefinition> grouping, List<BandDefinition> bands, List<RdfValidationError> errors)
    {
        var bandNames = new HashSet<string>(bands.Select(b => b.Name), StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < grouping.Count; i++)
        {
            var group = grouping[i];
            var path = $"Grouping[{i}]";

            if (string.IsNullOrWhiteSpace(group.FieldName) && string.IsNullOrWhiteSpace(group.GroupExpression))
                errors.Add(new(path, "Group must have either fieldName or groupExpression."));

            if (!string.IsNullOrWhiteSpace(group.HeaderBandName) && !bandNames.Contains(group.HeaderBandName))
                errors.Add(new(path + ".HeaderBandName", $"Group header references unknown band '{group.HeaderBandName}'."));

            if (!string.IsNullOrWhiteSpace(group.FooterBandName) && !bandNames.Contains(group.FooterBandName))
                errors.Add(new(path + ".FooterBandName", $"Group footer references unknown band '{group.FooterBandName}'."));
        }
    }

    private static void ValidateBands(
        List<BandDefinition> bands,
        List<StyleDefinition> styles,
        List<FieldDefinition> fields,
        ReportMetadata metadata,
        List<RdfValidationError> errors)
    {
        if (bands.Count == 0)
        {
            errors.Add(new("Bands", "At least one band is required."));
            return;
        }

        var bandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var styleNames = new HashSet<string>(styles.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var fieldNames = new HashSet<string>(fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

        // Content width = page width minus left and right margins
        double contentWidth = metadata.PageWidth - metadata.MarginLeft - metadata.MarginRight;

        // Track singleton band counts
        int reportHeaderCount = 0;
        int reportFooterCount = 0;
        int noDataCount = 0;

        for (int i = 0; i < bands.Count; i++)
        {
            var band = bands[i];
            var path = $"Bands[{i}]";

            if (string.IsNullOrWhiteSpace(band.Name))
                errors.Add(new(path + ".Name", "Band name is required."));
            else if (!bandNames.Add(band.Name))
                errors.Add(new(path + ".Name", $"Duplicate band name '{band.Name}'."));

            if (band.Height < 0)
                errors.Add(new(path + ".Height", "Band height must not be negative."));

            // Singleton constraints
            switch (band.Type)
            {
                case BandType.ReportHeader:
                    if (++reportHeaderCount > 1)
                        errors.Add(new(path, "Only one ReportHeader band is allowed."));
                    break;
                case BandType.ReportFooter:
                    if (++reportFooterCount > 1)
                        errors.Add(new(path, "Only one ReportFooter band is allowed."));
                    break;
                case BandType.NoData:
                    if (++noDataCount > 1)
                        errors.Add(new(path, "Only one NoData band is allowed."));
                    break;
            }

            // Group bands must reference a group name
            if (band.Type is BandType.GroupHeader or BandType.GroupFooter && string.IsNullOrWhiteSpace(band.GroupName))
                errors.Add(new(path + ".GroupName", $"GroupName is required for {band.Type} bands."));

            // Validate elements within the band
            for (int j = 0; j < band.Elements.Count; j++)
            {
                ValidateElement(band.Elements[j], $"{path}.Elements[{j}]", styleNames, fieldNames, contentWidth, band.Height, band.Type, errors);
            }
        }
    }

    private static void ValidateElement(
        ElementDefinition element,
        string path,
        HashSet<string> styleNames,
        HashSet<string> fieldNames,
        double contentWidth,
        double bandHeight,
        BandType bandType,
        List<RdfValidationError> errors)
    {
        // Style reference check
        if (!string.IsNullOrWhiteSpace(element.StyleName) && !styleNames.Contains(element.StyleName))
            errors.Add(new(path + ".StyleName", $"Element references unknown style '{element.StyleName}'."));

        // Field reference check
        if (!string.IsNullOrWhiteSpace(element.FieldName) && !fieldNames.Contains(element.FieldName))
            errors.Add(new(path + ".FieldName", $"Element references unknown field '{element.FieldName}'."));

        // Dimension checks
        if (element.Width < 0)
            errors.Add(new(path + ".Width", "Element width must not be negative."));

        if (element.Height < 0)
            errors.Add(new(path + ".Height", "Element height must not be negative."));

        // Bounds checking: element must fit within the content area
        if (contentWidth > 0 && element.X + element.Width > contentWidth + 0.001)
            errors.Add(new(path, $"Element exceeds content width ({element.X} + {element.Width} = {element.X + element.Width:F3} > {contentWidth:F3})."));

        if (bandHeight > 0 && element.Y + element.Height > bandHeight + 0.001)
            errors.Add(new(path, $"Element exceeds band height ({element.Y} + {element.Height} = {element.Y + element.Height:F3} > {bandHeight:F3})."));

        // Type-specific checks
        switch (element.Type)
        {
            case ElementType.StaticText:
                if (string.IsNullOrWhiteSpace(element.Value) && string.IsNullOrWhiteSpace(element.Expression))
                    errors.Add(new(path, "StaticText element requires a value or expression."));
                break;

            case ElementType.TextField:
                if (string.IsNullOrWhiteSpace(element.FieldName) && string.IsNullOrWhiteSpace(element.Expression))
                    errors.Add(new(path, "TextField element requires a fieldName or expression."));
                break;

            case ElementType.Subreport:
                if (string.IsNullOrWhiteSpace(element.SubreportPath))
                    errors.Add(new(path + ".SubreportPath", "Subreport element requires a subreportPath."));
                break;
        }

        // RunningTotal validation
        if (element.RunningTotal != RunningTotalMode.None)
        {
            if (element.Type != ElementType.TextField)
                errors.Add(new(path, "RunningTotal requires a TextField with a fieldName or expression."));
            else if (string.IsNullOrWhiteSpace(element.FieldName) && string.IsNullOrWhiteSpace(element.Expression))
                errors.Add(new(path, "RunningTotal requires a TextField with a fieldName or expression."));

            if (bandType != BandType.Detail)
                errors.Add(new(path, "RunningTotal is only valid on elements in the Detail band."));
        }

        // HideDuplicates validation
        if (element.HideDuplicates)
        {
            if (element.Type != ElementType.TextField)
                errors.Add(new(path, "HideDuplicates requires a TextField with a fieldName or expression."));
            else if (string.IsNullOrWhiteSpace(element.FieldName) && string.IsNullOrWhiteSpace(element.Expression))
                errors.Add(new(path, "HideDuplicates requires a TextField with a fieldName or expression."));

            if (bandType != BandType.Detail)
                errors.Add(new(path, "HideDuplicates is only valid on elements in the Detail band."));
        }
    }

    private static void ValidateStyles(List<StyleDefinition> styles, List<RdfValidationError> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < styles.Count; i++)
        {
            var style = styles[i];
            var path = $"Styles[{i}]";

            if (string.IsNullOrWhiteSpace(style.Name))
                errors.Add(new(path + ".Name", "Style name is required."));
            else if (!names.Add(style.Name))
                errors.Add(new(path + ".Name", $"Duplicate style name '{style.Name}'."));

            if (style.FontSize.HasValue && style.FontSize.Value <= 0)
                errors.Add(new(path + ".FontSize", "Font size must be greater than zero."));
        }
    }

    private static void ValidateExpressions(List<ExpressionDefinition> expressions, List<RdfValidationError> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < expressions.Count; i++)
        {
            var expr = expressions[i];
            var path = $"Expressions[{i}]";

            if (string.IsNullOrWhiteSpace(expr.Name))
                errors.Add(new(path + ".Name", "Expression name is required."));
            else if (!names.Add(expr.Name))
                errors.Add(new(path + ".Name", $"Duplicate expression name '{expr.Name}'."));

            if (string.IsNullOrWhiteSpace(expr.Expression))
                errors.Add(new(path + ".Expression", "Expression text is required."));
        }
    }
}
