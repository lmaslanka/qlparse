namespace QlParse;

// DECLARE @a type [= expr], @t TABLE (...), @c CURSOR, ...
public sealed class TsqlDeclareStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required IReadOnlyList<TsqlVariableDeclaration> Variables { get; init; }
}
