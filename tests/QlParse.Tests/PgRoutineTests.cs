namespace QlParse.Tests;

// Postgres routines (stream G): CREATE FUNCTION / PROCEDURE, DO, attributes, parameter lists, RETURNS
// forms, SQL-standard bodies, ALTER / DROP / CALL, and the eager parsing of routine bodies.
// SQL holding semicolons is kept in constants split so that no source line carries two of them.
public sealed class PgRoutineTests
{
    private const int TwoItems = 2;
    private const int ThreeItems = 3;
    private const int FourItems = 4;
    private const int Fourth = 3;
    private const int Sixth = 5;
    private const int Seventh = 6;
    private const int Ninth = 8;
    private const string Plpgsql = "plpgsql";
    private const string DoublePrecision = "double precision";
    private const string SelectOne = "select 1";
    private const string NullBlock = "begin null";
    private const string DollarQuote = "$$";

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Text(string sql, SyntaxToken token) => Text(sql, token.Span);

    private static string Text(string sql, SyntaxToken? token) => token is { } value ? Text(sql, value) : string.Empty;

    private static T ParseWhole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParsePg<T>(sql);
        Assert.Equal(sql, Text(sql, node.Span));
        new SqlVisitor().Visit(node);
        return node;
    }

    private static string Attribute(string sql, RoutineCharacteristic characteristic) => Text(sql, characteristic.Span);

    private static int At(string sql, string text) => At(sql, text, 1);

    // The position of the n-th occurrence of `text` in `sql`.
    private static int At(string sql, string text, int occurrence)
    {
        var seen = 0;
        for (var position = 0; position + text.Length <= sql.Length; position++)
        {
            if (string.CompareOrdinal(sql, position, text, 0, text.Length) == 0)
            {
                seen++;
                if (seen == occurrence)
                {
                    return position;
                }
            }
        }

        return -1;
    }

    // ---- DO ----

    private const string DoSql =
        "do $$ begin null"
        + "; end $$"
        ;

    [Fact]
    public void Parses_do_block_with_default_plpgsql_language()
    {
        var node = ParseWhole<PgDoStatement>(DoSql);
        Assert.Null(node.LanguageKeyword);
        Assert.Equal(DoSql[At(DoSql, DollarQuote)..], Text(DoSql, node.Code));
        var block = Assert.IsType<PlpgsqlBlock>(node.Body);
        Assert.Equal(At(DoSql, NullBlock), block.Span.Position);
        Assert.Equal(At(DoSql, " $$", TwoItems), block.Span.End);
        Assert.IsType<PlpgsqlNullStatement>(Assert.Single(block.Statements));
        SqlAssert.ParseFails(DoSql);
    }

    private const string DoLanguageBefore =
        "do language plpgsql $body$ begin null"
        + "; end $body$"
        ;

    private const string DoLanguageAfter =
        "do 'begin null"
        + "; end' language 'plpgsql'"
        ;

    [Fact]
    public void Parses_do_language_before_and_after_the_code()
    {
        var before = ParseWhole<PgDoStatement>(DoLanguageBefore);
        Assert.Equal(Plpgsql, Text(DoLanguageBefore, before.Language));
        Assert.Equal(At(DoLanguageBefore, NullBlock), before.Body!.Span.Position);

        var after = ParseWhole<PgDoStatement>(DoLanguageAfter);
        Assert.Equal("'plpgsql'", Text(DoLanguageAfter, after.Language));
        Assert.Null(after.Body);
    }

    private const string DoPerlSql =
        "do language plperl $$ my $x = 1"
        + "; $$"
        ;

    [Fact]
    public void Leaves_do_bodies_of_other_languages_unparsed()
    {
        var node = ParseWhole<PgDoStatement>(DoPerlSql);
        Assert.Null(node.Body);
        Assert.Empty(node.BodyTokens);
    }

    // ---- CREATE FUNCTION / PROCEDURE ----

    private const string FunctionSql =
        "create or replace function add_one(x int) returns int language plpgsql as $$ begin return x + 1"
        + "; end $$"
        ;

    [Fact]
    public void Parses_create_or_replace_function_language_plpgsql()
    {
        var node = ParseWhole<CreateRoutineStatement>(FunctionSql);
        Assert.Equal("or", Text(FunctionSql, node.OrKeyword));
        Assert.Equal("replace", Text(FunctionSql, node.ReplaceKeyword));
        Assert.Equal("int", Text(FunctionSql, node.ReturnsType!.Span));
        Assert.Equal("language plpgsql", Attribute(FunctionSql, Assert.Single(node.Characteristics)));
        Assert.Equal("as", Text(FunctionSql, node.AsKeyword));
        Assert.Equal(At(FunctionSql, DollarQuote), node.Definition?.Position);
        var block = Assert.IsType<PlpgsqlBlock>(node.Body);
        var statement = Assert.IsType<PlpgsqlReturnStatement>(Assert.Single(block.Statements));
        Assert.Equal("return x + 1", Text(FunctionSql, statement.Span));
        Assert.Equal(At(FunctionSql, "return x"), statement.Span.Position);
        SqlAssert.ParseFails(FunctionSql);
    }

    private const string ProcedureSql =
        "create or replace procedure bump(inout n int) as $p$ begin n := n + 1"
        + "; commit"
        + "; end $p$ language plpgsql"
        ;

    [Fact]
    public void Parses_create_or_replace_procedure_language_plpgsql()
    {
        var node = ParseWhole<CreateRoutineStatement>(ProcedureSql);
        Assert.Equal("procedure", Text(ProcedureSql, node.RoutineKeyword));
        Assert.Null(node.ReturnsKeyword);
        Assert.Equal("inout", Text(ProcedureSql, Assert.Single(node.Parameters).Mode));
        var block = Assert.IsType<PlpgsqlBlock>(node.Body);
        Assert.Equal(TwoItems, block.Statements.Count);
        var assignment = Assert.IsType<PlpgsqlAssignment>(block.Statements[0]);
        Assert.Equal("n := n + 1", Text(ProcedureSql, assignment.Span));
        Assert.IsType<CommitStatement>(block.Statements[1]);
        SqlAssert.ParseFails(ProcedureSql);
    }

    // ---- Attributes ----

    private const string AttributesSql =
        "create function f() returns int language plpgsql immutable strict leakproof parallel safe cost 10 rows 5 "
        + "security definer set search_path = public, pg_temp as $$ begin return 1"
        + "; end $$"
        ;

    [Fact]
    public void Parses_function_attributes()
    {
        var node = ParseWhole<CreateRoutineStatement>(AttributesSql);
        var attributes = node.Characteristics.Select(c => Attribute(AttributesSql, c)).ToArray();
        Assert.Equal(
            ["language plpgsql", "immutable", "strict", "leakproof", "parallel safe", "cost 10", "rows 5",
                "security definer", "set search_path = public, pg_temp"],
            attributes);
        var set = node.Characteristics[^1];
        Assert.Equal(TwoItems, set.Values.Count);
        Assert.Equal("pg_temp", Text(AttributesSql, set.Values[1].Span));
        SqlAssert.ParseFails(AttributesSql);
    }

    private const string MoreAttributesSql =
        "create function f() returns int stable volatile not leakproof external security invoker parallel restricted "
        + "parallel unsafe window support s.f transform for type int, for type double precision called on null input "
        + "returns null on null input set work_mem to '1MB' set datestyle from current language c as 'lib', 'sym'";

    [Fact]
    public void Parses_remaining_function_attributes_and_c_link_symbol()
    {
        var node = ParseWhole<CreateRoutineStatement>(MoreAttributesSql);
        var attributes = node.Characteristics.Select(c => Attribute(MoreAttributesSql, c)).ToArray();
        Assert.Equal(
            ["stable", "volatile", "not leakproof", "external security invoker", "parallel restricted", "parallel unsafe",
                "window", "support s.f", "transform for type int, for type double precision", "called on null input",
                "returns null on null input", "set work_mem to '1MB'", "set datestyle from current", "language c"],
            attributes);
        var transform = node.Characteristics[Ninth];
        Assert.Equal(DoublePrecision, Text(MoreAttributesSql, transform.Types[1].Span));
        Assert.Equal("'lib'", Text(MoreAttributesSql, node.Definition));
        Assert.Equal("'sym'", Text(MoreAttributesSql, node.LinkSymbol));
        Assert.Null(node.Body);
        SqlAssert.ParseFails(MoreAttributesSql);
    }

    // ---- Parameters and RETURNS ----

    private const string ParametersSql =
        "create function f(int, a text, out b int, inout c double precision, variadic d int[], character varying, "
        + "e bit varying default '1', in f t.col%type = 2) returns setof record language sql as $$ select 1 $$";

    [Fact]
    public void Parses_postgres_parameter_lists()
    {
        var node = ParseWhole<CreateRoutineStatement>(ParametersSql);
        var parameters = node.Parameters;
        Assert.Equal(
            ["int", "a text", "out b int", "inout c double precision", "variadic d int[]", "character varying",
                "e bit varying default '1'", "in f t.col%type = 2"],
            parameters.Select(p => Text(ParametersSql, p.Span)).ToArray());
        Assert.Null(parameters[0].Name);
        Assert.Equal("a", Text(ParametersSql, parameters[1].Name));
        Assert.Equal(DoublePrecision, Text(ParametersSql, parameters[Fourth].Type.Span));
        Assert.Null(parameters[Sixth].Name);
        Assert.Equal("default", Text(ParametersSql, parameters[Seventh].DefaultToken));
        Assert.Equal("'1'", Text(ParametersSql, parameters[Seventh].Default!.Span));
        Assert.Equal("%type", Text(ParametersSql, parameters[^1].TypeAttribute!.Span));
        Assert.Equal("t.col", Text(ParametersSql, parameters[^1].Type.Span));
        Assert.Equal("=", Text(ParametersSql, parameters[^1].DefaultToken));
        Assert.Equal("setof", Text(ParametersSql, node.SetofKeyword));
        Assert.Equal("record", Text(ParametersSql, node.ReturnsType!.Span));
    }

    [Theory]
    [InlineData("trigger")]
    [InlineData("void")]
    [InlineData("int[]")]
    [InlineData("setof text")]
    public void Parses_returns_type_forms(string returns)
    {
        var sql = $"create function f() returns {returns} language sql as $$ select null $$";
        var node = ParseWhole<CreateRoutineStatement>(sql);
        Assert.Equal(returns, Text(sql, SourceSpan.From(node.SetofKeyword ?? node.ReturnsType!.Name, node.ReturnsType!.Span)));
    }

    private const string ReturnsTableSql =
        "create function f() returns table (id int, name text) language sql as $$ select 1, 'a' $$";

    [Fact]
    public void Parses_returns_table()
    {
        var node = ParseWhole<CreateRoutineStatement>(ReturnsTableSql);
        Assert.Null(node.ReturnsType);
        var table = Assert.IsType<PgReturnsTable>(node.ReturnsTable);
        Assert.Equal("table (id int, name text)", Text(ReturnsTableSql, table.Span));
        Assert.Equal(TwoItems, table.Columns.Count);
        Assert.Equal("name", Text(ReturnsTableSql, table.Columns[1].Name));
        SqlAssert.ParseFails(ReturnsTableSql);
    }

    [Fact]
    public void Parses_rowtype_return()
    {
        const string Sql = "create function f() returns t%rowtype language sql as $$ select * from t $$";
        var node = ParseWhole<CreateRoutineStatement>(Sql);
        Assert.Equal("t", Text(Sql, node.ReturnsType!.Span));
        Assert.Equal("%rowtype", Text(Sql, node.ReturnsTypeAttribute!.Span));
    }

    // ---- SQL-standard bodies ----

    [Fact]
    public void Parses_sql_standard_return_body()
    {
        const string Sql = "create function f(x int) returns int language sql immutable return x + 1";
        var node = ParseWhole<CreateRoutineStatement>(Sql);
        var body = Assert.IsType<ReturnStatement>(node.Body);
        Assert.Equal("return x + 1", Text(Sql, body.Span));
        Assert.Null(node.Definition);
    }

    private const string AtomicSql =
        "create procedure p(x int) begin atomic insert into t values (x)"
        + "; select x"
        + "; end"
        ;

    [Fact]
    public void Parses_sql_standard_begin_atomic_body()
    {
        var node = ParseWhole<CreateRoutineStatement>(AtomicSql);
        var body = Assert.IsType<CompoundStatement>(node.Body);
        Assert.Equal("atomic", Text(AtomicSql, body.AtomicKeyword));
        Assert.Equal(TwoItems, body.Statements.Count);
        Assert.IsType<InsertStatement>(body.Statements[0]);
    }

    // ---- LANGUAGE sql bodies ----

    private const string SqlBodySql =
        "create function f() returns int language sql as $$ insert into t values (1)"
        + "; select 1 $$"
        ;

    [Fact]
    public void Parses_dollar_quoted_sql_bodies_eagerly()
    {
        var node = ParseWhole<CreateRoutineStatement>(SqlBodySql);
        var script = Assert.IsType<DirectSqlScript>(node.Body);
        Assert.Equal(TwoItems, script.Statements.Count);
        var select = Assert.IsType<SelectStatement>(script.Statements[1]);
        Assert.Equal(At(SqlBodySql, SelectOne), select.Span.Position);
    }

    [Fact]
    public void Parses_single_quoted_sql_bodies_without_inner_quotes()
    {
        const string Sql = "create function f() returns int language sql as 'select 1'";
        var node = ParseWhole<CreateRoutineStatement>(Sql);
        Assert.Equal(SelectOne, Text(Sql, Assert.IsType<SelectStatement>(node.Body).Span));
    }

    private const string SingleQuotedPlpgsql =
        "create function f() returns int language plpgsql as 'begin return 1"
        + "; end'"
        ;

    // Limitation: a single-quoted body with doubled '' quotes cannot be lexed in place, and a single-quoted
    // PL/pgSQL body is never parsed eagerly; both keep only the raw string token.
    [Fact]
    public void Keeps_single_quoted_bodies_with_quotes_and_plpgsql_bodies_raw()
    {
        const string Quoted = "create function f() returns text language sql as 'select ''a'''";
        var quoted = ParseWhole<CreateRoutineStatement>(Quoted);
        Assert.Null(quoted.Body);
        Assert.Equal("'select ''a'''", Text(Quoted, quoted.Definition));

        var plpgsql = ParseWhole<CreateRoutineStatement>(SingleQuotedPlpgsql);
        Assert.Null(plpgsql.Body);
        Assert.Empty(plpgsql.BodyTokens);
    }

    [Fact]
    public void Leaves_bodies_without_a_language_unparsed()
    {
        const string Sql = "create function f() returns int as $$ select 1 $$";
        Assert.Null(ParseWhole<CreateRoutineStatement>(Sql).Body);
    }

    // ---- Eager body parsing: positions, errors, trivia ----

    private const string NestedTagSql =
        "create function f() returns text language plpgsql as $fn$ begin return $$a$$"
        + "; end $fn$"
        ;

    [Fact]
    public void Lexes_dollar_quoted_strings_nested_inside_a_tagged_body()
    {
        var node = ParseWhole<CreateRoutineStatement>(NestedTagSql);
        var statement = Assert.IsType<PlpgsqlReturnStatement>(Assert.Single(Assert.IsType<PlpgsqlBlock>(node.Body).Statements));
        Assert.Equal("$$a$$", Text(NestedTagSql, statement.Value!.Span));
        Assert.Equal(SyntaxKind.EndOfFile, node.BodyTokens[^1].Kind);
        Assert.Equal(NestedTagSql.LastIndexOf("$fn$", StringComparison.Ordinal), node.BodyTokens[^1].Position);
    }

    private const string BrokenBodySql =
        "do $$ begin x := "
        + "; end $$"
        ;

    [Fact]
    public void Fails_the_whole_parse_at_the_absolute_position_of_a_body_error()
    {
        var result = Sql.Parse(BrokenBodySql, SqlOptions.Postgres);
        Assert.NotNull(result.Error);
        Assert.Equal(At(BrokenBodySql, ";"), result.Error.Position);
    }

    [Fact]
    public void Fails_on_an_unterminated_block_at_the_end_of_the_body()
    {
        const string Unterminated = "do $$ begin $$";
        var result = Sql.Parse(Unterminated, SqlOptions.Postgres);
        Assert.NotNull(result.Error);
        Assert.Equal(Unterminated.LastIndexOf('$') - 1, result.Error.Position);
    }

    private const string CommentSql = @"/* outer */ do $$ begin -- inner
 null; end $$"
        ;

    // Body comments are collected by the nested lexer into BodyTrivia; the outer Trivia list only holds
    // the comments outside the string, and the body tokens index into BodyTrivia.
    [Fact]
    public void Collects_body_comments_separately_from_the_outer_trivia()
    {
        var result = Sql.Parse(CommentSql, SqlOptions.Postgres);
        Assert.Null(result.Error);
        var outer = Assert.Single(result.Trivia);
        Assert.Equal("/* outer */", Text(CommentSql, new SourceSpan(outer.Position, outer.Length)));
        var node = Assert.IsType<PgDoStatement>(result.Root);
        var comment = Assert.Single(node.BodyTrivia);
        Assert.Equal("-- inner", Text(CommentSql, new SourceSpan(comment.Position, comment.Length)));
        var nullToken = node.BodyTokens.Single(t => t.Kind == SyntaxKind.NullKeyword);
        Assert.Equal(1, nullToken.LeadingTriviaCount);
        Assert.Equal(0, nullToken.LeadingTriviaStart);
    }

    // ---- ALTER / DROP / CALL ----

    [Fact]
    public void Parses_alter_function_attributes()
    {
        const string Sql = "alter function f(int, text) immutable security invoker set work_mem = '1MB' reset all restrict";
        var node = ParseWhole<PgAlterStatement>(Sql);
        Assert.Equal(TwoItems, node.Target.Arguments.Count);
        var action = Assert.IsType<PgRoutineAttributesAction>(Assert.Single(node.Actions));
        Assert.Equal(FourItems, action.Characteristics.Count);
        Assert.Equal("reset all", Attribute(Sql, action.Characteristics[^1]));
        Assert.Equal("restrict", Text(Sql, action.RestrictKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter function f(int) rename to g", typeof(PgRenameAction))]
    [InlineData("alter procedure p(int) owner to current_user", typeof(PgOwnerAction))]
    [InlineData("alter routine r set schema s", typeof(PgSetSchemaAction))]
    [InlineData("alter function f() depends on extension e", typeof(PgKeywordAction))]
    [InlineData("alter function f() no depends on extension e", typeof(PgKeywordAction))]
    public void Parses_alter_routine_shared_actions(string sql, Type actionType)
    {
        var node = ParseWhole<PgAlterStatement>(sql);
        Assert.IsType(actionType, Assert.Single(node.Actions));
        SqlAssert.ParseFails(sql);
    }

    private const string DropSql = "drop function if exists f(int, out text), g, s.h(double precision) cascade";

    [Fact]
    public void Parses_drop_function_lists()
    {
        var node = ParseWhole<PgDropStatement>(DropSql);
        Assert.Equal("if", Text(DropSql, node.IfKeyword));
        Assert.Equal(ThreeItems, node.Objects.Count);
        Assert.Equal("f(int, out text)", Text(DropSql, node.Objects[0].Span));
        Assert.Equal("cascade", Text(DropSql, node.Behavior));
        SqlAssert.ParseFails(DropSql);

        const string Routine = "drop routine r";
        Assert.Equal("routine", Text(Routine, ParseWhole<PgDropStatement>(Routine).ObjectKind[0]));
    }

    private const string CallSql = "call p(1, b => 2, c := 3)";

    [Fact]
    public void Parses_call_with_named_arguments()
    {
        var node = ParseWhole<CallStatement>(CallSql);
        Assert.Equal(ThreeItems, node.Arguments.Count);
        var named = Assert.IsType<NamedArgumentExpression>(node.Arguments[1]);
        Assert.Equal("b => 2", Text(CallSql, named.Span));
        Assert.IsType<NamedArgumentExpression>(node.Arguments[^1]);
        SqlAssert.ParseFails(CallSql);
    }

    // ---- ISO compatibility under the Postgres flag ----

    [Fact]
    public void Keeps_iso_routine_definitions_parsing_under_postgres()
    {
        const string Sql = "create function f(x int) returns int specific s1 deterministic contains sql sql return x";
        var node = ParseWhole<CreateRoutineStatement>(Sql);
        Assert.Equal("sql", Text(Sql, node.SqlKeyword));
        Assert.IsType<ReturnStatement>(node.Body);
        Assert.Equal(ThreeItems, node.Characteristics.Count);
        Assert.Null(SqlAssert.Parse<CreateRoutineStatement>(Sql).OrKeyword);
    }

    // ---- Visitor ----

    private const string VisitorSql =
        "create function f(a int default d1) returns table (x int) language plpgsql as $$ declare v int := d2"
        + "; begin if c1 then v := d3"
        + "; end if"
        + "; return query select d4 from t"
        + "; end $$"
        ;

    [Fact]
    public void Visitor_walks_into_routine_bodies()
    {
        var visitor = new IdentifierCollector(VisitorSql);
        visitor.Visit(SqlAssert.ParsePg<CreateRoutineStatement>(VisitorSql));
        Assert.Equal(["d1", "d2", "c1", "v", "d3", "d4"], visitor.Names);
    }

    private sealed class IdentifierCollector(string source) : SqlVisitor
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Names => _names;

        public override void VisitIdentifierExpression(IdentifierExpression node) =>
            _names.Add(source[node.Span.Position..node.Span.End]);
    }
}
