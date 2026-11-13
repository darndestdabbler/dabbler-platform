using System.Text;

namespace Dabbler.Reports.Core.Expressions;

/// <summary>
/// Tokenizes report expression strings into a sequence of <see cref="ExpressionToken"/> values.
/// </summary>
internal sealed class ExpressionLexer
{
    private readonly string _input;
    private int _pos;

    private static readonly HashSet<string> s_builtinNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Today", "Now", "PageNumber", "PageCount"
    };

    public ExpressionLexer(string input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _pos = 0;
    }

    /// <summary>
    /// Tokenizes the entire input and returns the token list (including the final End token).
    /// </summary>
    public List<ExpressionToken> Tokenize()
    {
        var tokens = new List<ExpressionToken>();

        while (true)
        {
            var token = NextToken();
            tokens.Add(token);
            if (token.Type == ExpressionTokenType.End)
                break;
        }

        return tokens;
    }

    private ExpressionToken NextToken()
    {
        SkipWhitespace();

        if (_pos >= _input.Length)
            return new ExpressionToken(ExpressionTokenType.End, "", _pos);

        int start = _pos;
        char c = _input[_pos];

        // String literal
        if (c == '"')
            return ReadStringLiteral();

        // Field reference [FieldName]
        if (c == '[')
            return ReadBracketedRef();

        // Brace references: {@Param}, {=Expr}, {Today}, {Now}, {PageNumber}, {PageCount}
        if (c == '{')
            return ReadBraceRef();

        // Number literal
        if (char.IsDigit(c) || (c == '.' && _pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1])))
            return ReadNumber();

        // Identifier (function names, true, false)
        if (char.IsLetter(c) || c == '_')
            return ReadIdentifier();

        // Operators and punctuation
        return ReadOperator();
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            _pos++;
    }

    private ExpressionToken ReadStringLiteral()
    {
        int start = _pos;
        _pos++; // skip opening quote
        var sb = new StringBuilder();

        while (_pos < _input.Length)
        {
            char c = _input[_pos];
            if (c == '"')
            {
                // Check for escaped quote ""
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '"')
                {
                    sb.Append('"');
                    _pos += 2;
                }
                else
                {
                    _pos++; // skip closing quote
                    return new ExpressionToken(ExpressionTokenType.String, sb.ToString(), start);
                }
            }
            else
            {
                sb.Append(c);
                _pos++;
            }
        }

        throw new ExpressionEvaluationException(
            $"Unterminated string literal at position {start}.");
    }

    private ExpressionToken ReadBracketedRef()
    {
        int start = _pos;
        _pos++; // skip [
        int nameStart = _pos;

        while (_pos < _input.Length && _input[_pos] != ']')
            _pos++;

        if (_pos >= _input.Length)
            throw new ExpressionEvaluationException(
                $"Unterminated field reference at position {start}.");

        string name = _input[nameStart.._pos];
        _pos++; // skip ]

        return new ExpressionToken(ExpressionTokenType.FieldRef, name, start);
    }

    private ExpressionToken ReadBraceRef()
    {
        int start = _pos;
        _pos++; // skip {

        if (_pos >= _input.Length)
            throw new ExpressionEvaluationException(
                $"Unterminated brace reference at position {start}.");

        char next = _input[_pos];

        if (next == '@')
        {
            // Parameter reference {@ParamName}
            _pos++; // skip @
            int nameStart = _pos;
            while (_pos < _input.Length && _input[_pos] != '}')
                _pos++;

            if (_pos >= _input.Length)
                throw new ExpressionEvaluationException(
                    $"Unterminated parameter reference at position {start}.");

            string name = _input[nameStart.._pos];
            _pos++; // skip }
            return new ExpressionToken(ExpressionTokenType.ParamRef, name, start);
        }

        if (next == '=')
        {
            // Named expression reference {=ExprName}
            _pos++; // skip =
            int nameStart = _pos;
            while (_pos < _input.Length && _input[_pos] != '}')
                _pos++;

            if (_pos >= _input.Length)
                throw new ExpressionEvaluationException(
                    $"Unterminated named expression reference at position {start}.");

            string name = _input[nameStart.._pos];
            _pos++; // skip }
            return new ExpressionToken(ExpressionTokenType.NamedExprRef, name, start);
        }

        // Built-in reference {Today}, {Now}, {PageNumber}, {PageCount}
        int builtinStart = _pos;
        while (_pos < _input.Length && _input[_pos] != '}')
            _pos++;

        if (_pos >= _input.Length)
            throw new ExpressionEvaluationException(
                $"Unterminated brace reference at position {start}.");

        string builtinName = _input[builtinStart.._pos];
        _pos++; // skip }

        if (!s_builtinNames.Contains(builtinName))
            throw new ExpressionEvaluationException(
                $"Unknown built-in reference '{{{builtinName}}}' at position {start}.");

        return new ExpressionToken(ExpressionTokenType.BuiltinRef, builtinName, start);
    }

    private ExpressionToken ReadNumber()
    {
        int start = _pos;
        bool hasDot = false;

        while (_pos < _input.Length)
        {
            char c = _input[_pos];
            if (char.IsDigit(c))
            {
                _pos++;
            }
            else if (c == '.' && !hasDot)
            {
                hasDot = true;
                _pos++;
            }
            else
            {
                break;
            }
        }

        return new ExpressionToken(ExpressionTokenType.Number, _input[start.._pos], start);
    }

    private ExpressionToken ReadIdentifier()
    {
        int start = _pos;

        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
            _pos++;

        string word = _input[start.._pos];

        if (word.Equals("true", StringComparison.OrdinalIgnoreCase))
            return new ExpressionToken(ExpressionTokenType.True, word, start);

        if (word.Equals("false", StringComparison.OrdinalIgnoreCase))
            return new ExpressionToken(ExpressionTokenType.False, word, start);

        return new ExpressionToken(ExpressionTokenType.Identifier, word, start);
    }

    private ExpressionToken ReadOperator()
    {
        int start = _pos;
        char c = _input[_pos];

        switch (c)
        {
            case '+':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Plus, "+", start);
            case '-':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Minus, "-", start);
            case '*':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Star, "*", start);
            case '/':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Slash, "/", start);
            case '(':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.LeftParen, "(", start);
            case ')':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.RightParen, ")", start);
            case ',':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Comma, ",", start);
            case '=':
                _pos++;
                return new ExpressionToken(ExpressionTokenType.Equals, "=", start);
            case '<':
                _pos++;
                if (_pos < _input.Length)
                {
                    if (_input[_pos] == '>')
                    {
                        _pos++;
                        return new ExpressionToken(ExpressionTokenType.NotEquals, "<>", start);
                    }
                    if (_input[_pos] == '=')
                    {
                        _pos++;
                        return new ExpressionToken(ExpressionTokenType.LessOrEqual, "<=", start);
                    }
                }
                return new ExpressionToken(ExpressionTokenType.LessThan, "<", start);
            case '>':
                _pos++;
                if (_pos < _input.Length && _input[_pos] == '=')
                {
                    _pos++;
                    return new ExpressionToken(ExpressionTokenType.GreaterOrEqual, ">=", start);
                }
                return new ExpressionToken(ExpressionTokenType.GreaterThan, ">", start);
            default:
                throw new ExpressionEvaluationException(
                    $"Unexpected character '{c}' at position {start}.");
        }
    }
}
