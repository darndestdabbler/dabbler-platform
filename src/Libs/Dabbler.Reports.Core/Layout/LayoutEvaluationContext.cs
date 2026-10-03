using System.Data;
using Dabbler.Components.Expressions;
using Dabbler.Reports.Core.Rdf;

namespace Dabbler.Reports.Core.Layout;

/// <summary>
/// Provides an <see cref="IEvaluationContext"/> implementation for the band layout engine.
/// Resolves fields from a <see cref="DataRow"/>, parameters from a dictionary,
/// named expressions from the <see cref="ReportDefinition"/>, built-in page values,
/// and aggregates over row subsets.
/// </summary>
internal sealed class LayoutEvaluationContext : IEvaluationContext
{
    private readonly ReportDefinition _report;
    private readonly Dictionary<string, object?> _parameters;
    private readonly Dictionary<string, string> _namedExpressions;
    private DataRow? _currentRow;
    private int _pageNumber;
    private int _pageCount;

    // Group-level and report-level row sets for aggregate computation.
    // Key = scope name (group field name or "report"), Value = rows in that scope.
    private readonly Dictionary<string, DataRow[]> _aggregateScopes = new(StringComparer.OrdinalIgnoreCase);
    private DataRow[]? _reportRows;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutEvaluationContext"/> class.
    /// </summary>
    /// <param name="report">The report definition.</param>
    /// <param name="parameters">The parameter name-to-value map.</param>
    public LayoutEvaluationContext(ReportDefinition report, Dictionary<string, object?>? parameters)
    {
        ArgumentNullException.ThrowIfNull(report);
        _report = report;
        _parameters = parameters ?? [];

        _namedExpressions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var expr in report.Expressions)
        {
            _namedExpressions[expr.Name] = expr.Expression;
        }
    }

    /// <summary>Sets the current data row for field resolution.</summary>
    public void SetCurrentRow(DataRow? row) => _currentRow = row;

    /// <summary>Sets the current page number.</summary>
    public void SetPageNumber(int pageNumber) => _pageNumber = pageNumber;

    /// <summary>Sets the total page count (backfilled after layout).</summary>
    public void SetPageCount(int pageCount) => _pageCount = pageCount;

    /// <summary>Sets the report-level row set for aggregates.</summary>
    public void SetReportRows(DataRow[] rows) => _reportRows = rows;

    /// <summary>Sets or replaces an aggregate scope (e.g., group rows).</summary>
    public void SetAggregateScope(string scopeName, DataRow[] rows)
    {
        _aggregateScopes[scopeName] = rows;
    }

    /// <summary>Clears all aggregate scopes.</summary>
    public void ClearAggregateScopes() => _aggregateScopes.Clear();

    /// <inheritdoc />
    public object? GetFieldValue(string fieldName)
    {
        if (_currentRow is null)
            throw new ExpressionEvaluationException($"No current row available for field '{fieldName}'.");

        // Try column name directly
        if (_currentRow.Table.Columns.Contains(fieldName))
        {
            var value = _currentRow[fieldName];
            return value == DBNull.Value ? null : value;
        }

        // Try matching via FieldDefinition.SourceColumn
        var field = _report.Fields.FirstOrDefault(
            f => string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (field is not null && _currentRow.Table.Columns.Contains(field.SourceColumn))
        {
            var value = _currentRow[field.SourceColumn];
            return value == DBNull.Value ? null : value;
        }

        throw new ExpressionEvaluationException($"Field '{fieldName}' not found in data.");
    }

    /// <inheritdoc />
    public object? GetParameterValue(string paramName)
    {
        if (_parameters.TryGetValue(paramName, out var value))
            return value;

        throw new ExpressionEvaluationException($"Parameter '{paramName}' not found.");
    }

    /// <inheritdoc />
    public string GetNamedExpression(string expressionName)
    {
        if (_namedExpressions.TryGetValue(expressionName, out var expr))
            return expr;

        throw new ExpressionEvaluationException($"Named expression '{expressionName}' not found.");
    }

    /// <inheritdoc />
    public object? GetBuiltinValue(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "TODAY" => DateTime.Today,
            "NOW" => DateTime.Now,
            "PAGENUMBER" => _pageNumber,
            "PAGECOUNT" => _pageCount,
            _ => throw new ExpressionEvaluationException($"Unknown built-in '{name}'.")
        };
    }

    /// <inheritdoc />
    public object? ComputeAggregate(AggregateFunction function, string? expression, string? scope)
    {
        var rows = ResolveScope(scope);
        if (rows.Length == 0)
            return function == AggregateFunction.Count ? 0 : null;

        if (function == AggregateFunction.Count && expression is null)
            return rows.Length;

        // Evaluate the inner expression for each row, collecting non-null numeric values.
        var engine = new ExpressionEngine(this);
        var savedRow = _currentRow;
        try
        {
            var values = new List<double>(rows.Length);
            foreach (var row in rows)
            {
                _currentRow = row;
                var result = engine.Evaluate(expression!);
                if (result is not null)
                {
                    values.Add(Convert.ToDouble(result));
                }
            }

            if (values.Count == 0)
                return function == AggregateFunction.Count ? 0 : null;

            return function switch
            {
                AggregateFunction.Sum => values.Sum(),
                AggregateFunction.Count => values.Count,
                AggregateFunction.Avg => values.Average(),
                AggregateFunction.Min => values.Min(),
                AggregateFunction.Max => values.Max(),
                _ => throw new ExpressionEvaluationException($"Unknown aggregate function '{function}'.")
            };
        }
        finally
        {
            _currentRow = savedRow;
        }
    }

    private DataRow[] ResolveScope(string? scope)
    {
        if (string.IsNullOrEmpty(scope) || string.Equals(scope, "report", StringComparison.OrdinalIgnoreCase))
        {
            return _reportRows ?? [];
        }

        if (_aggregateScopes.TryGetValue(scope, out var rows))
            return rows;

        // Fall back to report-level if scope not found
        return _reportRows ?? [];
    }
}
