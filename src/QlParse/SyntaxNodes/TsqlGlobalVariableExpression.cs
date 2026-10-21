namespace QlParse;

// `@@ROWCOUNT`, `@@ERROR`, `@@IDENTITY`, ...: a SQL Server system function spelled like a variable.
public sealed class TsqlGlobalVariableExpression : Expression
{
    public required SyntaxToken Name { get; init; }
}
