namespace Dabbler.Components.Expressions;

/// <summary>
/// A token produced by the expression lexer.
/// </summary>
internal readonly struct ExpressionToken
{
    public ExpressionTokenType Type { get; }
    public string Value { get; }
    public int Position { get; }

    public ExpressionToken(ExpressionTokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString() => $"{Type}({Value}) @{Position}";
}

/// <summary>
/// Token types for the expression lexer.
/// </summary>
internal enum ExpressionTokenType
{
    /// <summary>A numeric literal (integer or decimal).</summary>
    Number,

    /// <summary>A string literal enclosed in double quotes.</summary>
    String,

    /// <summary>The <c>true</c> keyword.</summary>
    True,

    /// <summary>The <c>false</c> keyword.</summary>
    False,

    /// <summary>A field reference: <c>[FieldName]</c>.</summary>
    FieldRef,

    /// <summary>A parameter reference: <c>{@ParamName}</c>.</summary>
    ParamRef,

    /// <summary>A named expression reference: <c>{=ExprName}</c>.</summary>
    NamedExprRef,

    /// <summary>A built-in reference: <c>{Today}</c>, <c>{Now}</c>, etc.</summary>
    BuiltinRef,

    /// <summary>An identifier (function name like IIf, Sum, Count, etc.).</summary>
    Identifier,

    /// <summary>The <c>+</c> operator.</summary>
    Plus,

    /// <summary>The <c>-</c> operator.</summary>
    Minus,

    /// <summary>The <c>*</c> operator.</summary>
    Star,

    /// <summary>The <c>/</c> operator.</summary>
    Slash,

    /// <summary>The <c>=</c> operator.</summary>
    Equals,

    /// <summary>The <c>&lt;&gt;</c> operator.</summary>
    NotEquals,

    /// <summary>The <c>&lt;</c> operator.</summary>
    LessThan,

    /// <summary>The <c>&gt;</c> operator.</summary>
    GreaterThan,

    /// <summary>The <c>&lt;=</c> operator.</summary>
    LessOrEqual,

    /// <summary>The <c>&gt;=</c> operator.</summary>
    GreaterOrEqual,

    /// <summary>A left parenthesis.</summary>
    LeftParen,

    /// <summary>A right parenthesis.</summary>
    RightParen,

    /// <summary>A comma separator.</summary>
    Comma,

    /// <summary>End of expression.</summary>
    End
}
