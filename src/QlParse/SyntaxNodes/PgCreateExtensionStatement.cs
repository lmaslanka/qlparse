namespace QlParse;

public sealed class PgCreateExtensionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ExtensionKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public SyntaxToken? Schema { get; init; }
    public SyntaxToken? VersionKeyword { get; init; }
    public SyntaxToken? Version { get; init; }
    public SyntaxToken? CascadeKeyword { get; init; }
}
