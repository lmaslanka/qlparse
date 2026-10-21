namespace QlParse.Tests;

// PL/pgSQL bodies (stream G), parsed eagerly from dollar-quoted DO / function bodies. Every span is an
// absolute position in the full source. SQL holding semicolons is kept in constants split so that no
// source line carries two of them.
public sealed class PlpgsqlTests
{
    private const int TwoItems = 2;
    private const int ThreeItems = 3;
    private const int FourItems = 4;
    private const int FiveItems = 5;
    private const int Third = 2;
    private const int Fourth = 3;
    private const int Fifth = 4;
    private const string LoopLabel = "outer";
    private const string EqualsSign = "=";
    private const string NoWord = "no";
    private const string Two = "2";
    private const string One = "1";
    private const string StrictWord = "strict";

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Text(string sql, SyntaxToken token) => Text(sql, token.Span);

    private static string Text(string sql, SyntaxToken? token) => token is { } value ? Text(sql, value) : string.Empty;

    private static int At(string sql, string text)
    {
        for (var position = 0; position + text.Length <= sql.Length; position++)
        {
            if (string.CompareOrdinal(sql, position, text, 0, text.Length) == 0)
            {
                return position;
            }
        }

        return -1;
    }

    private static PlpgsqlBlock Body(string sql)
    {
        var node = SqlAssert.ParsePg<PgDoStatement>(sql);
        Assert.Equal(sql, Text(sql, node.Span));
        new SqlVisitor().Visit(node);
        SqlAssert.ParseFails(sql);
        return Assert.IsType<PlpgsqlBlock>(node.Body);
    }

    private static T Statement<T>(string sql, int index = 0) where T : Query =>
        Assert.IsType<T>(Body(sql).Statements[index]);

    // Asserts the node starts at `text` in the full source and returns its source text.
    private static string Located(string sql, Query node, string text)
    {
        Assert.Equal(At(sql, text), node.Span.Position);
        return Text(sql, node.Span);
    }

    // ---- Blocks ----

    private const string BlockSql =
        "do $$ <<outer>> declare a int"
        + "; begin <<inner>> begin null"
        + "; exception when division_by_zero or sqlstate '22012' then null"
        + "; when others then raise"
        + "; end inner"
        + "; end outer $$"
        ;

    [Fact]
    public void Parses_labeled_blocks_with_exception_handlers()
    {
        var block = Body(BlockSql);
        Assert.Equal(LoopLabel, Text(BlockSql, block.Label!.Name));
        Assert.Equal("<<outer>>", Text(BlockSql, block.Label.Span));
        Assert.Equal(LoopLabel, Text(BlockSql, block.EndLabel));
        Assert.IsType<PlpgsqlVariableDeclaration>(Assert.Single(block.Declarations));
        var inner = Assert.IsType<PlpgsqlBlock>(Assert.Single(block.Statements));
        Assert.StartsWith("<<inner>> begin null", Located(BlockSql, inner, "<<inner>>"));
        Assert.EndsWith("end inner", Text(BlockSql, inner.Span));
        Assert.Equal(TwoItems, inner.Handlers.Count);
        var first = inner.Handlers[0];
        Assert.Equal(TwoItems, first.Conditions.Count);
        Assert.Equal("sqlstate '22012'", Text(BlockSql, first.Conditions[1].Span));
        Assert.Equal("others", Text(BlockSql, inner.Handlers[1].Conditions[0].Name));
        Assert.IsType<PlpgsqlRaiseStatement>(Assert.Single(inner.Handlers[1].Statements));
    }

    private const string EmptyBlockSql = "do $$ begin end $$";

    [Fact]
    public void Parses_an_empty_block_only_under_the_postgres_flag()
    {
        Assert.Empty(Body(EmptyBlockSql).Statements);
        SqlAssert.ParseFails(EmptyBlockSql);
    }

    // ---- Declarations ----

    private const string DeclarationsSql =
        "do $$ declare a alias for $1"
        + "; b constant int not null := 1"
        + @"; c text collate ""C"" default 'x'"
        + "; d t.col%type = 2"
        + "; e t%rowtype"
        + "; f refcursor"
        + "; g int[]"
        + "; begin null"
        + "; end $$"
        ;

    [Fact]
    public void Parses_variable_and_alias_declarations()
    {
        var declarations = Body(DeclarationsSql).Declarations;
        var alias = Assert.IsType<PlpgsqlAliasDeclaration>(declarations[0]);
        Assert.Equal("a alias for $1", Located(DeclarationsSql, alias, "a alias"));
        Assert.Equal(SyntaxKind.PositionalParameter, alias.Target.Kind);
        var constant = Assert.IsType<PlpgsqlVariableDeclaration>(declarations[1]);
        Assert.Equal("b constant int not null := 1", Text(DeclarationsSql, constant.Span));
        Assert.Equal(":=", Text(DeclarationsSql, constant.AssignmentToken));
        Assert.Equal("null", Text(DeclarationsSql, constant.NullKeyword));
        var collated = Assert.IsType<PlpgsqlVariableDeclaration>(declarations[Third]);
        Assert.Equal(@"""C""", Text(DeclarationsSql, collated.Collation[0]));
        Assert.Equal("default", Text(DeclarationsSql, collated.AssignmentToken));
        var typed = Assert.IsType<PlpgsqlVariableDeclaration>(declarations[Fourth]);
        Assert.Equal("t.col", Text(DeclarationsSql, typed.Type.Span));
        Assert.Equal("%type", Text(DeclarationsSql, typed.TypeAttribute!.Span));
        Assert.Equal(EqualsSign, Text(DeclarationsSql, typed.AssignmentToken));
        var row = Assert.IsType<PlpgsqlVariableDeclaration>(declarations[Fifth]);
        Assert.Equal("rowtype", Text(DeclarationsSql, row.TypeAttribute!.Attribute));
        Assert.Equal("refcursor", Text(DeclarationsSql, Assert.IsType<PlpgsqlVariableDeclaration>(declarations[^Third]).Type.Span));
        Assert.Single(Assert.IsType<PlpgsqlVariableDeclaration>(declarations[^1]).Type.Postgres!.ArrayBounds);
    }

    private const string CursorDeclarationsSql =
        "do $$ declare c1 cursor for select 1"
        + "; c2 no scroll cursor (k int, n text) for select * from t where id = k"
        + "; c3 scroll cursor is select 2"
        + "; begin null"
        + "; end $$"
        ;

    [Fact]
    public void Parses_cursor_declarations()
    {
        var declarations = Body(CursorDeclarationsSql).Declarations;
        var plain = Assert.IsType<PlpgsqlCursorDeclaration>(declarations[0]);
        Assert.Equal("c1 cursor for select 1", Located(CursorDeclarationsSql, plain, "c1"));
        Assert.IsType<SelectStatement>(plain.Query);
        var bound = Assert.IsType<PlpgsqlCursorDeclaration>(declarations[1]);
        Assert.Equal(NoWord, Text(CursorDeclarationsSql, bound.NoKeyword));
        Assert.Equal(TwoItems, bound.Arguments.Count);
        Assert.Equal("n text", Text(CursorDeclarationsSql, bound.Arguments[1].Span));
        var scroll = Assert.IsType<PlpgsqlCursorDeclaration>(declarations[Third]);
        Assert.Equal("is", Text(CursorDeclarationsSql, scroll.ForKeyword));
        Assert.Equal("scroll", Text(CursorDeclarationsSql, scroll.ScrollKeyword));
    }

    // ---- Assignment and trigger variables ----

    private const string TriggerSql =
        "create function audit() returns trigger language plpgsql as $$ begin if tg_op = 'DELETE' then return old"
        + "; end if"
        + "; new.updated_at := now()"
        + "; new.tags[1] = tg_argv[0]"
        + "; raise notice '% on %', tg_when, tg_table_name"
        + "; return new"
        + "; end $$"
        ;

    [Fact]
    public void Parses_trigger_function_special_variables()
    {
        var node = SqlAssert.ParsePg<CreateRoutineStatement>(TriggerSql);
        var statements = Assert.IsType<PlpgsqlBlock>(node.Body).Statements;
        var check = Assert.IsType<IfStatement>(statements[0]);
        Assert.Equal("tg_op = 'DELETE'", Text(TriggerSql, check.Condition.Span));
        var returnOld = Assert.IsType<PlpgsqlReturnStatement>(Assert.Single(check.ThenStatements));
        Assert.Equal("old", Text(TriggerSql, returnOld.Value!.Span));
        var stamp = Assert.IsType<PlpgsqlAssignment>(statements[1]);
        Assert.Equal("new.updated_at := now()", Located(TriggerSql, stamp, "new.updated_at"));
        Assert.Equal("new.updated_at", Text(TriggerSql, stamp.Target.Span));
        var element = Assert.IsType<PlpgsqlAssignment>(statements[Third]);
        Assert.Equal("new.tags[1]", Text(TriggerSql, element.Target.Span));
        Assert.Equal(EqualsSign, Text(TriggerSql, element.AssignmentToken));
        Assert.Equal("tg_argv[0]", Text(TriggerSql, element.Value.Span));
        var raise = Assert.IsType<PlpgsqlRaiseStatement>(statements[Fourth]);
        Assert.Equal(TwoItems, raise.Arguments.Count);
        Assert.Equal("new", Text(TriggerSql, Assert.IsType<PlpgsqlReturnStatement>(statements[^1]).Value!.Span));
        new SqlVisitor().Visit(node);
    }

    // ---- IF / CASE ----

    private const string IfSql =
        "do $$ begin if a then null"
        + "; elsif b then null"
        + "; elseif c then null"
        + "; else null"
        + "; end if"
        + "; end $$"
        ;

    [Fact]
    public void Parses_if_elsif_elseif_else()
    {
        var node = Statement<IfStatement>(IfSql);
        Assert.StartsWith("if a then", Located(IfSql, node, "if a"));
        Assert.EndsWith("end if", Text(IfSql, node.Span));
        Assert.Equal(TwoItems, node.ElseIfs.Count);
        Assert.Equal("elsif", Text(IfSql, node.ElseIfs[0].ElseIfKeyword));
        Assert.Equal("elseif", Text(IfSql, node.ElseIfs[1].ElseIfKeyword));
        Assert.IsType<PlpgsqlNullStatement>(Assert.Single(node.ElseStatements));
    }

    private const string CaseSql =
        "do $$ begin case x when 1, 2 then null"
        + "; when 3 then null"
        + "; else null"
        + "; end case"
        + "; case when x > 1 then null"
        + "; end case"
        + "; end $$"
        ;

    [Fact]
    public void Parses_simple_and_searched_case_statements()
    {
        var simple = Statement<CaseStatement>(CaseSql);
        Assert.StartsWith("case x when 1, 2", Located(CaseSql, simple, "case x"));
        Assert.Equal(Two, Text(CaseSql, Assert.Single(simple.Whens[0].AdditionalOperands).Span));
        Assert.Equal(TwoItems, simple.Whens.Count);
        var searched = Statement<CaseStatement>(CaseSql, 1);
        Assert.Null(searched.Operand);
        Assert.Equal("x > 1", Text(CaseSql, searched.Whens[0].Operand.Span));
    }

    // ---- Loops ----

    private const string LoopSql =
        "do $$ begin <<outer>> loop exit outer when x > 1"
        + "; continue"
        + "; exit"
        + "; end loop outer"
        + "; while x < 10 loop x := x + 1"
        + "; continue when x = 5"
        + "; end loop"
        + "; end $$"
        ;

    [Fact]
    public void Parses_loop_while_exit_and_continue()
    {
        var loop = Statement<PlpgsqlLoopStatement>(LoopSql);
        Assert.StartsWith("<<outer>> loop", Located(LoopSql, loop, "<<outer>>"));
        Assert.EndsWith("end loop outer", Text(LoopSql, loop.Span));
        Assert.Equal(LoopLabel, Text(LoopSql, loop.Body.EndLabel));
        Assert.Equal(ThreeItems, loop.Body.Statements.Count);
        var exit = Assert.IsType<PlpgsqlExitStatement>(loop.Body.Statements[0]);
        Assert.Equal("exit outer when x > 1", Text(LoopSql, exit.Span));
        Assert.Equal(LoopLabel, Text(LoopSql, exit.Label));
        var bareContinue = Assert.IsType<PlpgsqlExitStatement>(loop.Body.Statements[1]);
        Assert.Null(bareContinue.Label);
        Assert.Equal("continue", Text(LoopSql, bareContinue.Keyword));

        var loopWhile = Statement<PlpgsqlWhileStatement>(LoopSql, 1);
        Assert.Equal("x < 10", Text(LoopSql, loopWhile.Condition.Span));
        var continueWhen = Assert.IsType<PlpgsqlExitStatement>(loopWhile.Body.Statements[1]);
        Assert.Equal("x = 5", Text(LoopSql, continueWhen.Condition!.Span));
    }

    private const string RangeSql =
        "do $$ begin for i in 1..10 loop null"
        + "; end loop"
        + "; for j in reverse n * 2 .. lo by 2 loop null"
        + "; end loop"
        + "; end $$"
        ;

    [Fact]
    public void Parses_integer_for_loops()
    {
        var forward = Statement<PlpgsqlForRangeStatement>(RangeSql);
        Assert.StartsWith("for i in 1..10 loop", Located(RangeSql, forward, "for i"));
        Assert.Equal(One, Text(RangeSql, forward.Lower.Span));
        Assert.Equal("..", Text(RangeSql, forward.DotDot));
        Assert.Equal(SyntaxKind.DotDotToken, forward.DotDot.Kind);
        Assert.Equal("10", Text(RangeSql, forward.Upper.Span));
        var reverse = Statement<PlpgsqlForRangeStatement>(RangeSql, 1);
        Assert.Equal("reverse", Text(RangeSql, reverse.ReverseKeyword));
        Assert.Equal("n * 2", Text(RangeSql, reverse.Lower.Span));
        Assert.Equal("lo", Text(RangeSql, reverse.Upper.Span));
        Assert.Equal(Two, Text(RangeSql, reverse.Step!.Span));
    }

    [Fact]
    public void Keeps_two_dots_as_separate_tokens_outside_plpgsql_bodies()
    {
        Assert.Equal([SyntaxKind.Number, SyntaxKind.Dot, SyntaxKind.Dot, SyntaxKind.Number, SyntaxKind.EndOfFile], SqlAssert.Kinds("1..2"));
        SqlAssert.ParseFails("select 1..10", SqlOptions.Postgres);
    }

    private const string QueryLoopSql =
        "do $$ declare r record"
        + "; begin for r in select * from t loop null"
        + "; end loop"
        + "; for a, b in execute 'select 1, 2' using x loop null"
        + "; end loop"
        + "; for r in c(1, k := 2) loop null"
        + "; end loop"
        + "; for r in cur loop null"
        + "; end loop"
        + "; end $$"
        ;

    [Fact]
    public void Parses_query_execute_and_cursor_for_loops()
    {
        var query = Statement<PlpgsqlForQueryStatement>(QueryLoopSql);
        Assert.Equal("select * from t", Located(QueryLoopSql, query.Query, "select *"));
        Assert.StartsWith("for r in select", Located(QueryLoopSql, query, "for r in select"));
        var dynamic = Statement<PlpgsqlForQueryStatement>(QueryLoopSql, 1);
        Assert.Equal(TwoItems, dynamic.Targets.Count);
        var execute = Assert.IsType<PlpgsqlExecuteStatement>(dynamic.Query);
        Assert.Equal("x", Text(QueryLoopSql, Assert.Single(execute.Arguments).Span));
        var cursor = Statement<PlpgsqlForCursorStatement>(QueryLoopSql, Third);
        Assert.Equal("c", Text(QueryLoopSql, cursor.Cursor));
        Assert.IsType<NamedArgumentExpression>(cursor.Arguments[1]);
        Assert.Empty(Statement<PlpgsqlForCursorStatement>(QueryLoopSql, Fourth).Arguments);
    }

    private const string ForeachSql =
        "do $$ begin foreach x slice 1 in array arr loop null"
        + "; end loop"
        + "; foreach y in array array[1, 2] loop null"
        + "; end loop"
        + "; end $$"
        ;

    [Fact]
    public void Parses_foreach_loops()
    {
        var sliced = Statement<PlpgsqlForeachStatement>(ForeachSql);
        Assert.StartsWith("foreach x slice 1 in array arr", Located(ForeachSql, sliced, "foreach x"));
        Assert.Equal(One, Text(ForeachSql, sliced.Slice));
        Assert.Equal("arr", Text(ForeachSql, sliced.Array.Span));
        var plain = Statement<PlpgsqlForeachStatement>(ForeachSql, 1);
        Assert.Null(plain.SliceKeyword);
        Assert.Equal("array[1, 2]", Text(ForeachSql, plain.Array.Span));
    }

    // ---- RETURN ----

    private const string ReturnSql =
        "do $$ begin return next x + 1"
        + "; return next"
        + "; return query select a from t"
        + "; return query execute 'select 1' using y"
        + "; return"
        + "; end $$"
        ;

    [Fact]
    public void Parses_return_forms()
    {
        var next = Statement<PlpgsqlReturnStatement>(ReturnSql);
        Assert.Equal("return next x + 1", Located(ReturnSql, next, "return next x"));
        Assert.Equal("x + 1", Text(ReturnSql, next.Value!.Span));
        Assert.Null(Statement<PlpgsqlReturnStatement>(ReturnSql, 1).Value);
        var query = Statement<PlpgsqlReturnStatement>(ReturnSql, Third);
        Assert.Equal("query", Text(ReturnSql, query.Modifier));
        Assert.IsType<SelectStatement>(query.Query);
        var dynamic = Statement<PlpgsqlReturnStatement>(ReturnSql, Fourth);
        Assert.IsType<PlpgsqlExecuteStatement>(dynamic.Query);
        var bare = Statement<PlpgsqlReturnStatement>(ReturnSql, Fifth);
        Assert.Null(bare.Modifier);
        Assert.Null(bare.Value);
    }

    // ---- RAISE / ASSERT / PERFORM ----

    private const string RaiseSql =
        "do $$ begin raise notice 'x = %, y = %', x, y"
        + "; raise exception 'bad' using hint = 'h', errcode := '22012'"
        + "; raise sqlstate '22012'"
        + "; raise division_by_zero using message = 'm'"
        + "; raise"
        + "; raise debug using detail = 'd'"
        + "; end $$"
        ;

    [Fact]
    public void Parses_raise_forms()
    {
        var notice = Statement<PlpgsqlRaiseStatement>(RaiseSql);
        Assert.Equal("raise notice 'x = %, y = %', x, y", Located(RaiseSql, notice, "raise notice"));
        Assert.Equal("notice", Text(RaiseSql, notice.Level));
        Assert.Equal(TwoItems, notice.Arguments.Count);
        var exception = Statement<PlpgsqlRaiseStatement>(RaiseSql, 1);
        Assert.Equal(TwoItems, exception.Options.Count);
        Assert.Equal("errcode := '22012'", Text(RaiseSql, exception.Options[1].Span));
        var sqlState = Statement<PlpgsqlRaiseStatement>(RaiseSql, Third);
        Assert.Equal("'22012'", Text(RaiseSql, sqlState.Condition));
        Assert.NotNull(sqlState.SqlStateKeyword);
        var condition = Statement<PlpgsqlRaiseStatement>(RaiseSql, Fourth);
        Assert.Equal("division_by_zero", Text(RaiseSql, condition.Condition));
        var reraise = Statement<PlpgsqlRaiseStatement>(RaiseSql, Fifth);
        Assert.Null(reraise.Level);
        Assert.Null(reraise.Format);
        Assert.Single(Statement<PlpgsqlRaiseStatement>(RaiseSql, FiveItems).Options);
    }

    private const string AssertPerformSql =
        "do $$ begin assert x > 0, 'positive'"
        + "; assert y"
        + "; perform f(1)"
        + "; perform * from t where a = 1"
        + "; end $$"
        ;

    [Fact]
    public void Parses_assert_and_perform()
    {
        var check = Statement<PlpgsqlAssertStatement>(AssertPerformSql);
        Assert.Equal("assert x > 0, 'positive'", Located(AssertPerformSql, check, "assert x"));
        Assert.Equal("'positive'", Text(AssertPerformSql, check.Message!.Span));
        Assert.Null(Statement<PlpgsqlAssertStatement>(AssertPerformSql, 1).Message);
        var perform = Statement<PlpgsqlPerformStatement>(AssertPerformSql, Third);
        Assert.Equal("perform f(1)", Located(AssertPerformSql, perform, "perform f"));
        var query = Assert.IsType<SelectStatement>(perform.Query);
        Assert.Equal("f(1)", Text(AssertPerformSql, query.SelectList[0].Expression.Span));
        var where = Statement<PlpgsqlPerformStatement>(AssertPerformSql, Fourth);
        Assert.NotNull(Assert.IsType<SelectStatement>(where.Query).Where);
        Assert.Equal(SyntaxKind.Identifier, where.PerformKeyword.Kind);
    }

    // ---- EXECUTE / GET DIAGNOSTICS ----

    private const string ExecuteSql =
        "do $$ begin execute format('select %I from t', c) into strict r using a, b"
        + "; execute 'delete from t' using x into n"
        + "; execute q"
        + "; get diagnostics n = row_count, ctx := pg_context"
        + "; get stacked diagnostics m = message_text"
        + "; end $$"
        ;

    [Fact]
    public void Parses_dynamic_execute_and_get_diagnostics()
    {
        var first = Statement<PlpgsqlExecuteStatement>(ExecuteSql);
        Assert.StartsWith("execute format", Located(ExecuteSql, first, "execute format"));
        Assert.Equal(StrictWord, Text(ExecuteSql, first.StrictKeyword));
        Assert.Equal("r", Text(ExecuteSql, Assert.Single(first.Targets).Span));
        Assert.Equal(TwoItems, first.Arguments.Count);
        var usingFirst = Statement<PlpgsqlExecuteStatement>(ExecuteSql, 1);
        Assert.Equal("n", Text(ExecuteSql, Assert.Single(usingFirst.Targets).Span));
        Assert.Empty(Statement<PlpgsqlExecuteStatement>(ExecuteSql, Third).Targets);
        var diagnostics = Statement<GetDiagnosticsStatement>(ExecuteSql, Fourth);
        Assert.Equal("get diagnostics n = row_count, ctx := pg_context", Located(ExecuteSql, diagnostics, "get diagnostics"));
        Assert.Equal(TwoItems, diagnostics.Items.Count);
        Assert.Equal("pg_context", Text(ExecuteSql, diagnostics.Items[1].Name));
        Assert.Null(diagnostics.Postgres?.ScopeKeyword);
        var stacked = Statement<GetDiagnosticsStatement>(ExecuteSql, Fifth);
        Assert.Equal("stacked", Text(ExecuteSql, stacked.Postgres!.ScopeKeyword));
    }

    // ---- Cursors ----

    private const string CursorSql =
        "do $$ declare c refcursor"
        + "; begin open c for select 1"
        + "; open c no scroll for execute 'select 2' using x"
        + "; open c(1, k := 2)"
        + "; fetch next from c into r"
        + "; fetch c into a, b"
        + "; move forward 2 in c"
        + "; move backward all from c"
        + "; fetch absolute -1 from c into r"
        + "; close c"
        + "; end $$"
        ;

    [Fact]
    public void Parses_open_fetch_move_and_close()
    {
        var statements = Body(CursorSql).Statements;
        var open = Assert.IsType<PlpgsqlOpenStatement>(statements[0]);
        Assert.Equal("open c for select 1", Located(CursorSql, open, "open c for"));
        Assert.IsType<SelectStatement>(open.Query);
        var dynamic = Assert.IsType<PlpgsqlOpenStatement>(statements[1]);
        Assert.Equal(NoWord, Text(CursorSql, dynamic.NoKeyword));
        Assert.IsType<PlpgsqlExecuteStatement>(dynamic.Query);
        Assert.Equal(TwoItems, Assert.IsType<PlpgsqlOpenStatement>(statements[Third]).Arguments.Count);
        var next = Assert.IsType<PlpgsqlFetchStatement>(statements[Fourth]);
        Assert.Equal("fetch next from c into r", Located(CursorSql, next, "fetch next"));
        Assert.Equal("next", Text(CursorSql, next.Direction[0]));
        Assert.Equal(TwoItems, Assert.IsType<PlpgsqlFetchStatement>(statements[Fifth]).Targets.Count);
        var move = Assert.IsType<PlpgsqlFetchStatement>(statements[FiveItems]);
        Assert.Equal(Two, Text(CursorSql, move.Count!.Span));
        Assert.Equal("in", Text(CursorSql, move.FromKeyword));
        Assert.Equal(TwoItems, Assert.IsType<PlpgsqlFetchStatement>(statements[^ThreeItems]).Direction.Count);
        Assert.Equal("-1", Text(CursorSql, Assert.IsType<PlpgsqlFetchStatement>(statements[^TwoItems]).Count!.Span));
        Assert.IsType<CloseStatement>(statements[^1]);
    }

    // ---- SQL statements ----

    private const string SqlStatementsSql =
        "do $$ begin select a, b into strict x, y from t where id = 1"
        + "; insert into t (a) values (1) returning id into z"
        + "; update t set a = 2 where id = z"
        + "; select into w count(*) from t"
        + "; null"
        + "; commit"
        + "; rollback"
        + "; end $$"
        ;

    [Fact]
    public void Parses_sql_statements_with_into_targets()
    {
        var statements = Body(SqlStatementsSql).Statements;
        var select = Assert.IsType<PlpgsqlSqlStatement>(statements[0]);
        Assert.Equal("select a, b into strict x, y from t where id = 1", Located(SqlStatementsSql, select, "select a"));
        Assert.Equal(StrictWord, Text(SqlStatementsSql, select.StrictKeyword));
        Assert.Equal(TwoItems, select.Targets.Count);
        var query = Assert.IsType<SelectStatement>(select.Statement);
        Assert.Equal(TwoItems, query.SelectList.Count);
        Assert.NotNull(query.Where);
        var insert = Assert.IsType<PlpgsqlSqlStatement>(statements[1]);
        Assert.Equal("z", Text(SqlStatementsSql, Assert.Single(insert.Targets).Span));
        Assert.IsType<InsertStatement>(insert.Statement);
        Assert.IsType<UpdateStatement>(statements[Third]);
        var leading = Assert.IsType<PlpgsqlSqlStatement>(statements[Fourth]);
        Assert.Equal("w", Text(SqlStatementsSql, Assert.Single(leading.Targets).Span));
        Assert.IsType<PlpgsqlNullStatement>(statements[Fifth]);
        Assert.IsType<CommitStatement>(statements[FiveItems]);
        Assert.IsType<RollbackStatement>(statements[^1]);
    }

    // ---- Visitor ----

    private const string VisitorSql =
        "do $$ declare v int := d1"
        + "; begin for i in s1..s2 loop perform p1"
        + "; end loop"
        + "; raise notice '%', r1"
        + "; select c1 into t1 from t"
        + "; execute e1 into t2 using u1"
        + "; end $$"
        ;

    [Fact]
    public void Visitor_walks_plpgsql_statements()
    {
        var visitor = new IdentifierCollector(VisitorSql);
        visitor.Visit(SqlAssert.ParsePg<PgDoStatement>(VisitorSql));
        Assert.Equal(["d1", "s1", "s2", "p1", "r1", "c1", "t1", "e1", "t2", "u1"], visitor.Names);
    }

    private sealed class IdentifierCollector(string source) : SqlVisitor
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Names => _names;

        public override void VisitIdentifierExpression(IdentifierExpression node) =>
            _names.Add(source[node.Span.Position..node.Span.End]);
    }
}
