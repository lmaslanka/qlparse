namespace QlParse;

// SQL Server permissions and execution context (workstream F): GRANT / DENY / REVOKE with multi-word permissions and
// `class::name` securables, EXECUTE AS and REVERT.
internal sealed partial class Parser
{
    private readonly record struct TsqlPermissionTarget(
        SyntaxToken? OnKeyword,
        TsqlSecurable? Securable,
        SyntaxToken? OpenParen,
        IReadOnlyList<SyntaxToken> Columns,
        SyntaxToken? CloseParen);

    // GRANT | DENY | REVOKE [GRANT OPTION FOR] permission, ... [ON [class ::] securable [(column, ...)]]
    // {TO | FROM} principal, ... [WITH GRANT OPTION] [CASCADE] [AS principal]
    private TsqlPermissionStatement ParseTsqlPermissionStatement()
    {
        var verb = Advance();
        var isRevoke = TokenEquals(verb, Keyword.Revoke);
        var grantOptionFor = isRevoke ? ParseTsqlGrantOptionFor() : [];
        var permissions = ParseTsqlPermissions();
        var target = ParseTsqlPermissionTarget();
        var toKeyword = isRevoke && _current.Kind == SyntaxKind.FromKeyword ? Advance() : ExpectIdent(Keyword.To);
        var principals = ParseGrantees();
        var tail = ParseTsqlPermissionTail(verb);
        var end = tail.AsPrincipal?.Span ?? tail.Cascade?.Span ?? tail.GrantOption?.Span ?? principals[^1].Span;
        return new TsqlPermissionStatement
        {
            Span = SourceSpan.From(verb, end),
            VerbKeyword = verb,
            GrantOptionFor = grantOptionFor,
            Permissions = permissions,
            OnKeyword = target.OnKeyword,
            Securable = target.Securable,
            OpenParen = target.OpenParen,
            Columns = target.Columns,
            CloseParen = target.CloseParen,
            ToKeyword = toKeyword,
            Principals = principals,
            GrantOption = tail.GrantOption,
            CascadeKeyword = tail.Cascade,
            AsKeyword = tail.AsKeyword,
            AsPrincipal = tail.AsPrincipal,
        };
    }

    private readonly record struct TsqlPermissionTail(
        GrantOptionClause? GrantOption,
        SyntaxToken? Cascade,
        SyntaxToken? AsKeyword,
        SyntaxToken? AsPrincipal);

    // [WITH GRANT OPTION] [CASCADE] [AS principal]
    private TsqlPermissionTail ParseTsqlPermissionTail(SyntaxToken verb)
    {
        var grantOption = IsWithOption(Keyword.Grant) && TokenEquals(verb, Keyword.Grant) ? ParseWithOption() : null;
        var cascade = IdentifierEquals(Keyword.Cascade) ? Advance() : (SyntaxToken?)null;
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var asPrincipal = asKeyword is null ? (SyntaxToken?)null : ParseGrantee();
        return new TsqlPermissionTail(grantOption, cascade, asKeyword, asPrincipal);
    }

    private IReadOnlyList<SyntaxToken> ParseTsqlGrantOptionFor()
    {
        if (!IdentifierEquals(Keyword.Grant) || !NextEquals(Keyword.Option))
        {
            return [];
        }

        var grant = Advance();
        var option = Advance();
        return [grant, option, Expect(SyntaxKind.ForKeyword)];
    }

    private TsqlPermissionTarget ParseTsqlPermissionTarget()
    {
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return new TsqlPermissionTarget(null, null, null, [], null);
        }

        var onKeyword = Advance();
        var securable = ParseTsqlSecurable();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlPermissionTarget(onKeyword, securable, null, [], null);
        }

        ParseRequiredNameList(out var openParen, out var columns, out var closeParen);
        return new TsqlPermissionTarget(onKeyword, securable, openParen, columns, closeParen);
    }

    // permission [(column, ...)], ... where a permission is one or more words (`SELECT`, `ALTER ANY USER`, `ALL PRIVILEGES`).
    internal List<TsqlPermission> ParseTsqlPermissions()
    {
        var permissions = new List<TsqlPermission> { ParseTsqlPermission() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            permissions.Add(ParseTsqlPermission());
        }

        return permissions;
    }

    private TsqlPermission ParseTsqlPermission()
    {
        if (!IsTsqlPermissionWord())
        {
            throw new SqlParseException($"Expected permission, found {_current.Kind}", _current.Position);
        }

        var keywords = new List<SyntaxToken> { Advance() };
        while (IsTsqlPermissionWord())
        {
            keywords.Add(Advance());
        }

        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlPermission { Span = SourceSpan.From(keywords[0], keywords[^1]), Keywords = keywords };
        }

        ParseRequiredNameList(out var openParen, out var columns, out var closeParen);
        return new TsqlPermission
        {
            Span = SourceSpan.From(keywords[0], closeParen),
            Keywords = keywords,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    private bool IsTsqlPermissionWord() =>
        IsTsqlWordToken(_current)
            && _current.Kind is not (SyntaxKind.OnKeyword or SyntaxKind.FromKeyword or SyntaxKind.AsKeyword or SyntaxKind.WithKeyword)
            && !IdentifierEquals(Keyword.To);

    // EXEC[UTE] AS {CALLER | SELF | OWNER | USER = 'name' | LOGIN = 'name'} [WITH {NO REVERT | COOKIE INTO @cookie}]
    private Query? TryParseTsqlExecuteAs() => NextKind == SyntaxKind.AsKeyword ? ParseTsqlExecuteAs() : null;

    private TsqlExecuteAsStatement ParseTsqlExecuteAs()
    {
        var execute = Advance();
        var asKeyword = Advance();
        var context = ExpectTsqlExecutionContext();
        SyntaxToken? equalsToken = null;
        SyntaxToken? principal = null;
        if (context.Kind == SyntaxKind.UserKeyword || TokenEquals(context, Keyword.SecLogin))
        {
            equalsToken = Expect(SyntaxKind.EqualsToken);
            principal = ExpectTsqlStringOrVariable();
        }

        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return new TsqlExecuteAsStatement
            {
                Span = SourceSpan.From(execute, principal ?? context),
                ExecuteKeyword = execute,
                AsKeyword = asKeyword,
                ContextKeyword = context,
                EqualsToken = equalsToken,
                Principal = principal,
            };
        }

        var withKeyword = Advance();
        var (withKeywords, cookie) = ParseTsqlExecuteAsWith();
        return new TsqlExecuteAsStatement
        {
            Span = SourceSpan.From(execute, cookie ?? withKeywords[^1]),
            ExecuteKeyword = execute,
            AsKeyword = asKeyword,
            ContextKeyword = context,
            EqualsToken = equalsToken,
            Principal = principal,
            WithKeyword = withKeyword,
            WithKeywords = withKeywords,
            Cookie = cookie,
        };
    }

    private SyntaxToken ExpectTsqlExecutionContext()
    {
        if (_current.Kind == SyntaxKind.UserKeyword || IdentifierEquals(Keyword.SecLogin) || IsTsqlModuleContext())
        {
            return Advance();
        }

        throw new SqlParseException($"Expected CALLER, SELF, OWNER, USER or LOGIN, found {_current.Kind}", _current.Position);
    }

    private bool IsTsqlModuleContext() =>
        IdentifierEquals(Keyword.SecCaller) || IdentifierEquals(Keyword.Self) || IdentifierEquals(Keyword.SecOwner);

    // NO REVERT | COOKIE INTO @cookie
    private (List<SyntaxToken> Keywords, SyntaxToken? Cookie) ParseTsqlExecuteAsWith()
    {
        if (IdentifierEquals(Keyword.No))
        {
            var no = Advance();
            return ([no, ExpectIdent(Keyword.SecRevert)], null);
        }

        var cookieKeyword = ExpectIdent(Keyword.SecCookie);
        var into = ExpectIdent(Keyword.Into);
        return ([cookieKeyword, into], Expect(SyntaxKind.EmbeddedHost));
    }

    private SyntaxToken ExpectTsqlStringOrVariable()
    {
        if (_current.Kind is SyntaxKind.String or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected string or variable, found {_current.Kind}", _current.Position);
    }

    // REVERT [WITH COOKIE = @cookie]
    private TsqlRevertStatement ParseTsqlRevert()
    {
        var revert = Advance();
        if (_current.Kind != SyntaxKind.WithKeyword || !NextEquals(Keyword.SecCookie))
        {
            return new TsqlRevertStatement { Span = revert.Span, RevertKeyword = revert };
        }

        var withKeyword = Advance();
        var cookieKeyword = Advance();
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var cookie = Expect(SyntaxKind.EmbeddedHost);
        return new TsqlRevertStatement
        {
            Span = SourceSpan.From(revert, cookie),
            RevertKeyword = revert,
            WithKeyword = withKeyword,
            CookieKeyword = cookieKeyword,
            EqualsToken = equalsToken,
            Cookie = cookie,
        };
    }
}
