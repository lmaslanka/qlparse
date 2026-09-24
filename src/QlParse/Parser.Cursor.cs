namespace QlParse;

internal sealed partial class Parser
{
    private bool IsConnect() => IdentifierEquals(Keyword.Connect);

    private bool IsDisconnect() => IdentifierEquals(Keyword.Disconnect);

    private bool IsSetConnection() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Connection);

    private bool IsDeclareCursor() => LooksLikeCursor(Keyword.Declare);

    private bool IsOpen() => IdentifierEquals(Keyword.Open);

    private bool IsFetch() => _current.Kind == SyntaxKind.FetchKeyword;

    private bool IsClose() => IdentifierEquals(Keyword.Close);

    private bool IsAllocateCursor() => LooksLikeCursor(Keyword.Allocate);

    private bool IsDeallocate() =>
        IdentifierEquals(Keyword.Deallocate)
        && (NextEquals(Keyword.Prepare) || NextEquals(Keyword.Descriptor));

    private ConnectStatement ParseConnect()
    {
        var connectKeyword = Advance();
        var toKeyword = ExpectIdent(Keyword.To);
        var target = ParseConnectionTarget();
        SyntaxToken? asKeyword = null;
        SyntaxToken? connectionName = null;
        SyntaxToken? userKeyword = null;
        SyntaxToken? user = null;
        var end = target.Span;
        if (!TokenEquals(target, Keyword.Default) && _current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            var name = ParseSessionValue();
            connectionName = name;
            end = name.Span;
        }

        if (!TokenEquals(target, Keyword.Default) && _current.Kind == SyntaxKind.UserKeyword)
        {
            userKeyword = Advance();
            var userToken = ParseSessionValue();
            user = userToken;
            end = userToken.Span;
        }

        return new ConnectStatement
        {
            Span = SourceSpan.From(connectKeyword, end),
            ConnectKeyword = connectKeyword,
            ToKeyword = toKeyword,
            Target = target,
            AsKeyword = asKeyword,
            ConnectionName = connectionName,
            UserKeyword = userKeyword,
            User = user,
        };
    }

    private DisconnectStatement ParseDisconnect()
    {
        var disconnectKeyword = Advance();
        var target = ParseConnectionObject(allowAll: true, allowCurrent: true);
        return new DisconnectStatement
        {
            Span = SourceSpan.From(disconnectKeyword, target),
            DisconnectKeyword = disconnectKeyword,
            Target = target,
        };
    }

    private SetConnectionStatement ParseSetConnection()
    {
        var setKeyword = Advance();
        var connectionKeyword = ExpectIdent(Keyword.Connection);
        var target = ParseConnectionObject(allowAll: false, allowCurrent: false);
        return new SetConnectionStatement
        {
            Span = SourceSpan.From(setKeyword, target),
            SetKeyword = setKeyword,
            ConnectionKeyword = connectionKeyword,
            Target = target,
        };
    }

    private Query ParseDeclareCursor()
    {
        var declareKeyword = Advance();
        var name = ParseCursorName();
        ParseCursorIntent(out var sensitivity, out var noScroll, out var scroll);
        var cursorKeyword = ExpectIdent(Keyword.Cursor);
        ParseHold(out var holdWith, out var hold);
        ParseReturn(out var returnWith, out var returnKeyword);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        if (IsCursorQueryStart())
        {
            var query = ParseQuery();
            return new DeclareCursorStatement
            {
                Span = SourceSpan.From(declareKeyword, query.Span),
                DeclareKeyword = declareKeyword,
                Name = name,
                Sensitivity = sensitivity,
                NoScroll = noScroll,
                Scroll = scroll,
                CursorKeyword = cursorKeyword,
                HoldWith = holdWith,
                Hold = hold,
                ReturnWith = returnWith,
                ReturnKeyword = returnKeyword,
                ForKeyword = forKeyword,
                Query = query,
            };
        }

        var statement = ParseCursorName();
        return new DynamicDeclareCursorStatement
        {
            Span = SourceSpan.From(declareKeyword, statement),
            DeclareKeyword = declareKeyword,
            Name = name,
            Sensitivity = sensitivity,
            NoScroll = noScroll,
            Scroll = scroll,
            CursorKeyword = cursorKeyword,
            HoldWith = holdWith,
            Hold = hold,
            ReturnWith = returnWith,
            ReturnKeyword = returnKeyword,
            ForKeyword = forKeyword,
            Statement = statement,
        };
    }

    private OpenStatement ParseOpen()
    {
        var openKeyword = Advance();
        var cursor = ParseCursorName();
        return new OpenStatement
        {
            Span = SourceSpan.From(openKeyword, cursor),
            OpenKeyword = openKeyword,
            Cursor = cursor,
        };
    }

    private FetchStatement ParseFetchStatement()
    {
        var fetchKeyword = Advance();
        SyntaxToken? orientation = null;
        Expression? offset = null;
        SyntaxToken? fromKeyword = null;
        if (IsAbsoluteOrRelative() || IsOrientationFrom())
        {
            var orientationToken = Advance();
            orientation = orientationToken;
            if (TokenEquals(orientationToken, Keyword.Absolute) || TokenEquals(orientationToken, Keyword.Relative))
            {
                offset = ParseExpression();
            }

            fromKeyword = Expect(SyntaxKind.FromKeyword);
        }
        else if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
        }

        var cursor = ParseCursorName();
        var intoKeyword = ExpectIdent(Keyword.Into);
        var targets = ParseIntoTargets();
        return new FetchStatement
        {
            Span = SourceSpan.From(fetchKeyword, targets[^1]),
            FetchKeyword = fetchKeyword,
            Orientation = orientation,
            Offset = offset,
            FromKeyword = fromKeyword,
            Cursor = cursor,
            IntoKeyword = intoKeyword,
            Targets = targets,
        };
    }

    private CloseStatement ParseClose()
    {
        var closeKeyword = Advance();
        var cursor = ParseCursorName();
        return new CloseStatement
        {
            Span = SourceSpan.From(closeKeyword, cursor),
            CloseKeyword = closeKeyword,
            Cursor = cursor,
        };
    }

    private AllocateCursorStatement ParseAllocateCursor()
    {
        var allocateKeyword = Advance();
        var name = ParseCursorName();
        ParseCursorIntent(out var sensitivity, out var noScroll, out var scroll);
        var cursorKeyword = ExpectIdent(Keyword.Cursor);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var statement = ParseCursorName();
        return new AllocateCursorStatement
        {
            Span = SourceSpan.From(allocateKeyword, statement),
            AllocateKeyword = allocateKeyword,
            Name = name,
            Sensitivity = sensitivity,
            NoScroll = noScroll,
            Scroll = scroll,
            CursorKeyword = cursorKeyword,
            ForKeyword = forKeyword,
            Statement = statement,
        };
    }

    private DeallocateStatement ParseDeallocate()
    {
        var deallocateKeyword = Advance();
        if (!IdentifierEquals(Keyword.Prepare) && !IdentifierEquals(Keyword.Descriptor))
        {
            throw new SqlParseException($"Expected PREPARE or DESCRIPTOR, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        var name = ParseCursorName();
        return new DeallocateStatement
        {
            Span = SourceSpan.From(deallocateKeyword, name),
            DeallocateKeyword = deallocateKeyword,
            Kind = kind,
            Name = name,
        };
    }

    private void ParseCursorIntent(
        out SyntaxToken? sensitivity,
        out SyntaxToken? noScroll,
        out SyntaxToken? scroll)
    {
        sensitivity = IsSensitivity() ? Advance() : null;
        noScroll = null;
        scroll = null;
        if (IdentifierEquals(Keyword.No) && NextEquals(Keyword.Scroll))
        {
            noScroll = Advance();
            scroll = Advance();
            return;
        }

        if (IdentifierEquals(Keyword.Scroll))
        {
            scroll = Advance();
        }
    }

    private void ParseHold(out SyntaxToken? withKeyword, out SyntaxToken? hold)
    {
        withKeyword = null;
        hold = null;
        if (!IsWithWord(Keyword.Hold))
        {
            return;
        }

        withKeyword = Advance();
        hold = ExpectIdent(Keyword.Hold);
    }

    private void ParseReturn(out SyntaxToken? withKeyword, out SyntaxToken? returnKeyword)
    {
        withKeyword = null;
        returnKeyword = null;
        if (!IsWithWord(Keyword.Return))
        {
            return;
        }

        withKeyword = Advance();
        returnKeyword = ExpectIdent(Keyword.Return);
    }

    private SyntaxToken ParseConnectionTarget()
    {
        if (IdentifierEquals(Keyword.Default) || IsSessionValue())
        {
            return Advance();
        }

        throw new SqlParseException($"Expected connection target, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ParseConnectionObject(bool allowAll, bool allowCurrent)
    {
        if ((allowAll && _current.Kind == SyntaxKind.AllKeyword)
            || (allowCurrent && IdentifierEquals(Keyword.Current))
            || IdentifierEquals(Keyword.Default)
            || IsSessionValue())
        {
            return Advance();
        }

        throw new SqlParseException($"Expected connection, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ParseCursorName()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected cursor, found {_current.Kind}", _current.Position);
    }

    private bool LooksLikeCursor(string start)
    {
        if (!IdentifierEquals(start))
        {
            return false;
        }

        var index = _index;
        if (!IsCursorNameAt(index))
        {
            return false;
        }

        index++;
        if (IsSensitivityAt(index))
        {
            index++;
        }

        if (IsKeywordAt(index, Keyword.No) && IsKeywordAt(index + 1, Keyword.Scroll))
        {
            index++;
            index++;
        }
        else if (IsKeywordAt(index, Keyword.Scroll))
        {
            index++;
        }

        return IsKeywordAt(index, Keyword.Cursor);
    }

    private bool IsCursorQueryStart() =>
        _current.Kind is SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword or SyntaxKind.ValuesKeyword or SyntaxKind.OpenParen;

    private bool IsSensitivity() =>
        IdentifierEquals(Keyword.Sensitive)
        || IdentifierEquals(Keyword.Insensitive)
        || IdentifierEquals(Keyword.Asensitive);

    private bool IsSensitivityAt(int index) =>
        IsKeywordAt(index, Keyword.Sensitive)
        || IsKeywordAt(index, Keyword.Insensitive)
        || IsKeywordAt(index, Keyword.Asensitive);

    private bool IsAbsoluteOrRelative() =>
        IdentifierEquals(Keyword.Absolute) || IdentifierEquals(Keyword.Relative);

    private bool IsOrientationFrom() =>
        (IdentifierEquals(Keyword.Next) || IdentifierEquals(Keyword.Prior)
            || IdentifierEquals(Keyword.First) || IdentifierEquals(Keyword.Last))
        && NextKind == SyntaxKind.FromKeyword;

    private bool IsWithWord(string word) =>
        (_current.Kind == SyntaxKind.WithKeyword && NextEquals(word))
        || (IdentifierEquals(Keyword.Without) && NextEquals(word));

    private bool IsCursorNameAt(int index) =>
        index < _tokens.Count
        && _tokens[index].Kind is SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost;

    private bool IsKeywordAt(int index, string keyword) =>
        index < _tokens.Count && TokenEquals(_tokens[index], keyword);
}
