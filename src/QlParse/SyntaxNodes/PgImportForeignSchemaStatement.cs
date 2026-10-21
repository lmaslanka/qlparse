namespace QlParse;

public sealed class PgImportForeignSchemaStatement : Query
{
    public required SyntaxToken ImportKeyword { get; init; }
    public required SyntaxToken ForeignKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required SyntaxToken RemoteSchema { get; init; }
    public SyntaxToken? FilterKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Tables { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken ServerKeyword { get; init; }
    public required SyntaxToken Server { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required SyntaxToken LocalSchema { get; init; }
    public PgOptionList? Options { get; init; }
}
