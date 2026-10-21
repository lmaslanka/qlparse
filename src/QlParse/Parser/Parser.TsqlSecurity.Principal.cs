namespace QlParse;

// SQL Server principals (workstream F): logins, users, database / server / application roles, and DROP of every
// object kind this workstream creates.
internal sealed partial class Parser
{
    // CREATE {LOGIN | USER | ROLE | SERVER ROLE | APPLICATION ROLE} name [AUTHORIZATION owner]
    // [{FOR | FROM} {LOGIN l | WINDOWS | EXTERNAL PROVIDER | CERTIFICATE c | ASYMMETRIC KEY k} | WITHOUT LOGIN]
    // [WITH option, ...]
    private TsqlCreatePrincipalStatement ParseTsqlCreatePrincipal(int kindLength)
    {
        var create = Advance();
        var kind = TakeTsqlWords(kindLength);
        var name = Expect(SyntaxKind.Identifier);
        var authorization = IdentifierEquals(Keyword.Authorization) ? Advance() : (SyntaxToken?)null;
        var owner = authorization is null ? (SyntaxToken?)null : ParseGrantee();
        var (sourceKeywords, source) = ParseTsqlPrincipalSource();
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = withKeyword is null ? null : ParseTsqlObjectOptionItems();
        var end = source?.Span ?? (sourceKeywords.Count > 0 ? sourceKeywords[^1].Span : owner?.Span ?? name.Span);
        if (options is not null)
        {
            end = options[^1].Span;
        }

        return new TsqlCreatePrincipalStatement
        {
            Span = SourceSpan.From(create, end),
            CreateKeyword = create,
            KindKeywords = kind,
            Name = name,
            AuthorizationKeyword = authorization,
            Owner = owner,
            SourceKeywords = sourceKeywords,
            Source = source,
            WithKeyword = withKeyword,
            Options = options ?? (IReadOnlyList<TsqlObjectOption>)[],
        };
    }

    private (IReadOnlyList<SyntaxToken> Keywords, SyntaxToken? Name) ParseTsqlPrincipalSource()
    {
        if (IdentifierEquals(Keyword.Without))
        {
            var without = Advance();
            return ([without, ExpectIdent(Keyword.SecLogin)], null);
        }

        if (_current.Kind is not (SyntaxKind.ForKeyword or SyntaxKind.FromKeyword))
        {
            return ([], null);
        }

        var preposition = Advance();
        var kind = ExpectTsqlWord();
        if (TokenEquals(kind, Keyword.SecWindows))
        {
            return ([preposition, kind], null);
        }

        if (TokenEquals(kind, Keyword.External))
        {
            return ([preposition, kind, ExpectIdent(Keyword.SecProvider)], null);
        }

        if (TokenEquals(kind, Keyword.Asymmetric))
        {
            var key = ExpectIdent(Keyword.Key);
            return ([preposition, kind, key], Expect(SyntaxKind.Identifier));
        }

        return ([preposition, kind], Expect(SyntaxKind.Identifier));
    }

    // ALTER {LOGIN | USER | ROLE | SERVER ROLE | APPLICATION ROLE} name
    // {WITH option, ... | ENABLE | DISABLE | {ADD | DROP} {MEMBER | CREDENTIAL} name}
    private TsqlAlterPrincipalStatement ParseTsqlAlterPrincipal(int kindLength)
    {
        var alter = Advance();
        var kind = TakeTsqlWords(kindLength);
        var name = Expect(SyntaxKind.Identifier);
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            var withKeyword = Advance();
            var options = ParseTsqlObjectOptionItems();
            return BuildTsqlAlterPrincipal(alter, kind, name, [withKeyword], null, options);
        }

        if (IdentifierEquals(Keyword.SecEnable) || IdentifierEquals(Keyword.Disable))
        {
            return BuildTsqlAlterPrincipal(alter, kind, name, [Advance()], null, null);
        }

        if (!IdentifierEquals(Keyword.Add) && !IdentifierEquals(Keyword.Drop))
        {
            throw new SqlParseException($"Expected WITH, ENABLE, DISABLE, ADD or DROP, found {_current.Kind}", _current.Position);
        }

        var verb = Advance();
        var target = IdentifierEquals(Keyword.SecMember) ? Advance() : ExpectIdent(Keyword.SecCredential);
        return BuildTsqlAlterPrincipal(alter, kind, name, [verb, target], ParseGrantee(), null);
    }

    private static TsqlAlterPrincipalStatement BuildTsqlAlterPrincipal(
        SyntaxToken alter,
        List<SyntaxToken> kind,
        SyntaxToken name,
        List<SyntaxToken> action,
        SyntaxToken? member,
        List<TsqlObjectOption>? options) => new()
        {
            Span = SourceSpan.From(alter.Span, options is not null ? options[^1].Span : member?.Span ?? action[^1].Span),
            AlterKeyword = alter,
            KindKeywords = kind,
            Name = name,
            ActionKeywords = action,
            Member = member,
            Options = options ?? (IReadOnlyList<TsqlObjectOption>)[],
        };

    // DROP kind [IF EXISTS] [name, ...]; MASTER KEY and DATABASE ENCRYPTION KEY have no name.
    private TsqlDropSecurableStatement ParseTsqlDropSecurable(int kindLength)
    {
        var unnamed = IsTsqlUnnamedKindAt(_index);
        var drop = Advance();
        var kind = TakeTsqlWords(kindLength);
        var (ifKeyword, existsKeyword) = ParseIfExists();
        var names = unnamed ? null : ParseSchemaNameList();
        var options = ParseTsqlDropSecurableTail(kind);
        return new TsqlDropSecurableStatement
        {
            Span = SourceSpan.From(drop, TsqlLastConsumed()),
            DropKeyword = drop,
            KindKeywords = kind,
            IfKeyword = ifKeyword,
            ExistsKeyword = existsKeyword,
            Names = names ?? (IReadOnlyList<IReadOnlyList<SyntaxToken>>)[],
            Options = options,
        };
    }

    private bool IsTsqlUnnamedKindAt(int index) =>
        IsTsqlKindAt(index, Keyword.SecMaster)
            || (IsTsqlKindAt(index, Keyword.UtilDatabase) && IsTsqlKindAt(index + 1, Keyword.SecEncryption));

    // The number of object-kind words starting at `index` when they name an object this workstream owns
    // (`LOGIN`, `SERVER ROLE`, `XML SCHEMA COLLECTION`, ...), otherwise 0.
    private int TsqlSecurableKindLength(int index)
    {
        if (index >= _tokens.Count)
        {
            return 0;
        }

        var token = _tokens[index];
        if (token.Kind == SyntaxKind.UserKeyword)
        {
            return 1;
        }

        if (token.Kind != SyntaxKind.Identifier)
        {
            return 0;
        }

        var text = token.TextOf(_source);
        var compound = TsqlCompoundKindLength(text, index + 1) ?? TsqlKeyKindLength(text, index + 1);
        if (compound is int length)
        {
            return length;
        }

        return IsTsqlSingleWordKind(text) ? 1 : 0;
    }

    // DATABASE [SCOPED CREDENTIAL | AUDIT SPECIFICATION | ENCRYPTION KEY], SERVER {ROLE | AUDIT [SPECIFICATION]},
    // EXTERNAL {DATA SOURCE | FILE FORMAT | TABLE | MODEL}, and the TsqlNamedCompoundKindLength words; null when the
    // first word is none of these.
    private int? TsqlCompoundKindLength(ReadOnlySpan<char> text, int next)
    {
        if (TextEquals(text, Keyword.UtilDatabase))
        {
            return IsTsqlKindAt(next, Keyword.SecScoped) || IsTsqlKindAt(next, Keyword.SecAudit) || IsTsqlKindAt(next, Keyword.SecEncryption)
                ? Len3
                : 1;
        }

        if (TextEquals(text, Keyword.SecServer))
        {
            return TsqlServerKindLength(next);
        }

        if (TextEquals(text, Keyword.External))
        {
            return TsqlExternalKindLength(next);
        }

        return TsqlNamedCompoundKindLength(text, next);
    }

    // XML SCHEMA COLLECTION, APPLICATION ROLE, SECURITY POLICY; null for any other first word.
    private int? TsqlNamedCompoundKindLength(ReadOnlySpan<char> text, int next)
    {
        if (TextEquals(text, Keyword.Xml))
        {
            return IsTsqlKindAt(next, Keyword.Schema) && IsTsqlKindAt(next + 1, Keyword.SecCollection) ? Len3 : 0;
        }

        if (TextEquals(text, Keyword.SecApplication))
        {
            return IsTsqlKindAt(next, Keyword.Role) ? Len2 : 0;
        }

        if (TextEquals(text, Keyword.UtilSecurity))
        {
            return IsTsqlKindAt(next, Keyword.UtilPolicy) ? Len2 : 0;
        }

        return null;
    }

    private int TsqlServerKindLength(int next)
    {
        if (IsTsqlKindAt(next, Keyword.Role))
        {
            return Len2;
        }

        if (!IsTsqlKindAt(next, Keyword.SecAudit))
        {
            return 0;
        }

        return IsTsqlKindAt(next + 1, Keyword.SecSpecification) ? Len3 : Len2;
    }

    private int TsqlExternalKindLength(int next)
    {
        if ((IsTsqlKindAt(next, Keyword.Data) && IsTsqlKindAt(next + 1, Keyword.Source))
            || (IsTsqlKindAt(next, Keyword.SecFile) && IsTsqlKindAt(next + 1, Keyword.Format)))
        {
            return Len3;
        }

        return IsTsqlKindAt(next, Keyword.Table) || IsTsqlKindAt(next, Keyword.SecModel) ? Len2 : 0;
    }

    // MASTER KEY, SYMMETRIC KEY, ASYMMETRIC KEY, COLUMN {MASTER | ENCRYPTION} KEY; null for any other first word.
    private int? TsqlKeyKindLength(ReadOnlySpan<char> text, int next)
    {
        if (TextEquals(text, Keyword.SecMaster) || TextEquals(text, Keyword.Symmetric) || TextEquals(text, Keyword.Asymmetric))
        {
            return IsTsqlKindAt(next, Keyword.Key) ? Len2 : 0;
        }

        if (TextEquals(text, Keyword.Column))
        {
            return (IsTsqlKindAt(next, Keyword.SecMaster) || IsTsqlKindAt(next, Keyword.SecEncryption))
                && IsTsqlKindAt(next + 1, Keyword.Key)
                ? Len3
                : 0;
        }

        return null;
    }

    private static bool IsTsqlSingleWordKind(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.SecLogin) || TextEquals(text, Keyword.Role) || TextEquals(text, Keyword.Schema)
            || TextEquals(text, Keyword.SecAssembly) || TextEquals(text, Keyword.Default) || TextEquals(text, Keyword.DdlRule)
            || TextEquals(text, Keyword.SecCertificate) || TextEquals(text, Keyword.SecCredential);
}
