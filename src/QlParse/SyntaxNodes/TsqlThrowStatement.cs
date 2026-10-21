namespace QlParse;

// THROW [error_number, message, state].
public sealed class TsqlThrowStatement : Query
{
    public required SyntaxToken ThrowKeyword { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
}
