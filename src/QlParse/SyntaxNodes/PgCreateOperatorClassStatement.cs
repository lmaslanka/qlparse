namespace QlParse;

public sealed class PgCreateOperatorClassStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken OperatorKeyword { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public DataType? Type { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken AccessMethod { get; init; }
    public SyntaxToken? FamilyKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Family { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public IReadOnlyList<PgOperatorClassItem> Items { get; init; } = [];
}
