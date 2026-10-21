namespace QlParse;

// DECLARE name CURSOR [LOCAL|GLOBAL] [FORWARD_ONLY|SCROLL] [STATIC|KEYSET|DYNAMIC|FAST_FORWARD] ... FOR select.
// The ISO form (DECLARE name [INSENSITIVE] [SCROLL] CURSOR FOR ...) stays a DeclareCursorStatement.
public sealed class TsqlDeclareCursorStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required TsqlCursorDefinition Cursor { get; init; }
}
