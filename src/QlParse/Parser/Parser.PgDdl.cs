namespace QlParse;

internal sealed partial class Parser
{
    // Workstream D: tables & storage DDL. Claims only statements the ISO dispatch cannot reach
    // (CREATE TEMP/UNLOGGED TABLE, CREATE OR REPLACE VIEW, materialized views, tablespaces, rules,
    // REFRESH, REINDEX, ALTER SEQUENCE), Postgres clauses of ISO statements are parsed inline.
    internal Query? TryParsePgDdlStatement()
    {
        if (_current.Kind != SyntaxKind.Identifier || _index >= _tokens.Count)
        {
            return null;
        }

        var text = _current.TextOf(_source);
        return text.Length switch
        {
            Len4 => TextEquals(text, Keyword.Drop) ? TryParsePgDdlDrop() : null,
            Len5 => TextEquals(text, Keyword.Alter) ? TryParsePgDdlAlter() : null,
            Len6 => TextEquals(text, Keyword.Create) ? TryParsePgDdlCreate() : null,
            Len7 => TryParsePgDdlVerb(text),
            _ => null,
        };
    }

    private Query? TryParsePgDdlVerb(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Reindex))
        {
            return ParseReindex();
        }

        if (TextEquals(text, Keyword.Refresh) && NextEquals(Keyword.DdlMaterialized))
        {
            return ParseRefreshMaterializedView();
        }

        return null;
    }

    private Query? TryParsePgDdlCreate()
    {
        var next = _tokens[_index];
        if (next.Kind == SyntaxKind.OrKeyword)
        {
            return TryParsePgCreateOrReplace();
        }

        if (next.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        var text = next.TextOf(_source);
        if (TextEquals(text, Keyword.DdlTemp) || TextEquals(text, Keyword.Temporary))
        {
            return TryParsePgCreateTemporary(_index + 1);
        }

        if (TextEquals(text, Keyword.Unlogged))
        {
            return TryParsePgCreateUnlogged();
        }

        return TryParsePgCreateObject(text);
    }

    private Query? TryParsePgCreateObject(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.DdlMaterialized) && IsKeywordAt(_index + 1, Keyword.View))
        {
            return ParseCreateMaterializedView();
        }

        if (TextEquals(text, Keyword.DdlTablespace))
        {
            return ParseCreateTablespace();
        }

        if (TextEquals(text, Keyword.DdlRule))
        {
            return ParseCreateRule();
        }

        return null;
    }

    private Query? TryParsePgCreateOrReplace()
    {
        var index = _index + 1;
        if (!IsKeywordAt(index, Keyword.DdlReplace))
        {
            return null;
        }

        index++;
        if (IsKeywordAt(index, Keyword.DdlRule))
        {
            return ParseCreateRule();
        }

        if (IsKeywordAt(index, Keyword.DdlTemp) || IsKeywordAt(index, Keyword.Temporary))
        {
            index++;
        }

        return IsViewAt(index) ? ParseCreateView() : null;
    }

    private Query? TryParsePgCreateTemporary(int index)
    {
        if (IsKeywordAt(index, Keyword.Table))
        {
            return ParseCreateTable();
        }

        if (IsKeywordAt(index, Keyword.Sequence))
        {
            return ParseCreateSequence();
        }

        return IsViewAt(index) ? ParseCreateView() : null;
    }

    private Query? TryParsePgCreateUnlogged()
    {
        var index = _index + 1;
        if (IsKeywordAt(index, Keyword.Table))
        {
            return ParseCreateTable();
        }

        return IsKeywordAt(index, Keyword.Sequence) ? ParseCreateSequence() : null;
    }

    private bool IsViewAt(int index)
    {
        var recursive = index < _tokens.Count && _tokens[index].Kind == SyntaxKind.RecursiveKeyword;
        return IsKeywordAt(recursive ? index + 1 : index, Keyword.View);
    }

    private Query? TryParsePgDdlAlter()
    {
        if (NextEquals(Keyword.Sequence))
        {
            return ParseAlterSequence();
        }

        if (NextEquals(Keyword.DdlTablespace))
        {
            return ParseAlterTablespace();
        }

        return IsMaterializedViewNext() ? ParseAlterView() : null;
    }

    private Query? TryParsePgDdlDrop()
    {
        if (NextEquals(Keyword.DdlTablespace))
        {
            return ParseDropTablespace();
        }

        if (NextEquals(Keyword.DdlRule))
        {
            return ParseDropRule();
        }

        return IsMaterializedViewNext() ? ParseDropView() : null;
    }

    private bool IsMaterializedViewNext() =>
        NextEquals(Keyword.DdlMaterialized) && IsKeywordAt(_index + 1, Keyword.View);
}
