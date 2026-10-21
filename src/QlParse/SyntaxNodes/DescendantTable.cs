namespace QlParse;

// Postgres `name *`: a table reference that explicitly includes inheriting child tables.
public sealed class DescendantTable : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public required SyntaxToken Star { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
