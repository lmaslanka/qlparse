namespace QlParse;

// {CREATE [OR ALTER] | ALTER} FUNCTION name ([@param ...]) RETURNS {type | TABLE | @var TABLE (...)}
// [WITH option, ...] [AS] {BEGIN ... END | RETURN [(] select [)] | EXTERNAL NAME a.c.m}.
// Scalar functions set ReturnType; inline table-valued functions set ReturnTableKeyword; multi-statement
// table-valued functions set ReturnVariable and ReturnTable (CLR table-valued functions set only ReturnTable).
public sealed class TsqlCreateFunctionStatement : Query
{
    public SyntaxToken? CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? AlterKeyword { get; init; }
    public required SyntaxToken FunctionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<TsqlParameter> Parameters { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken ReturnsKeyword { get; init; }
    public DataType? ReturnType { get; init; }
    public SyntaxToken? ReturnTableKeyword { get; init; }
    public SyntaxToken? ReturnVariable { get; init; }
    public TsqlTableDefinition? ReturnTable { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlOption> Options { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public Query? Body { get; init; }
    public TsqlExternalName? ExternalName { get; init; }
}
