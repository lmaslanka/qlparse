namespace QlParse;

// OPEN, CLOSE or DEALLOCATE [GLOBAL] {cursor | @cursor_variable}.
public sealed class TsqlCursorStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public SyntaxToken? GlobalKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}
