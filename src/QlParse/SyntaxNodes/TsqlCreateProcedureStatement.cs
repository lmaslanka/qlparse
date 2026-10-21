namespace QlParse;

// {CREATE [OR ALTER] | ALTER} PROC[EDURE] name [;number] [(] @param ... [)] [WITH option, ...] [FOR REPLICATION]
// AS {statements | EXTERNAL NAME a.c.m}. CreateKeyword or AlterKeyword (or both, with OrKeyword) is set.
public sealed class TsqlCreateProcedureStatement : Query
{
    public SyntaxToken? CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? AlterKeyword { get; init; }
    public required SyntaxToken ProcedureKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? NumberSemicolon { get; init; }
    public SyntaxToken? Number { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlParameter> Parameters { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlOption> Options { get; init; } = [];
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? ReplicationKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public TsqlExternalName? ExternalName { get; init; }
}
