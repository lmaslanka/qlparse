namespace QlParse;

// IF condition statement [ELSE statement]; each branch is one statement, usually a BEGIN ... END block.
public sealed class TsqlIfStatement : Query
{
    public required SyntaxToken IfKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required Query Then { get; init; }
    public SyntaxToken? ElseKeyword { get; init; }
    public Query? Else { get; init; }
}
