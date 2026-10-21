namespace QlParse;

// T-SQL transactions (BEGIN [DISTRIBUTED] / SAVE / COMMIT / ROLLBACK TRAN[SACTION]) and USE database.
// The SET session statements live with the other SET forms in Parser.TsqlProc.Declare.cs.
internal sealed partial class Parser
{
    // BEGIN [DISTRIBUTED] TRAN [name] [WITH MARK ['d']] | SAVE TRAN name |
    // COMMIT [TRAN [name] | WORK] [WITH (DELAYED_DURABILITY = ...)] | ROLLBACK [TRAN [name] | WORK]
    private TsqlTransactionStatement ParseTsqlTransaction()
    {
        var verbKeyword = Advance();
        var distributedKeyword = AcceptTsqlWord(Keyword.ProcDistributed);
        var transactionKeyword = IdentifierEquals(Keyword.ProcTran) || IdentifierEquals(Keyword.Transaction) || IdentifierEquals(Keyword.Work)
            ? Advance()
            : (SyntaxToken?)null;
        SyntaxToken? name = null;
        if (TokenEquals(verbKeyword, Keyword.ProcSave))
        {
            name = transactionKeyword is null ? throw new SqlParseException($"Expected TRANSACTION, found {_current.Kind}", _current.Position) : ExpectTsqlName();
        }
        else
        {
            if (transactionKeyword is not null && !TokenEquals(transactionKeyword.Value, Keyword.Work) && IsTsqlOptionalName())
            {
                name = Advance();
            }
        }

        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return new TsqlTransactionStatement
            {
                Span = SourceSpan.From(verbKeyword, TsqlPreviousToken()),
                VerbKeyword = verbKeyword,
                DistributedKeyword = distributedKeyword,
                TransactionKeyword = transactionKeyword,
                Name = name,
            };
        }

        return ParseTsqlTransactionWith(verbKeyword, distributedKeyword, transactionKeyword, name);
    }

    // WITH MARK ['description'] after BEGIN TRAN, or WITH (DELAYED_DURABILITY = {ON | OFF}) after COMMIT.
    private TsqlTransactionStatement ParseTsqlTransactionWith(
        SyntaxToken verbKeyword,
        SyntaxToken? distributedKeyword,
        SyntaxToken? transactionKeyword,
        SyntaxToken? name)
    {
        var withKeyword = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var markKeyword = ExpectIdent(Keyword.ProcMark);
            var description = _current.Kind == SyntaxKind.String ? Advance() : (SyntaxToken?)null;
            return new TsqlTransactionStatement
            {
                Span = SourceSpan.From(verbKeyword, description ?? markKeyword),
                VerbKeyword = verbKeyword,
                DistributedKeyword = distributedKeyword,
                TransactionKeyword = transactionKeyword,
                Name = name,
                WithKeyword = withKeyword,
                MarkKeyword = markKeyword,
                MarkDescription = description,
            };
        }

        var openParen = Advance();
        var durability = ParseTsqlAssignedOption();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlTransactionStatement
        {
            Span = SourceSpan.From(verbKeyword, closeParen),
            VerbKeyword = verbKeyword,
            DistributedKeyword = distributedKeyword,
            TransactionKeyword = transactionKeyword,
            Name = name,
            WithKeyword = withKeyword,
            OpenParen = openParen,
            Durability = durability,
            CloseParen = closeParen,
        };
    }

    private TsqlUseStatement ParseTsqlUse()
    {
        var useKeyword = Advance();
        var database = Expect(SyntaxKind.Identifier);
        return new TsqlUseStatement { Span = SourceSpan.From(useKeyword, database), UseKeyword = useKeyword, Database = database };
    }
}
