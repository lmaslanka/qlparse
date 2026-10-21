namespace QlParse;

// SQL Server keys, certificates, credentials, server audits and module signatures (workstream F). These statements
// are a verb and object kind followed by a sequence of clauses, each kept as a TsqlObjectOption:
//   CREATE MASTER KEY [ENCRYPTION BY PASSWORD = 'p']            OPEN MASTER KEY DECRYPTION BY PASSWORD = 'p'
//   CREATE CERTIFICATE c [AUTHORIZATION u] {FROM FILE = '...' [WITH PRIVATE KEY (...)] | WITH SUBJECT = '...', ...}
//   CREATE {SYMMETRIC | ASYMMETRIC} KEY k ... WITH ALGORITHM = ... ENCRYPTION BY CERTIFICATE c, PASSWORD = 'p'
//   OPEN SYMMETRIC KEY k DECRYPTION BY CERTIFICATE c [WITH PASSWORD = 'p']      CLOSE ALL SYMMETRIC KEYS
//   CREATE COLUMN MASTER KEY k WITH (...)    CREATE COLUMN ENCRYPTION KEY k WITH VALUES (...) [, (...)]
//   CREATE [DATABASE SCOPED] CREDENTIAL c WITH IDENTITY = '...', SECRET = '...' [FOR CRYPTOGRAPHIC PROVIDER p]
//   CREATE SERVER AUDIT a TO FILE (...) [WITH (...)] [WHERE predicate]
internal sealed partial class Parser
{
    // The verb, `kindLength` object-kind words, the name when the object has one, and its clauses.
    private TsqlSecurityObjectStatement ParseTsqlSecurityObject(int kindLength, bool named)
    {
        var keywords = TakeTsqlWords(kindLength + 1);
        var name = named ? Expect(SyntaxKind.Identifier) : (SyntaxToken?)null;
        var clauses = ParseTsqlSecurityClauses();
        var whereKeyword = _current.Kind == SyntaxKind.WhereKeyword ? Advance() : (SyntaxToken?)null;
        var where = whereKeyword is null ? null : ParseExpression();
        var end = where?.Span ?? (clauses is not null ? clauses[^1].Span : name?.Span ?? keywords[^1].Span);
        return new TsqlSecurityObjectStatement
        {
            Span = SourceSpan.From(keywords[0], end),
            Keywords = keywords,
            Name = name,
            Clauses = clauses ?? (IReadOnlyList<TsqlObjectOption>)[],
            WhereKeyword = whereKeyword,
            Where = where,
        };
    }

    private List<TsqlObjectOption>? ParseTsqlSecurityClauses()
    {
        List<TsqlObjectOption>? clauses = null;
        var clause = TryParseTsqlSecurityClause();
        while (clause is not null)
        {
            (clauses ??= []).Add(clause);
            clause = TryParseTsqlSecurityClause();
        }

        return clauses;
    }

    private TsqlObjectOption? TryParseTsqlSecurityClause() => _current.Kind switch
    {
        SyntaxKind.WithKeyword => ParseTsqlWithClause(),
        SyntaxKind.FromKeyword => ParseTsqlFromClause(),
        SyntaxKind.ForKeyword => NextEquals(Keyword.SecCryptographic) ? ParseTsqlProviderClause() : null,
        SyntaxKind.Comma => NextKind == SyntaxKind.OpenParen ? ParseTsqlContinuationClause() : null,
        SyntaxKind.Identifier => TryParseTsqlKeyClause() ?? TryParseTsqlAlterClause(),
        _ => null,
    };

    // AUTHORIZATION u | {ENCRYPTION | DECRYPTION} BY mechanism, ... | ACTIVE FOR BEGIN_DIALOG = ON | TO target [(...)]
    private TsqlObjectOption? TryParseTsqlKeyClause()
    {
        if (IdentifierEquals(Keyword.Authorization))
        {
            return ParseTsqlNamedClause([Advance()]);
        }

        if ((IdentifierEquals(Keyword.SecEncryption) || IdentifierEquals(Keyword.SecDecryption)) && NextKind == SyntaxKind.ByKeyword)
        {
            var encryption = Advance();
            return TsqlKeywordClause([encryption, Advance()], ParseTsqlKeyMechanisms());
        }

        if (IdentifierEquals(Keyword.SecActive) && NextKind == SyntaxKind.ForKeyword)
        {
            return ParseTsqlOption();
        }

        if (IdentifierEquals(Keyword.To))
        {
            var to = Advance();
            var target = ExpectTsqlWord();
            return TsqlKeywordClause([to, target], _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : null);
        }

        return null;
    }

    // The ALTER forms: {ADD | DROP} ENCRYPTION BY ..., {ADD | DROP} VALUE (...), [FORCE] REGENERATE,
    // REMOVE {PRIVATE KEY | PROVIDER KEY | WHERE}, MODIFY NAME = new.
    private TsqlObjectOption? TryParseTsqlAlterClause()
    {
        if (IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop))
        {
            return TryParseTsqlAddDropClause();
        }

        if (IdentifierEquals(Keyword.SecRegenerate) || (IdentifierEquals(Keyword.Force) && NextEquals(Keyword.SecRegenerate)))
        {
            return TsqlKeywordClause([Advance()], null);
        }

        if (IdentifierEquals(Keyword.SecRemove))
        {
            return ParseTsqlRemoveClause();
        }

        if (IdentifierEquals(Keyword.SecModify) && NextEquals(Keyword.Name))
        {
            return ParseTsqlOption();
        }

        return null;
    }

    private TsqlObjectOption? TryParseTsqlAddDropClause()
    {
        if (NextEquals(Keyword.SecEncryption))
        {
            var verb = Advance();
            var encryption = Advance();
            var by = Expect(SyntaxKind.ByKeyword);
            return TsqlKeywordClause([verb, encryption, by], ParseTsqlKeyMechanisms());
        }

        if (NextEquals(Keyword.Value))
        {
            var verb = Advance();
            return TsqlKeywordClause([verb, Advance()], ParseTsqlOptionList());
        }

        return null;
    }

    private TsqlObjectOption ParseTsqlRemoveClause()
    {
        var remove = Advance();
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            return TsqlKeywordClause([remove, Advance()], null);
        }

        if (!IdentifierEquals(Keyword.SecPrivate) && !IdentifierEquals(Keyword.SecProvider))
        {
            throw new SqlParseException($"Expected PRIVATE KEY, PROVIDER KEY or WHERE, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        return TsqlKeywordClause([remove, kind, ExpectIdent(Keyword.Key)], null);
    }

    // WITH (option, ...) | WITH PRIVATE KEY (option, ...) | WITH VALUES (option, ...) | WITH option, ...
    private TsqlObjectOption ParseTsqlWithClause()
    {
        var with = Advance();
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return TsqlKeywordClause([with], ParseTsqlOptionList());
        }

        if (IdentifierEquals(Keyword.SecPrivate))
        {
            var privateKeyword = Advance();
            var key = ExpectIdent(Keyword.Key);
            return TsqlKeywordClause([with, privateKeyword, key], ParseTsqlOptionList());
        }

        if (_current.Kind == SyntaxKind.ValuesKeyword)
        {
            return TsqlKeywordClause([with, Advance()], ParseTsqlOptionList());
        }

        return TsqlKeywordClause([with], ParseTsqlBareOptionList());
    }

    // FROM {FILE = '...' | EXECUTABLE FILE = '...' | BINARY = 0x... | ASSEMBLY a | PROVIDER p}
    private TsqlObjectOption ParseTsqlFromClause()
    {
        var from = Advance();
        var keywords = new List<SyntaxToken> { from, ExpectTsqlWord() };
        if (TokenEquals(keywords[1], Keyword.SecExecutable))
        {
            keywords.Add(ExpectIdent(Keyword.SecFile));
        }

        return _current.Kind == SyntaxKind.EqualsToken ? FinishTsqlOption(keywords) : ParseTsqlNamedClause(keywords);
    }

    // FOR CRYPTOGRAPHIC PROVIDER p
    private TsqlObjectOption ParseTsqlProviderClause()
    {
        var forKeyword = Advance();
        var cryptographic = Advance();
        return ParseTsqlNamedClause([forKeyword, cryptographic, ExpectIdent(Keyword.SecProvider)]);
    }

    // `, (option, ...)` continuing the previous clause's list, as in WITH VALUES (...), (...).
    private TsqlObjectOption ParseTsqlContinuationClause()
    {
        Advance();
        var list = ParseTsqlOptionList();
        return new TsqlObjectOption { Span = list.Span, Keywords = [], Options = list };
    }

    // {ADD | DROP} [COUNTER] SIGNATURE {TO | FROM} [class ::] module BY mechanism [WITH ...], ...
    private TsqlSignatureStatement ParseTsqlSignature()
    {
        var verb = Advance();
        var counter = IdentifierEquals(Keyword.SecCounter) ? Advance() : (SyntaxToken?)null;
        var signature = Advance();
        var to = _current.Kind == SyntaxKind.FromKeyword ? Advance() : ExpectIdent(Keyword.To);
        var securable = ParseTsqlSecurable();
        var by = Expect(SyntaxKind.ByKeyword);
        var signers = ParseTsqlKeyMechanisms().Items;
        return new TsqlSignatureStatement
        {
            Span = SourceSpan.From(verb, signers[^1].Span),
            VerbKeyword = verb,
            CounterKeyword = counter,
            SignatureKeyword = signature,
            ToKeyword = to,
            Securable = securable,
            ByKeyword = by,
            Signers = signers,
        };
    }

    private bool IsTsqlSignatureNext() =>
        NextEquals(Keyword.SecSignature) || (NextEquals(Keyword.SecCounter) && IsTsqlKindAt(_index + 1, Keyword.SecSignature));
}
