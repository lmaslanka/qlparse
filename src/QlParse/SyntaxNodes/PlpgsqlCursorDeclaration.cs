namespace QlParse;

public sealed class PlpgsqlCursorDeclaration : Query
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ScrollKeyword { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required Query Query { get; init; }
}
