namespace QlParse;

// SQL Server row-level security policies and audit specifications (workstream F).
internal sealed partial class Parser
{
    // {CREATE | ALTER} SECURITY POLICY name [predicate_action, ...] [WITH (STATE = ..., SCHEMABINDING = ...)]
    // [NOT FOR REPLICATION]
    private TsqlSecurityPolicyStatement ParseTsqlSecurityPolicy()
    {
        var verb = Advance();
        var security = Advance();
        var policy = Advance();
        var name = ParseQualifiedName();
        var predicates = ParseTsqlSecurityPredicates();
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = withKeyword is null ? null : ParseTsqlObjectOptionList();
        IReadOnlyList<SyntaxToken> notForReplication = [];
        if (_current.Kind == SyntaxKind.NotKeyword && NextKind == SyntaxKind.ForKeyword)
        {
            var notKeyword = Advance();
            var forKeyword = Advance();
            notForReplication = [notKeyword, forKeyword, ExpectIdent(Keyword.SecReplication)];
        }

        var end = options?.Span ?? (predicates is not null ? predicates[^1].Span : name[^1].Span);
        if (notForReplication.Count > 0)
        {
            end = notForReplication[^1].Span;
        }

        return new TsqlSecurityPolicyStatement
        {
            Span = SourceSpan.From(verb, end),
            VerbKeyword = verb,
            SecurityKeyword = security,
            PolicyKeyword = policy,
            Name = name,
            Predicates = predicates ?? (IReadOnlyList<TsqlSecurityPredicate>)[],
            WithKeyword = withKeyword,
            Options = options,
            NotForReplication = notForReplication,
        };
    }

    private List<TsqlSecurityPredicate>? ParseTsqlSecurityPredicates()
    {
        if (!IsTsqlPredicateActionStart())
        {
            return null;
        }

        var predicates = new List<TsqlSecurityPredicate> { ParseTsqlSecurityPredicate() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            predicates.Add(ParseTsqlSecurityPredicate());
        }

        return predicates;
    }

    private bool IsTsqlPredicateActionStart() =>
        (IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Alter) || IdentifierEquals(Keyword.Drop))
            && (NextEquals(Keyword.Filter) || NextEquals(Keyword.SecBlock) || NextEquals(Keyword.SecPredicate));

    // {ADD | ALTER | DROP} [FILTER | BLOCK] PREDICATE [fn(arg, ...)] ON table [{AFTER | BEFORE} {INSERT | UPDATE | DELETE}]
    private TsqlSecurityPredicate ParseTsqlSecurityPredicate()
    {
        var action = Advance();
        var kind = IdentifierEquals(Keyword.Filter) || IdentifierEquals(Keyword.SecBlock) ? Advance() : (SyntaxToken?)null;
        var predicate = ExpectIdent(Keyword.SecPredicate);
        var function = _current.Kind == SyntaxKind.OnKeyword ? default : ParseTsqlPredicateFunction();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var table = ParseQualifiedName();
        IReadOnlyList<SyntaxToken> operation = [];
        if (IdentifierEquals(Keyword.After) || IdentifierEquals(Keyword.Before))
        {
            var timing = Advance();
            operation = [timing, ExpectTsqlWord()];
        }

        return new TsqlSecurityPredicate
        {
            Span = SourceSpan.From(action, operation.Count > 0 ? operation[^1] : table[^1]),
            ActionKeyword = action,
            KindKeyword = kind,
            PredicateKeyword = predicate,
            FunctionName = function.Name ?? (IReadOnlyList<SyntaxToken>)[],
            OpenParen = function.OpenParen,
            Arguments = function.Arguments ?? (IReadOnlyList<Expression>)[],
            CloseParen = function.CloseParen,
            OnKeyword = onKeyword,
            Table = table,
            BlockOperation = operation,
        };
    }

    private readonly record struct TsqlPredicateFunction(
        List<SyntaxToken>? Name,
        SyntaxToken? OpenParen,
        List<Expression>? Arguments,
        SyntaxToken? CloseParen);

    private TsqlPredicateFunction ParseTsqlPredicateFunction()
    {
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = new List<Expression>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            arguments.Add(ParseExpression());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParseExpression());
            }
        }

        return new TsqlPredicateFunction(name, openParen, arguments, Expect(SyntaxKind.CloseParen));
    }

    // {CREATE | ALTER} {SERVER | DATABASE} AUDIT SPECIFICATION name [FOR SERVER AUDIT audit]
    // [{ADD | DROP} (...), ...] [WITH (STATE = {ON | OFF})]
    private TsqlAuditSpecificationStatement ParseTsqlAuditSpecification()
    {
        var keywords = TakeTsqlWords(Len4);
        var name = Expect(SyntaxKind.Identifier);
        IReadOnlyList<SyntaxToken> forServerAudit = [];
        SyntaxToken? audit = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            var forKeyword = Advance();
            var server = ExpectIdent(Keyword.SecServer);
            forServerAudit = [forKeyword, server, ExpectIdent(Keyword.SecAudit)];
            audit = Expect(SyntaxKind.Identifier);
        }

        var actions = ParseTsqlAuditActions();
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = withKeyword is null ? null : ParseTsqlObjectOptionList();
        var end = options?.Span ?? (actions is not null ? actions[^1].Span : audit?.Span ?? name.Span);
        return new TsqlAuditSpecificationStatement
        {
            Span = SourceSpan.From(keywords[0], end),
            Keywords = keywords,
            Name = name,
            ForServerAudit = forServerAudit,
            Audit = audit,
            Actions = actions ?? (IReadOnlyList<TsqlAuditAction>)[],
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private List<TsqlAuditAction>? ParseTsqlAuditActions()
    {
        if (!IsTsqlAuditActionStart(_current, NextKind))
        {
            return null;
        }

        var actions = new List<TsqlAuditAction> { ParseTsqlAuditAction() };
        while (_current.Kind == SyntaxKind.Comma && _index + 1 < _tokens.Count
            && IsTsqlAuditActionStart(_tokens[_index], _tokens[_index + 1].Kind))
        {
            Advance();
            actions.Add(ParseTsqlAuditAction());
        }

        return actions;
    }

    private bool IsTsqlAuditActionStart(SyntaxToken token, SyntaxKind next) =>
        next == SyntaxKind.OpenParen && (TokenEquals(token, Keyword.Add) || TokenEquals(token, Keyword.Drop));

    // {ADD | DROP} (group) or {ADD | DROP} (action, ... ON [class ::] securable BY principal, ...)
    private TsqlAuditAction ParseTsqlAuditAction()
    {
        var verb = Advance();
        var openParen = Advance();
        var actions = ParseTsqlPermissions();
        SyntaxToken? onKeyword = null;
        TsqlSecurable? securable = null;
        SyntaxToken? byKeyword = null;
        IReadOnlyList<SyntaxToken> principals = [];
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            securable = ParseTsqlSecurable();
            byKeyword = Expect(SyntaxKind.ByKeyword);
            principals = ParseGrantees();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlAuditAction
        {
            Span = SourceSpan.From(verb, closeParen),
            ActionKeyword = verb,
            OpenParen = openParen,
            Actions = actions,
            OnKeyword = onKeyword,
            Securable = securable,
            ByKeyword = byKeyword,
            Principals = principals,
            CloseParen = closeParen,
        };
    }
}
