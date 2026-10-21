namespace QlParse;

// EXEC[UTE] [@status =] {module | @module_variable} [;number] [argument, ...] [WITH option, ...].
// Module holds the name parts, or the single @variable token naming the module.
public sealed class TsqlExecuteStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public SyntaxToken? ReturnStatus { get; init; }
    public SyntaxToken? ReturnEquals { get; init; }
    public required IReadOnlyList<SyntaxToken> Module { get; init; }
    public SyntaxToken? NumberSemicolon { get; init; }
    public SyntaxToken? Number { get; init; }
    public IReadOnlyList<TsqlExecuteArgument> Arguments { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlExecuteOption> Options { get; init; } = [];
}
