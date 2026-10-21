namespace QlParse;

// `ALTER SEARCH PROPERTY LIST name {ADD 'property' WITH (option, ...) | DROP 'property'}`.
public sealed class TsqlAlterSearchPropertyListStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SearchKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken ListKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Action { get; init; }
    public required SyntaxToken Property { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
