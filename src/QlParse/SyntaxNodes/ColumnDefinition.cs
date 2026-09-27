namespace QlParse;

public sealed class ColumnDefinition : TableElement
{
    public required SyntaxToken Name { get; init; }
    public DataType? Type { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OptionsKeyword { get; init; }
    public DefaultClause? Default { get; init; }
    public IdentityColumn? Identity { get; init; }
    public GeneratedColumn? Generated { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public IReadOnlyList<TableConstraint> Constraints { get; init; } = [];
}
