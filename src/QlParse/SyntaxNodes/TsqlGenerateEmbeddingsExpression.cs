namespace QlParse;

// `AI_GENERATE_EMBEDDINGS(source USE MODEL model [PARAMETERS json])`.
public sealed class TsqlGenerateEmbeddingsExpression : Expression
{
    public required SyntaxToken FunctionName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken UseKeyword { get; init; }
    public required SyntaxToken ModelKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ModelName { get; init; }
    public SyntaxToken? ParametersKeyword { get; init; }
    public Expression? Parameters { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
