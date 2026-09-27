namespace QlParse;

public sealed class AlterColumnAction : AlterTableAction
{
    public required SyntaxToken AlterKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public SyntaxToken? DropKeyword { get; init; }
    public SyntaxToken? AddKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public DataType? DataType { get; init; }
    public DefaultClause? Default { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public SyntaxToken? Behavior { get; init; }
    public SyntaxToken? RestartKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public Expression? RestartValue { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? IdentityKeyword { get; init; }
    public SequenceOption? IdentityOption { get; init; }
}
