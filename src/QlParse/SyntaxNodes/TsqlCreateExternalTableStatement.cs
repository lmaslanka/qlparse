namespace QlParse;

// CREATE EXTERNAL TABLE name [(column, ...)] WITH (option, ...) [AS select] — the AS form is CETAS.
public sealed class TsqlCreateExternalTableStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ExternalKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TableElement> Elements { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required TsqlObjectOptionList Options { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public Query? Query { get; init; }
}
