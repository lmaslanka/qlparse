namespace QlParse;

// {ADD | DROP} [COUNTER] SIGNATURE {TO | FROM} [class ::] module BY {CERTIFICATE c | ASYMMETRIC KEY k} [WITH ...], ...
public sealed class TsqlSignatureStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public SyntaxToken? CounterKeyword { get; init; }
    public required SyntaxToken SignatureKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required TsqlSecurable Securable { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<TsqlObjectOption> Signers { get; init; }
}
