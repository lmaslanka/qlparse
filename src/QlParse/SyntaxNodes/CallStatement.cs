namespace QlParse;

public sealed class CallStatement : Query
{
    public required SyntaxToken CallKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
