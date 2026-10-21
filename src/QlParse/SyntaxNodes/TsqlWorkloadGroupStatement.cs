namespace QlParse;

// `{CREATE | ALTER} WORKLOAD GROUP name [WITH (option, ...)] [USING pool [, EXTERNAL external_pool]]`.
public sealed class TsqlWorkloadGroupStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken WorkloadKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? Pool { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? ExternalPool { get; init; }
}
