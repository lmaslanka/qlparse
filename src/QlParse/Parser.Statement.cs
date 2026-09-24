namespace QlParse;

internal sealed partial class Parser
{
    private const int Len2 = 2;
    private const int Len3 = 3;
    private const int Len4 = 4;
    private const int Len5 = 5;
    private const int Len6 = 6;
    private const int Len7 = 7;
    private const int Len8 = 8;
    private const int Len9 = 9;
    private const int Len10 = 10;
    private const int Len11 = 11;
    private const int Len12 = 12;

    private Query ParseStatement()
    {
        var labeled = ParseLabeledStatement();
        if (labeled is not null)
        {
            return labeled;
        }

        return _current.Kind switch
        {
            SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword or SyntaxKind.ValuesKeyword or SyntaxKind.OpenParen
                => ParseQuery(),
            SyntaxKind.UpdateKeyword => ParseUpdate(),
            SyntaxKind.SetKeyword => ParseSetStatement(),
            SyntaxKind.FetchKeyword => ParseFetchStatement(),
            SyntaxKind.CaseKeyword => ParseCaseStatement(),
            SyntaxKind.ForKeyword => ParseForStatement(),
            SyntaxKind.EndKeyword => IsDeclareSection() ? ParseDeclareSection() : ParseQuery(),
            SyntaxKind.Identifier => ParseIdentifierStatement(),
            _ => ParseQuery(),
        };
    }

    private Query? ParseLabeledStatement()
    {
        if (_current.Kind != SyntaxKind.Identifier || NextKind != SyntaxKind.ColonToken)
        {
            return null;
        }

        var verbIndex = _index + 1;
        if (verbIndex >= _tokens.Count)
        {
            return null;
        }

        var verb = _tokens[verbIndex];
        if (verb.Kind == SyntaxKind.ForKeyword)
        {
            return ParseForStatement();
        }

        if (verb.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        var text = verb.TextOf(_source);
        if (TextEquals(text, Keyword.Begin))
        {
            return ParseCompound();
        }

        if (TextEquals(text, Keyword.Loop))
        {
            return ParseLoop();
        }

        if (TextEquals(text, Keyword.While))
        {
            return ParseWhile();
        }

        if (TextEquals(text, Keyword.Repeat))
        {
            return ParseRepeatStatement();
        }

        return null;
    }

    private Query ParseIdentifierStatement()
    {
        var text = _current.TextOf(_source);
        switch (text.Length)
        {
            case Len2:
                if (TextEquals(text, Keyword.If))
                {
                    return ParseIf();
                }

                break;
            case Len3:
                if (TextEquals(text, Keyword.Get))
                {
                    return ParseGetDiagnostics();
                }

                break;
            case Len4:
                if (TextEquals(text, Keyword.Drop))
                {
                    return ParseDropStatement();
                }

                if (TextEquals(text, Keyword.Open))
                {
                    return ParseOpen();
                }

                if (TextEquals(text, Keyword.Call))
                {
                    return ParseCall();
                }

                if (TextEquals(text, Keyword.Loop))
                {
                    return ParseLoop();
                }

                if (TextEquals(text, Keyword.Exec))
                {
                    return ParseEmbeddedSql();
                }

                break;
            case Len5:
                if (TextEquals(text, Keyword.Alter))
                {
                    return ParseAlterStatement();
                }

                if (TextEquals(text, Keyword.Merge))
                {
                    return ParseMerge();
                }

                if (TextEquals(text, Keyword.Grant))
                {
                    return ParseGrant();
                }

                if (TextEquals(text, Keyword.Start))
                {
                    return ParseStartTransaction();
                }

                if (TextEquals(text, Keyword.Close))
                {
                    return ParseClose();
                }

                if (TextEquals(text, Keyword.Begin))
                {
                    return IsDeclareSection() ? ParseDeclareSection() : ParseCompound();
                }

                if (TextEquals(text, Keyword.While))
                {
                    return ParseWhile();
                }

                if (TextEquals(text, Keyword.Leave))
                {
                    return ParseLeave();
                }

                break;
            case Len6:
                if (TextEquals(text, Keyword.Create))
                {
                    return ParseCreateStatement();
                }

                if (TextEquals(text, Keyword.Insert))
                {
                    return ParseInsert();
                }

                if (TextEquals(text, Keyword.Delete))
                {
                    return ParseDelete();
                }

                if (TextEquals(text, Keyword.Commit))
                {
                    return ParseCommit();
                }

                if (TextEquals(text, Keyword.Return))
                {
                    return ParseReturn();
                }

                if (TextEquals(text, Keyword.Module))
                {
                    return ParseModule();
                }

                if (TextEquals(text, Keyword.Signal))
                {
                    return ParseSignal();
                }

                if (TextEquals(text, Keyword.Repeat))
                {
                    return ParseRepeatStatement();
                }

                if (TextEquals(text, Keyword.Revoke))
                {
                    return ParseRevoke();
                }

                break;
            case Len7:
                if (TextEquals(text, Keyword.Connect))
                {
                    return ParseConnect();
                }

                if (TextEquals(text, Keyword.Declare))
                {
                    return ParseDeclareStatement();
                }

                if (TextEquals(text, Keyword.Prepare))
                {
                    return ParsePrepare();
                }

                if (TextEquals(text, Keyword.Execute))
                {
                    return NextEquals(Keyword.Immediate) ? ParseExecuteImmediate() : ParseExecute();
                }

                if (TextEquals(text, Keyword.Iterate))
                {
                    return ParseIterate();
                }

                if (TextEquals(text, Keyword.Comment))
                {
                    return ParseComment();
                }

                if (TextEquals(text, Keyword.Release))
                {
                    return ParseReleaseSavepoint();
                }

                break;
            case Len8:
                if (TextEquals(text, Keyword.Truncate))
                {
                    return ParseTruncate();
                }

                if (TextEquals(text, Keyword.Rollback))
                {
                    return ParseRollback();
                }

                if (TextEquals(text, Keyword.Allocate))
                {
                    return ParseAllocateStatement();
                }

                if (TextEquals(text, Keyword.Describe))
                {
                    return ParseDescribe();
                }

                if (TextEquals(text, Keyword.Whenever))
                {
                    return ParseWhenever();
                }

                if (TextEquals(text, Keyword.Resignal))
                {
                    return ParseResignal();
                }

                break;
            case Len9:
                if (TextEquals(text, Keyword.Savepoint))
                {
                    return ParseSavepoint();
                }

                break;
            case Len10:
                if (TextEquals(text, Keyword.Disconnect))
                {
                    return ParseDisconnect();
                }

                if (TextEquals(text, Keyword.Deallocate))
                {
                    return ParseDeallocate();
                }

                break;
        }

        return ParseQuery();
    }

    private Query ParseCreateStatement()
    {
        var nextKind = NextKind;
        if (nextKind == SyntaxKind.CastKeyword)
        {
            return ParseCreateCast();
        }

        if (nextKind == SyntaxKind.RecursiveKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.View) ? ParseCreateView() : ParseQuery();
        }

        if (nextKind == SyntaxKind.UniqueKeyword)
        {
            return IsCreateIndexTail() ? ParseCreateIndex() : ParseQuery();
        }

        if (nextKind != SyntaxKind.Identifier)
        {
            return ParseQuery();
        }

        var text = _tokens[_index].TextOf(_source);
        switch (text.Length)
        {
            case Len4:
                if (TextEquals(text, Keyword.View))
                {
                    return ParseCreateView();
                }

                if (TextEquals(text, Keyword.Type))
                {
                    return ParseCreateType();
                }

                if (TextEquals(text, Keyword.Role))
                {
                    return ParseCreateRole();
                }

                break;
            case Len5:
                if (TextEquals(text, Keyword.Table) || TextEquals(text, Keyword.Local))
                {
                    return ParseCreateTable();
                }

                if (TextEquals(text, Keyword.Index))
                {
                    return ParseCreateIndex();
                }

                break;
            case Len6:
                if (TextEquals(text, Keyword.Schema))
                {
                    return ParseCreateSchema();
                }

                if (TextEquals(text, Keyword.Global))
                {
                    return ParseCreateTable();
                }

                if (TextEquals(text, Keyword.Domain))
                {
                    return ParseCreateDomain();
                }

                if (TextEquals(text, Keyword.Method))
                {
                    return ParseCreateMethod();
                }

                if (TextEquals(text, Keyword.Static) && IsKeywordAt(_index + 1, Keyword.Method))
                {
                    return ParseCreateMethod();
                }

                break;
            case Len7:
                if (TextEquals(text, Keyword.Trigger))
                {
                    return ParseCreateTrigger();
                }

                break;
            case Len8:
                if (TextEquals(text, Keyword.Ordering))
                {
                    return ParseCreateOrdering();
                }

                if (TextEquals(text, Keyword.Sequence))
                {
                    return ParseCreateSequence();
                }

                if (TextEquals(text, Keyword.Function))
                {
                    return ParseCreateFunction();
                }

                if (TextEquals(text, Keyword.Instance) && IsKeywordAt(_index + 1, Keyword.Method))
                {
                    return ParseCreateMethod();
                }

                if (TextEquals(text, Keyword.Property))
                {
                    return ParseCreatePropertyGraph();
                }

                break;
            case Len9:
                if (TextEquals(text, Keyword.Transform))
                {
                    return ParseCreateTransform();
                }

                if (TextEquals(text, Keyword.Assertion))
                {
                    return ParseCreateAssertion();
                }

                if (TextEquals(text, Keyword.Character) && NextNextIsSet())
                {
                    return ParseCreateCharacterSet();
                }

                if (TextEquals(text, Keyword.Collation))
                {
                    return ParseCreateCollation();
                }

                if (TextEquals(text, Keyword.Procedure))
                {
                    return ParseCreateProcedure();
                }

                if (TextEquals(text, Keyword.Clustered) && IsCreateIndexTail())
                {
                    return ParseCreateIndex();
                }

                break;
            case Len10:
                if (TextEquals(text, Keyword.Transforms))
                {
                    return ParseCreateTransform();
                }

                break;
            case Len11:
                if (TextEquals(text, Keyword.Translation))
                {
                    return ParseCreateTranslation();
                }

                if (TextEquals(text, Keyword.Constructor) && IsKeywordAt(_index + 1, Keyword.Method))
                {
                    return ParseCreateMethod();
                }

                break;
            case Len12:
                if (TextEquals(text, Keyword.Nonclustered) && IsCreateIndexTail())
                {
                    return ParseCreateIndex();
                }

                break;
        }

        return ParseQuery();
    }

    private Query ParseAlterStatement()
    {
        if (IsRoutineDesignatorNext())
        {
            return ParseAlterRoutine();
        }

        if (NextKind != SyntaxKind.Identifier)
        {
            return ParseQuery();
        }

        var text = _tokens[_index].TextOf(_source);
        switch (text.Length)
        {
            case Len4:
                if (TextEquals(text, Keyword.View))
                {
                    return ParseAlterView();
                }

                break;
            case Len5:
                if (TextEquals(text, Keyword.Table))
                {
                    return ParseAlterTable();
                }

                if (TextEquals(text, Keyword.Index))
                {
                    return ParseAlterIndex();
                }

                break;
            case Len6:
                if (TextEquals(text, Keyword.Schema))
                {
                    return ParseAlterSchema();
                }

                if (TextEquals(text, Keyword.Domain))
                {
                    return ParseAlterDomain();
                }

                break;
        }

        return ParseQuery();
    }

    private Query ParseDropStatement()
    {
        if (IsRoutineDesignatorNext())
        {
            return ParseDropRoutine();
        }

        if (NextKind != SyntaxKind.Identifier)
        {
            return ParseQuery();
        }

        var text = _tokens[_index].TextOf(_source);
        switch (text.Length)
        {
            case Len4:
                if (TextEquals(text, Keyword.View))
                {
                    return ParseDropView();
                }

                if (TextEquals(text, Keyword.Type))
                {
                    return ParseDropType();
                }

                if (TextEquals(text, Keyword.Role))
                {
                    return ParseDropRole();
                }

                break;
            case Len5:
                if (TextEquals(text, Keyword.Table))
                {
                    return ParseDropTable();
                }

                if (TextEquals(text, Keyword.Index))
                {
                    return ParseDropIndex();
                }

                break;
            case Len6:
                if (TextEquals(text, Keyword.Schema))
                {
                    return ParseDropSchema();
                }

                if (TextEquals(text, Keyword.Domain))
                {
                    return ParseDropDomain();
                }

                break;
            case Len7:
                if (TextEquals(text, Keyword.Trigger))
                {
                    return ParseDropTrigger();
                }

                break;
            case Len8:
                if (TextEquals(text, Keyword.Sequence))
                {
                    return ParseDropSequence();
                }

                if (TextEquals(text, Keyword.Property))
                {
                    return ParseDropPropertyGraph();
                }

                break;
            case Len9:
                if (TextEquals(text, Keyword.Assertion))
                {
                    return ParseDropAssertion();
                }

                if (TextEquals(text, Keyword.Character) && NextNextIsSet())
                {
                    return ParseDropCharacterSet();
                }

                if (TextEquals(text, Keyword.Collation))
                {
                    return ParseDropCollation();
                }

                break;
            case Len11:
                if (TextEquals(text, Keyword.Translation))
                {
                    return ParseDropTranslation();
                }

                break;
        }

        return ParseQuery();
    }

    private Query ParseSetStatement()
    {
        if (NextKind == SyntaxKind.TimeKeyword
            && _index + 1 < _tokens.Count
            && TokenEquals(_tokens[_index + 1], Keyword.Zone))
        {
            return ParseSetTimeZone();
        }

        if (NextKind != SyntaxKind.Identifier)
        {
            return ParseSetAssignment();
        }

        var text = _tokens[_index].TextOf(_source);
        switch (text.Length)
        {
            case Len4:
                if (TextEquals(text, Keyword.Role))
                {
                    return ParseSetRole();
                }

                if (TextEquals(text, Keyword.Path))
                {
                    return ParseSetPath();
                }

                break;
            case Len5:
                if (TextEquals(text, Keyword.Names))
                {
                    return ParseSetNames();
                }

                if (TextEquals(text, Keyword.Local)
                    && _index + 1 < _tokens.Count
                    && TokenEquals(_tokens[_index + 1], Keyword.Transaction))
                {
                    return ParseSetTransaction();
                }

                break;
            case Len6:
                if (TextEquals(text, Keyword.Schema))
                {
                    return ParseSetSchema();
                }

                break;
            case Len7:
                if (TextEquals(text, Keyword.Session))
                {
                    return ParseSetSession();
                }

                if (TextEquals(text, Keyword.Catalog))
                {
                    return ParseSetCatalog();
                }

                break;
            case Len9:
                if (TextEquals(text, Keyword.Collation))
                {
                    return ParseSetCollation();
                }

                if (TextEquals(text, Keyword.Character) && NextNextIsSet())
                {
                    return ParseSetCharacterSet();
                }

                break;
            case Len10:
                if (TextEquals(text, Keyword.Connection))
                {
                    return ParseSetConnection();
                }

                break;
            case Len11:
                if (TextEquals(text, Keyword.Transaction))
                {
                    return ParseSetTransaction();
                }

                if (TextEquals(text, Keyword.Constraints))
                {
                    return ParseSetConstraints();
                }

                break;
        }

        return ParseSetAssignment();
    }

    private Query ParseDeclareStatement()
    {
        if (IsDeclareCursor())
        {
            return ParseDeclareCursor();
        }

        if (NextKind == SyntaxKind.Identifier)
        {
            var text = _tokens[_index].TextOf(_source);
            if (text.Length == Len4 && (TextEquals(text, Keyword.Exit) || TextEquals(text, Keyword.Undo))
                || text.Length == Len8 && TextEquals(text, Keyword.Continue))
            {
                return ParseDeclareHandler();
            }
        }

        return ParseDeclareVariable();
    }

    private Query ParseAllocateStatement()
    {
        if (LooksLikeCursor(Keyword.Allocate))
        {
            return ParseAllocateCursor();
        }

        var index = _index;
        if (IsKeywordAt(index, Keyword.Sql))
        {
            index++;
        }

        if (IsKeywordAt(index, Keyword.Descriptor))
        {
            return ParseAllocateDescriptor();
        }

        return ParseQuery();
    }

    private bool IsCreateIndexTail()
    {
        var index = _index;
        if (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.UniqueKeyword)
        {
            index++;
        }

        if (index < _tokens.Count
            && (TokenEquals(_tokens[index], Keyword.Clustered) || TokenEquals(_tokens[index], Keyword.Nonclustered)))
        {
            index++;
        }

        return index < _tokens.Count && TokenEquals(_tokens[index], Keyword.Index);
    }

    private bool IsRoutineDesignatorNext()
    {
        if (NextKind != SyntaxKind.Identifier)
        {
            return false;
        }

        var text = _tokens[_index].TextOf(_source);
        return text.Length switch
        {
            Len6 => TextEquals(text, Keyword.Method) || TextEquals(text, Keyword.Static),
            Len7 => TextEquals(text, Keyword.Routine),
            Len8 => TextEquals(text, Keyword.Function)
                || TextEquals(text, Keyword.Specific)
                || TextEquals(text, Keyword.Instance),
            Len9 => TextEquals(text, Keyword.Procedure),
            Len11 => TextEquals(text, Keyword.Constructor),
            _ => false,
        };
    }

    private static bool TextEquals(ReadOnlySpan<char> text, string keyword) =>
        text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);
}
