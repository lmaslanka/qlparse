namespace QlParse;

// {CREATE [OR ALTER] | ALTER} TRIGGER name ON {table | DATABASE | ALL SERVER} [WITH option, ...]
// {FOR | AFTER | INSTEAD OF} event[, ...] [WITH APPEND] [NOT FOR REPLICATION] AS {statements | EXTERNAL NAME a.c.m}.
// Target is set for a DML trigger; DatabaseKeyword or AllKeyword + ServerKeyword for DDL and logon triggers.
public sealed class TsqlCreateTriggerStatement : Query
{
    public SyntaxToken? CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? AlterKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Target { get; init; } = [];
    public SyntaxToken? DatabaseKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? ServerKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlOption> Options { get; init; } = [];
    public required SyntaxToken TimingKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Events { get; init; }
    public SyntaxToken? AppendWithKeyword { get; init; }
    public SyntaxToken? AppendKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ReplicationForKeyword { get; init; }
    public SyntaxToken? ReplicationKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public TsqlExternalName? ExternalName { get; init; }
}
