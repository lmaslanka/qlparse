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

    internal Query ParseStatement()
    {
        var labeled = ParseLabeledStatement();
        return labeled ?? ParseStatementByKind();
    }

    private Query ParseStatementByKind() => _current.Kind switch
    {
        SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword or SyntaxKind.ValuesKeyword or SyntaxKind.OpenParen
            => ParseQuery(),
        SyntaxKind.UpdateKeyword => ParseUpdate(),
        SyntaxKind.SetKeyword => ParseSetStatement(),
        SyntaxKind.FetchKeyword => ParseFetchStatement(),
        SyntaxKind.CaseKeyword => ParseCaseStatement(),
        SyntaxKind.ForKeyword => ParseForStatement(),
        SyntaxKind.EndKeyword => ParseEndKeywordStatement(),
        SyntaxKind.Identifier => ParseIdentifierStatement(),
        _ => ParseQuery(),
    };

    private Query ParseEndKeywordStatement() => IsDeclareSection() ? ParseDeclareSection() : ParseQuery();

    private Query? ParseLabeledStatement()
    {
        if (!IsLabeled())
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
        return text.Length <= Len6 ? ParseIdentLowLength(text) : ParseIdentHighLength(text);
    }

    private Query ParseIdentLowLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len2 => TryParseIdentLen2(text),
        Len3 => TryParseIdentLen3(text),
        Len4 => TryParseIdentLen4(text),
        Len5 => TryParseIdentLen5(text),
        Len6 => TryParseIdentLen6(text),
        _ => ParseQuery(),
    };

    private Query ParseIdentHighLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len7 => TryParseIdentLen7(text),
        Len8 => TryParseIdentLen8(text),
        Len9 => TryParseIdentLen9(text),
        Len10 => TryParseIdentLen10(text),
        _ => ParseQuery(),
    };

    private Query TryParseIdentLen2(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.If))
        {
            return ParseIf();
        }

        return ParseQuery();
    }

    private Query TryParseIdentLen3(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Get))
        {
            return ParseGetDiagnostics();
        }

        return ParseQuery();
    }

    private Query TryParseIdentLen4(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseIdentLen5(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseIdentLen6(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseIdentLen7(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseIdentLen8(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseIdentLen9(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Savepoint))
        {
            return ParseSavepoint();
        }

        return ParseQuery();
    }

    private Query TryParseIdentLen10(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Disconnect))
        {
            return ParseDisconnect();
        }

        if (TextEquals(text, Keyword.Deallocate))
        {
            return ParseDeallocate();
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
        return text.Length <= Len8 ? ParseCreateLowLength(text) : ParseCreateHighLength(text);
    }

    private Query ParseCreateLowLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len4 => TryParseCreateLen4(text),
        Len5 => TryParseCreateLen5(text),
        Len6 => TryParseCreateLen6(text),
        Len7 => TryParseCreateLen7(text),
        Len8 => TryParseCreateLen8(text),
        _ => ParseQuery(),
    };

    private Query ParseCreateHighLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len9 => TryParseCreateLen9(text),
        Len10 => TryParseCreateLen10(text),
        Len11 => TryParseCreateLen11(text),
        Len12 => TryParseCreateLen12(text),
        _ => ParseQuery(),
    };

    private Query TryParseCreateLen4(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseCreateLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Table))
        {
            return ParseCreateTable();
        }

        if (TextEquals(text, Keyword.Local))
        {
            return IsTemporaryTableAfterScope() ? ParseCreateTable() : ParseQuery();
        }

        if (TextEquals(text, Keyword.Index))
        {
            return ParseCreateIndex();
        }

        return ParseQuery();
    }

    private Query TryParseCreateLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Schema))
        {
            return ParseCreateSchema();
        }

        if (TextEquals(text, Keyword.Global))
        {
            return IsTemporaryTableAfterScope() ? ParseCreateTable() : ParseQuery();
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

        return ParseQuery();
    }

    private Query TryParseCreateLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Trigger))
        {
            return ParseCreateTrigger();
        }

        return ParseQuery();
    }

    private Query TryParseCreateLen8(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseCreateLen9(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseCreateLen10(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Transforms))
        {
            return ParseCreateTransform();
        }

        return ParseQuery();
    }

    private Query TryParseCreateLen11(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Translation))
        {
            return ParseCreateTranslation();
        }

        if (TextEquals(text, Keyword.Constructor) && IsKeywordAt(_index + 1, Keyword.Method))
        {
            return ParseCreateMethod();
        }

        return ParseQuery();
    }

    private Query TryParseCreateLen12(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Nonclustered) && IsCreateIndexTail())
        {
            return ParseCreateIndex();
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
        return text.Length switch
        {
            Len4 => TryParseAlterLen4(text),
            Len5 => TryParseAlterLen5(text),
            Len6 => TryParseAlterLen6(text),
            _ => ParseQuery(),
        };
    }

    private Query TryParseAlterLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.View))
        {
            return ParseAlterView();
        }

        return ParseQuery();
    }

    private Query TryParseAlterLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Table))
        {
            return ParseAlterTable();
        }

        if (TextEquals(text, Keyword.Index))
        {
            return ParseAlterIndex();
        }

        return ParseQuery();
    }

    private Query TryParseAlterLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Schema))
        {
            return ParseAlterSchema();
        }

        if (TextEquals(text, Keyword.Domain))
        {
            return ParseAlterDomain();
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

        return ParseDropByLength(_tokens[_index].TextOf(_source));
    }

    private Query ParseDropByLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len4 => TryParseDropLen4(text),
        Len5 => TryParseDropLen5(text),
        Len6 => TryParseDropLen6(text),
        Len7 => TryParseDropLen7(text),
        Len8 => TryParseDropLen8(text),
        Len9 => TryParseDropLen9(text),
        Len11 => TryParseDropLen11(text),
        _ => ParseQuery(),
    };

    private Query TryParseDropLen4(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseDropLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Table))
        {
            return ParseDropTable();
        }

        if (TextEquals(text, Keyword.Index))
        {
            return ParseDropIndex();
        }

        return ParseQuery();
    }

    private Query TryParseDropLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Schema))
        {
            return ParseDropSchema();
        }

        if (TextEquals(text, Keyword.Domain))
        {
            return ParseDropDomain();
        }

        return ParseQuery();
    }

    private Query TryParseDropLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Trigger))
        {
            return ParseDropTrigger();
        }

        return ParseQuery();
    }

    private Query TryParseDropLen8(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Sequence))
        {
            return ParseDropSequence();
        }

        if (TextEquals(text, Keyword.Property))
        {
            return ParseDropPropertyGraph();
        }

        return ParseQuery();
    }

    private Query TryParseDropLen9(ReadOnlySpan<char> text)
    {
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

        return ParseQuery();
    }

    private Query TryParseDropLen11(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Translation))
        {
            return ParseDropTranslation();
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

        return ParseSetByLength(_tokens[_index].TextOf(_source));
    }

    private Query ParseSetByLength(ReadOnlySpan<char> text) => text.Length switch
    {
        Len4 => TryParseSetLen4(text),
        Len5 => TryParseSetLen5(text),
        Len6 => TryParseSetLen6(text),
        Len7 => TryParseSetLen7(text),
        Len9 => TryParseSetLen9(text),
        Len10 => TryParseSetLen10(text),
        Len11 => TryParseSetLen11(text),
        _ => ParseSetAssignment(),
    };

    private Query TryParseSetLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Role))
        {
            return ParseSetRole();
        }

        if (TextEquals(text, Keyword.Path))
        {
            return ParseSetPath();
        }

        return ParseSetAssignment();
    }

    private Query TryParseSetLen5(ReadOnlySpan<char> text)
    {
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

        return ParseSetAssignment();
    }

    private Query TryParseSetLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Schema))
        {
            return ParseSetSchema();
        }

        return ParseSetAssignment();
    }

    private Query TryParseSetLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Session))
        {
            return ParseSetSession();
        }

        if (TextEquals(text, Keyword.Catalog))
        {
            return ParseSetCatalog();
        }

        return ParseSetAssignment();
    }

    private Query TryParseSetLen9(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Collation))
        {
            return ParseSetCollation();
        }

        if (TextEquals(text, Keyword.Character) && NextNextIsSet())
        {
            return ParseSetCharacterSet();
        }

        return ParseSetAssignment();
    }

    private Query TryParseSetLen10(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Connection))
        {
            return ParseSetConnection();
        }

        return ParseSetAssignment();
    }

    private Query TryParseSetLen11(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Transaction))
        {
            return ParseSetTransaction();
        }

        if (TextEquals(text, Keyword.Constraints))
        {
            return ParseSetConstraints();
        }

        return ParseSetAssignment();
    }

    private Query ParseDeclareStatement()
    {
        if (IsDeclareCursor())
        {
            return ParseDeclareCursor();
        }

        if (NextKind == SyntaxKind.Identifier && IsDeclareHandlerKeyword(_tokens[_index].TextOf(_source)))
        {
            return ParseDeclareHandler();
        }

        return ParseDeclareVariable();
    }

    private static bool IsDeclareHandlerKeyword(ReadOnlySpan<char> text) =>
        text.Length == Len4 && (TextEquals(text, Keyword.Exit) || TextEquals(text, Keyword.Undo))
            || text.Length == Len8 && TextEquals(text, Keyword.Continue);

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

    private bool IsTemporaryTableAfterScope()
    {
        var afterScope = _index + 1;
        return IsKeywordAt(afterScope, Keyword.Temporary) && IsKeywordAt(afterScope + 1, Keyword.Table);
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
            Len6 => IsMethodOrStatic(text),
            Len7 => TextEquals(text, Keyword.Routine),
            Len8 => IsFunctionSpecificOrInstance(text),
            Len9 => TextEquals(text, Keyword.Procedure),
            Len11 => TextEquals(text, Keyword.Constructor),
            _ => false,
        };
    }

    private static bool IsMethodOrStatic(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.Method) || TextEquals(text, Keyword.Static);

    private static bool IsFunctionSpecificOrInstance(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.Function) || TextEquals(text, Keyword.Specific) || TextEquals(text, Keyword.Instance);

    private static bool TextEquals(ReadOnlySpan<char> text, string keyword) =>
        text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);
}
