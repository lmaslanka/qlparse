namespace QlParse;

// CREATE OR ALTER VIEW ...: View is the ordinary view definition parsed from the ALTER keyword on, so its
// CreateKeyword holds that ALTER token.
public sealed class TsqlCreateOrAlterViewStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken OrKeyword { get; init; }
    public required CreateViewStatement View { get; init; }
}
