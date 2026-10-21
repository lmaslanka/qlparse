namespace QlParse;

// EXEC[UTE] (string_expr [, argument ...]) [AS {LOGIN|USER} = 'name'] [AT linked_server | AT DATA_SOURCE name].
public sealed class TsqlExecuteStringStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Command { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? PrincipalKind { get; init; }
    public SyntaxToken? PrincipalEquals { get; init; }
    public SyntaxToken? Principal { get; init; }
    public SyntaxToken? AtKeyword { get; init; }
    public SyntaxToken? DataSourceKeyword { get; init; }
    public SyntaxToken? Target { get; init; }
}
