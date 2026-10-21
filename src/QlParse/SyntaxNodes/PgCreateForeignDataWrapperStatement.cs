namespace QlParse;

public sealed class PgCreateForeignDataWrapperStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ForeignKeyword { get; init; }
    public required SyntaxToken DataKeyword { get; init; }
    public required SyntaxToken WrapperKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<PgHandlerClause> Handlers { get; init; } = [];
    public PgOptionList? Options { get; init; }
}
