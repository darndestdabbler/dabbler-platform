namespace Dabbler.Components.Expressions;

/// <summary>
/// Base class for all expression AST nodes.
/// </summary>
public abstract class ExpressionNode
{
}

/// <summary>
/// A literal value (string, number, boolean, or null).
/// </summary>
public sealed class LiteralNode : ExpressionNode
{
    /// <summary>Gets the literal value.</summary>
    public object? Value { get; }

    /// <summary>Initializes a new <see cref="LiteralNode"/> with the specified value.</summary>
    /// <param name="value">The literal value (string, number, boolean, or <see langword="null"/>).</param>
    public LiteralNode(object? value) => Value = value;
}

/// <summary>
/// A field reference: <c>[FieldName]</c>.
/// </summary>
public sealed class FieldRefNode : ExpressionNode
{
    /// <summary>Gets the field name.</summary>
    public string FieldName { get; }

    /// <summary>Initializes a new <see cref="FieldRefNode"/> for the specified field.</summary>
    /// <param name="fieldName">The name of the field to reference.</param>
    public FieldRefNode(string fieldName) => FieldName = fieldName;
}

/// <summary>
/// A parameter reference: <c>{@ParamName}</c>.
/// </summary>
public sealed class ParamRefNode : ExpressionNode
{
    /// <summary>Gets the parameter name.</summary>
    public string ParamName { get; }

    /// <summary>Initializes a new <see cref="ParamRefNode"/> for the specified parameter.</summary>
    /// <param name="paramName">The name of the parameter to reference.</param>
    public ParamRefNode(string paramName) => ParamName = paramName;
}

/// <summary>
/// A named expression reference: <c>{=ExprName}</c>.
/// </summary>
public sealed class NamedExprRefNode : ExpressionNode
{
    /// <summary>Gets the expression name.</summary>
    public string ExpressionName { get; }

    /// <summary>Initializes a new <see cref="NamedExprRefNode"/> for the specified named expression.</summary>
    /// <param name="expressionName">The name of the expression to reference.</param>
    public NamedExprRefNode(string expressionName) => ExpressionName = expressionName;
}

/// <summary>
/// A built-in value reference: <c>{Today}</c>, <c>{Now}</c>, <c>{PageNumber}</c>, <c>{PageCount}</c>.
/// </summary>
public sealed class BuiltinRefNode : ExpressionNode
{
    /// <summary>Gets the built-in name.</summary>
    public string Name { get; }

    /// <summary>Initializes a new <see cref="BuiltinRefNode"/> for the specified built-in value.</summary>
    /// <param name="name">The built-in name (e.g., "Today", "Now", "PageNumber").</param>
    public BuiltinRefNode(string name) => Name = name;
}

/// <summary>
/// A binary operation: arithmetic, comparison, or string concatenation.
/// </summary>
public sealed class BinaryOpNode : ExpressionNode
{
    /// <summary>Gets the left operand.</summary>
    public ExpressionNode Left { get; }

    /// <summary>Gets the operator.</summary>
    public BinaryOperator Operator { get; }

    /// <summary>Gets the right operand.</summary>
    public ExpressionNode Right { get; }

    /// <summary>Initializes a new <see cref="BinaryOpNode"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="op">The binary operator.</param>
    /// <param name="right">The right operand.</param>
    public BinaryOpNode(ExpressionNode left, BinaryOperator op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

/// <summary>
/// A unary operation (negation).
/// </summary>
public sealed class UnaryOpNode : ExpressionNode
{
    /// <summary>Gets the operator.</summary>
    public UnaryOperator Operator { get; }

    /// <summary>Gets the operand.</summary>
    public ExpressionNode Operand { get; }

    /// <summary>Initializes a new <see cref="UnaryOpNode"/>.</summary>
    /// <param name="op">The unary operator.</param>
    /// <param name="operand">The operand.</param>
    public UnaryOpNode(UnaryOperator op, ExpressionNode operand)
    {
        Operator = op;
        Operand = operand;
    }
}

/// <summary>
/// A function call: <c>IIf(cond, trueVal, falseVal)</c>, <c>Sum([Field])</c>, etc.
/// </summary>
public sealed class FunctionCallNode : ExpressionNode
{
    /// <summary>Gets the function name.</summary>
    public string FunctionName { get; }

    /// <summary>Gets the argument list.</summary>
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    /// <summary>Initializes a new <see cref="FunctionCallNode"/>.</summary>
    /// <param name="functionName">The function name (e.g., "IIf", "Sum", "Left").</param>
    /// <param name="arguments">The argument nodes passed to the function.</param>
    public FunctionCallNode(string functionName, IReadOnlyList<ExpressionNode> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
}

/// <summary>
/// Represents <c>Count(*)</c> specifically.
/// </summary>
public sealed class CountStarNode : ExpressionNode
{
    /// <summary>Gets the optional scope qualifier.</summary>
    public string? Scope { get; }

    /// <summary>Initializes a new <see cref="CountStarNode"/>.</summary>
    /// <param name="scope">Optional scope qualifier for the count aggregate.</param>
    public CountStarNode(string? scope = null) => Scope = scope;
}

/// <summary>Binary operators supported in expressions.</summary>
public enum BinaryOperator
{
    /// <summary>Addition (<c>+</c>).</summary>
    Add,
    /// <summary>Subtraction (<c>-</c>).</summary>
    Subtract,
    /// <summary>Multiplication (<c>*</c>).</summary>
    Multiply,
    /// <summary>Division (<c>/</c>).</summary>
    Divide,
    /// <summary>Equality comparison (<c>=</c>).</summary>
    Equal,
    /// <summary>Inequality comparison (<c>&lt;&gt;</c>).</summary>
    NotEqual,
    /// <summary>Less-than comparison (<c>&lt;</c>).</summary>
    LessThan,
    /// <summary>Greater-than comparison (<c>&gt;</c>).</summary>
    GreaterThan,
    /// <summary>Less-than-or-equal comparison (<c>&lt;=</c>).</summary>
    LessOrEqual,
    /// <summary>Greater-than-or-equal comparison (<c>&gt;=</c>).</summary>
    GreaterOrEqual
}

/// <summary>Unary operators supported in expressions.</summary>
public enum UnaryOperator
{
    /// <summary>Arithmetic negation (<c>-</c>).</summary>
    Negate
}
