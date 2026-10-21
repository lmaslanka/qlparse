namespace QlParse;

// A FROM-clause rowset function whose arguments are not a plain expression list: OPENJSON, OPENXML, OPENROWSET,
// OPENQUERY, OPENDATASOURCE(...).db.schema.object, CONTAINSTABLE, FREETEXTTABLE, SEMANTIC*TABLE, VECTOR_SEARCH,
// CHANGETABLE.
// Separators keeps the `,` or `;` between arguments; ObjectName is the `.db.schema.object` after OPENDATASOURCE.
public sealed class TsqlRowsetFunctionTable : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlRowsetArgument> Arguments { get; init; }
    public IReadOnlyList<SyntaxToken> Separators { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<SyntaxToken> ObjectName { get; init; } = [];
    public TsqlRowsetSchema? Schema { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
