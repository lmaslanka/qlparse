namespace QlParse;

// SQL Server CREATE TYPE: an alias type (`FROM base_type [NULL|NOT NULL]`), a table type
// (`AS TABLE (elements) [WITH (options)]`) or a CLR type (`EXTERNAL NAME assembly.class`).
public sealed class TsqlCreateTypeStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public DataType? BaseType { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? TableKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TableElement>? Elements { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? NameKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ExternalName { get; init; }
}
