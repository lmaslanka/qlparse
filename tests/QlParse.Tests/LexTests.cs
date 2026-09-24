namespace QlParse.Tests;

public sealed class LexTests
{
    [Fact]
    public void Empty_input_is_eof()
    {
        Assert.Equal([SyntaxKind.EndOfFile], SqlAssert.Kinds(string.Empty));
    }

    [Fact]
    public void Whitespace_is_discarded()
    {
        Assert.Equal(
            [SyntaxKind.SelectKeyword, SyntaxKind.Identifier, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(" \t\r\n select\ta "));
    }

    [Theory]
    [InlineData("select", SyntaxKind.SelectKeyword)]
    [InlineData("from", SyntaxKind.FromKeyword)]
    [InlineData("where", SyntaxKind.WhereKeyword)]
    [InlineData("and", SyntaxKind.AndKeyword)]
    [InlineData("or", SyntaxKind.OrKeyword)]
    [InlineData("as", SyntaxKind.AsKeyword)]
    [InlineData("inner", SyntaxKind.InnerKeyword)]
    [InlineData("join", SyntaxKind.JoinKeyword)]
    [InlineData("left", SyntaxKind.LeftKeyword)]
    [InlineData("right", SyntaxKind.RightKeyword)]
    [InlineData("full", SyntaxKind.FullKeyword)]
    [InlineData("cross", SyntaxKind.CrossKeyword)]
    [InlineData("outer", SyntaxKind.OuterKeyword)]
    [InlineData("natural", SyntaxKind.NaturalKeyword)]
    [InlineData("on", SyntaxKind.OnKeyword)]
    [InlineData("using", SyntaxKind.UsingKeyword)]
    [InlineData("between", SyntaxKind.BetweenKeyword)]
    [InlineData("in", SyntaxKind.InKeyword)]
    [InlineData("is", SyntaxKind.IsKeyword)]
    [InlineData("null", SyntaxKind.NullKeyword)]
    [InlineData("group", SyntaxKind.GroupKeyword)]
    [InlineData("grouping", SyntaxKind.GroupingKeyword)]
    [InlineData("by", SyntaxKind.ByKeyword)]
    [InlineData("order", SyntaxKind.OrderKeyword)]
    [InlineData("limit", SyntaxKind.LimitKeyword)]
    [InlineData("offset", SyntaxKind.OffsetKeyword)]
    [InlineData("fetch", SyntaxKind.FetchKeyword)]
    [InlineData("having", SyntaxKind.HavingKeyword)]
    [InlineData("not", SyntaxKind.NotKeyword)]
    [InlineData("asc", SyntaxKind.AscKeyword)]
    [InlineData("desc", SyntaxKind.DescKeyword)]
    [InlineData("case", SyntaxKind.CaseKeyword)]
    [InlineData("when", SyntaxKind.WhenKeyword)]
    [InlineData("then", SyntaxKind.ThenKeyword)]
    [InlineData("else", SyntaxKind.ElseKeyword)]
    [InlineData("end", SyntaxKind.EndKeyword)]
    [InlineData("exists", SyntaxKind.ExistsKeyword)]
    [InlineData("all", SyntaxKind.AllKeyword)]
    [InlineData("any", SyntaxKind.AnyKeyword)]
    [InlineData("some", SyntaxKind.SomeKeyword)]
    [InlineData("union", SyntaxKind.UnionKeyword)]
    [InlineData("except", SyntaxKind.ExceptKeyword)]
    [InlineData("intersect", SyntaxKind.IntersectKeyword)]
    [InlineData("with", SyntaxKind.WithKeyword)]
    [InlineData("over", SyntaxKind.OverKeyword)]
    [InlineData("window", SyntaxKind.WindowKeyword)]
    [InlineData("lateral", SyntaxKind.LateralKeyword)]
    [InlineData("tablesample", SyntaxKind.TablesampleKeyword)]
    [InlineData("recursive", SyntaxKind.RecursiveKeyword)]
    [InlineData("search", SyntaxKind.SearchKeyword)]
    [InlineData("cycle", SyntaxKind.CycleKeyword)]
    [InlineData("distinct", SyntaxKind.DistinctKeyword)]
    [InlineData("like", SyntaxKind.LikeKeyword)]
    [InlineData("similar", SyntaxKind.SimilarKeyword)]
    [InlineData("cast", SyntaxKind.CastKeyword)]
    [InlineData("treat", SyntaxKind.TreatKeyword)]
    [InlineData("deref", SyntaxKind.DerefKeyword)]
    [InlineData("specifictype", SyntaxKind.SpecifictypeKeyword)]
    [InlineData("nullif", SyntaxKind.NullIfKeyword)]
    [InlineData("coalesce", SyntaxKind.CoalesceKeyword)]
    [InlineData("array", SyntaxKind.ArrayKeyword)]
    [InlineData("multiset", SyntaxKind.MultisetKeyword)]
    [InlineData("unnest", SyntaxKind.UnnestKeyword)]
    [InlineData("set", SyntaxKind.SetKeyword)]
    [InlineData("cardinality", SyntaxKind.CardinalityKeyword)]
    [InlineData("element", SyntaxKind.ElementKeyword)]
    [InlineData("absent", SyntaxKind.AbsentKeyword)]
    [InlineData("true", SyntaxKind.TrueKeyword)]
    [InlineData("false", SyntaxKind.FalseKeyword)]
    [InlineData("filter", SyntaxKind.FilterKeyword)]
    [InlineData("escape", SyntaxKind.EscapeKeyword)]
    [InlineData("values", SyntaxKind.ValuesKeyword)]
    [InlineData("unknown", SyntaxKind.UnknownKeyword)]
    [InlineData("collate", SyntaxKind.CollateKeyword)]
    [InlineData("overlaps", SyntaxKind.OverlapsKeyword)]
    [InlineData("unique", SyntaxKind.UniqueKeyword)]
    [InlineData("match", SyntaxKind.MatchKeyword)]
    [InlineData("partial", SyntaxKind.PartialKeyword)]
    [InlineData("corresponding", SyntaxKind.CorrespondingKeyword)]
    [InlineData("date", SyntaxKind.DateKeyword)]
    [InlineData("time", SyntaxKind.TimeKeyword)]
    [InlineData("timestamp", SyntaxKind.TimestampKeyword)]
    [InlineData("interval", SyntaxKind.IntervalKeyword)]
    [InlineData("trim", SyntaxKind.TrimKeyword)]
    [InlineData("extract", SyntaxKind.ExtractKeyword)]
    [InlineData("substring", SyntaxKind.SubstringKeyword)]
    [InlineData("position", SyntaxKind.PositionKeyword)]
    [InlineData("for", SyntaxKind.ForKeyword)]
    [InlineData("user", SyntaxKind.UserKeyword)]
    [InlineData("current_date", SyntaxKind.CurrentDateKeyword)]
    [InlineData("current_time", SyntaxKind.CurrentTimeKeyword)]
    [InlineData("current_timestamp", SyntaxKind.CurrentTimestampKeyword)]
    [InlineData("current_user", SyntaxKind.CurrentUserKeyword)]
    [InlineData("session_user", SyntaxKind.SessionUserKeyword)]
    [InlineData("system_user", SyntaxKind.SystemUserKeyword)]
    [InlineData("localtime", SyntaxKind.LocalTimeKeyword)]
    [InlineData("localtimestamp", SyntaxKind.LocalTimestampKeyword)]
    [InlineData("current_role", SyntaxKind.CurrentRoleKeyword)]
    [InlineData("current_catalog", SyntaxKind.CurrentCatalogKeyword)]
    [InlineData("current_schema", SyntaxKind.CurrentSchemaKeyword)]
    [InlineData("current_path", SyntaxKind.CurrentPathKeyword)]
    [InlineData("convert", SyntaxKind.ConvertKeyword)]
    [InlineData("translate", SyntaxKind.TranslateKeyword)]
    [InlineData("read", SyntaxKind.ReadKeyword)]
    [InlineData("only", SyntaxKind.OnlyKeyword)]
    [InlineData("update", SyntaxKind.UpdateKeyword)]
    [InlineData("of", SyntaxKind.OfKeyword)]
    [InlineData("upper", SyntaxKind.UpperKeyword)]
    [InlineData("lower", SyntaxKind.LowerKeyword)]
    [InlineData("overlay", SyntaxKind.OverlayKeyword)]
    [InlineData("char_length", SyntaxKind.CharLengthKeyword)]
    [InlineData("octet_length", SyntaxKind.OctetLengthKeyword)]
    [InlineData("bit_length", SyntaxKind.BitLengthKeyword)]
    [InlineData("normalize", SyntaxKind.NormalizeKeyword)]
    [InlineData("floor", SyntaxKind.FloorKeyword)]
    [InlineData("ceil", SyntaxKind.CeilKeyword)]
    [InlineData("power", SyntaxKind.PowerKeyword)]
    [InlineData("sqrt", SyntaxKind.SqrtKeyword)]
    [InlineData("ln", SyntaxKind.LnKeyword)]
    [InlineData("exp", SyntaxKind.ExpKeyword)]
    [InlineData("mod", SyntaxKind.ModKeyword)]
    [InlineData("abs", SyntaxKind.AbsKeyword)]
    [InlineData("width_bucket", SyntaxKind.WidthBucketKeyword)]
    public void Classifies_keywords(string text, SyntaxKind kind)
    {
        Assert.Equal([kind, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData("SELECT")]
    [InlineData("Select")]
    public void Keywords_are_case_insensitive(string text)
    {
        Assert.Equal([SyntaxKind.SelectKeyword, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("next")]
    [InlineData("value")]
    [InlineData("U")]
    [InlineData("_x")]
    [InlineData("_foo")]
    [InlineData("foo_bar")]
    [InlineData("foo1")]
    [InlineData("foo$")]
    [InlineData("foo$bar")]
    [InlineData("leading")]
    [InlineData("trailing")]
    [InlineData("both")]
    [InlineData("to")]
    [InlineData("double")]
    [InlineData("precision")]
    [InlineData("character")]
    [InlineData("char")]
    [InlineData("varying")]
    [InlineData("zone")]
    [InlineData("café")]
    public void Unquoted_identifiers(string text)
    {
        Assert.Equal([SyntaxKind.Identifier, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Fact]
    public void Quoted_identifier()
    {
        var result = SqlAssert.Lex("\"from\"");
        Assert.Equal(SyntaxKind.Identifier, result.Tokens[0].Kind);
        Assert.Equal("\"from\"", result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData("\"a\"\"b\"")]
    public void Quoted_identifier_escapes_quotes(string sql)
    {
        var result = SqlAssert.Lex(sql);
        Assert.Equal(sql, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData("_latin1\"foo\"")]
    [InlineData("_UTF8\"a\"\"b\"")]
    public void Character_set_introducer_identifier(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.Identifier, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData("_utf8'hi'")]
    public void Character_set_introducer_string(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.String, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData("_latin1 \"foo\"")]
    public void Character_set_introducer_requires_no_space(string sql)
    {
        Assert.Equal(
            [SyntaxKind.Identifier, SyntaxKind.Identifier, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData("U&\"foo\"")]
    [InlineData("u&\"foo\"")]
    [InlineData("U&\"a\"\"b\"")]
    [InlineData("U&\"\\0441\"")]
    public void Unicode_delimited_identifier(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.Identifier, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData(",", SyntaxKind.Comma)]
    [InlineData("=", SyntaxKind.EqualsToken)]
    [InlineData(".", SyntaxKind.Dot)]
    [InlineData(";", SyntaxKind.Semicolon)]
    [InlineData("(", SyntaxKind.OpenParen)]
    [InlineData(")", SyntaxKind.CloseParen)]
    [InlineData("[", SyntaxKind.OpenBracket)]
    [InlineData("]", SyntaxKind.CloseBracket)]
    [InlineData(">", SyntaxKind.GreaterThan)]
    [InlineData("<", SyntaxKind.LessThan)]
    [InlineData("*", SyntaxKind.Star)]
    [InlineData("+", SyntaxKind.PlusToken)]
    [InlineData("-", SyntaxKind.MinusToken)]
    [InlineData("/", SyntaxKind.SlashToken)]
    [InlineData("?", SyntaxKind.QuestionMark)]
    [InlineData(":", SyntaxKind.ColonToken)]
    public void Single_char_tokens(string text, SyntaxKind kind)
    {
        Assert.Equal([kind, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData(":foo")]
    [InlineData(":from")]
    public void Embedded_host_name(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.EmbeddedHost, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData("::", SyntaxKind.DoubleColonToken)]
    [InlineData("||", SyntaxKind.ConcatToken)]
    [InlineData(">=", SyntaxKind.GreaterOrEqual)]
    [InlineData("<=", SyntaxKind.LessOrEqual)]
    [InlineData("<>", SyntaxKind.NotEqualsToken)]
    [InlineData("!=", SyntaxKind.NotEqualsToken)]
    [InlineData("->", SyntaxKind.JsonArrowToken)]
    [InlineData("->>", SyntaxKind.JsonTextArrowToken)]
    public void Multi_char_tokens(string text, SyntaxKind kind)
    {
        Assert.Equal([kind, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData("|", SyntaxKind.BarToken)]
    [InlineData("{", SyntaxKind.OpenBrace)]
    [InlineData("}", SyntaxKind.CloseBrace)]
    [InlineData("^", SyntaxKind.CaretToken)]
    [InlineData("$", SyntaxKind.DollarToken)]
    public void Pattern_tokens(string text, SyntaxKind kind)
    {
        Assert.Equal([kind, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData("->>>")]
    public void Longest_match_prefers_json_text_arrow(string sql)
    {
        Assert.Equal(
            [SyntaxKind.JsonTextArrowToken, SyntaxKind.GreaterThan, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("42")]
    [InlineData("3.14")]
    [InlineData("10.0")]
    [InlineData("1e2")]
    [InlineData("1E2")]
    [InlineData("1.5e+10")]
    [InlineData("1.5E-3")]
    [InlineData("1e0")]
    public void Numbers(string text)
    {
        Assert.Equal([SyntaxKind.Number, SyntaxKind.EndOfFile], SqlAssert.Kinds(text));
    }

    [Theory]
    [InlineData("1e", SyntaxKind.Number, SyntaxKind.Identifier)]
    [InlineData("1 e10", SyntaxKind.Number, SyntaxKind.Identifier)]
    public void Incomplete_exponent_is_not_a_number(string sql, SyntaxKind first, SyntaxKind second)
    {
        Assert.Equal([first, second, SyntaxKind.EndOfFile], SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData("1e+")]
    public void Exponent_sign_without_digits_is_not_a_number(string sql)
    {
        Assert.Equal(
            [SyntaxKind.Number, SyntaxKind.Identifier, SyntaxKind.PlusToken, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData("1.")]
    public void Trailing_dot_is_not_part_of_number(string sql)
    {
        Assert.Equal(
            [SyntaxKind.Number, SyntaxKind.Dot, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData(".5")]
    public void Leading_dot_is_not_a_number(string sql)
    {
        Assert.Equal(
            [SyntaxKind.Dot, SyntaxKind.Number, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData("'hello'")]
    [InlineData("'it''s'")]
    [InlineData("N'foo'")]
    [InlineData("n'foo'")]
    [InlineData("B'1'")]
    [InlineData("b'1'")]
    [InlineData("X'AB'")]
    [InlineData("x'ab'")]
    [InlineData("U&'foo'")]
    [InlineData("u&'foo'")]
    [InlineData("U&'it''s'")]
    [InlineData("U&'\\0441'")]
    [InlineData("U&''")]
    public void Strings(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.String, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Fact]
    public void Line_comment_is_leading_trivia()
    {
        var result = SqlAssert.Lex("-- c\nselect");
        var token = result.Tokens[0];
        Assert.Equal(SyntaxKind.SelectKeyword, token.Kind);
        Assert.Equal(1, token.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.LineCommentTrivia, result.Trivia[token.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Extra_minuses_are_still_a_line_comment()
    {
        var result = SqlAssert.Lex("--- c\nselect");
        var token = result.Tokens[0];
        Assert.Equal(SyntaxKind.SelectKeyword, token.Kind);
        Assert.Equal(1, token.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.LineCommentTrivia, result.Trivia[token.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Extra_minuses_after_a_token_are_a_line_comment()
    {
        var result = SqlAssert.Lex("select 1---x");
        Assert.Equal(SyntaxKind.SelectKeyword, result.Tokens[0].Kind);
        Assert.Equal(SyntaxKind.Number, result.Tokens[1].Kind);
        var eof = result.Tokens[^1];
        Assert.Equal(SyntaxKind.EndOfFile, eof.Kind);
        Assert.Equal(1, eof.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.LineCommentTrivia, result.Trivia[eof.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Glued_line_comment_is_trivia()
    {
        var result = SqlAssert.Lex("select--c");
        Assert.Equal(SyntaxKind.SelectKeyword, result.Tokens[0].Kind);
        var eof = result.Tokens[^1];
        Assert.Equal(SyntaxKind.EndOfFile, eof.Kind);
        Assert.Equal(1, eof.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.LineCommentTrivia, result.Trivia[eof.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Line_comment_at_eof_attaches_to_eof()
    {
        var result = SqlAssert.Lex("select -- c");
        var eof = result.Tokens[^1];
        Assert.Equal(SyntaxKind.EndOfFile, eof.Kind);
        Assert.Equal(1, eof.LeadingTriviaCount);
    }

    [Fact]
    public void Block_comment_is_leading_trivia()
    {
        var result = SqlAssert.Lex("select /* x */ a");
        var ident = result.Tokens[1];
        Assert.Equal(SyntaxKind.Identifier, ident.Kind);
        Assert.Equal(1, ident.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.BlockCommentTrivia, result.Trivia[ident.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Nested_block_comment()
    {
        var result = SqlAssert.Lex("/* a /* b */ c */ select");
        Assert.Equal(SyntaxKind.SelectKeyword, result.Tokens[0].Kind);
        Assert.Equal(SyntaxKind.BlockCommentTrivia, result.Trivia[0].Kind);
    }

    [Fact]
    public void Lex_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Sql.Lex(null!));
    }

    [Theory]
    [InlineData("@")]
    [InlineData("!")]
    [InlineData("# c\nselect")]
    public void Unexpected_character_sets_error(string sql)
    {
        var result = Sql.Lex(sql);
        Assert.IsType<SqlParseException>(result.Error);
    }

    [Theory]
    [InlineData("@foo")]
    public void At_parameter_lexes_with_flag(string sql)
    {
        var result = Sql.Lex(sql, SqlFlags.AtParameters);
        Assert.Null(result.Error);
        Assert.Equal(SyntaxKind.EmbeddedHost, result.Tokens[0].Kind);
        Assert.Equal(sql, result.Tokens[0].TextOf(result.Source));
    }

    [Fact]
    public void Lex_keeps_tokens_before_error()
    {
        var result = Sql.Lex("select @");
        Assert.IsType<SqlParseException>(result.Error);
        Assert.Equal(SyntaxKind.SelectKeyword, result.Tokens[0].Kind);
    }

    [Theory]
    [InlineData("'oops")]
    [InlineData("\"oops")]
    [InlineData("\"\"")]
    [InlineData("U&\"oops")]
    [InlineData("U&\"\"")]
    [InlineData("U&'oops")]
    [InlineData("_cs\"oops")]
    [InlineData("_cs\"\"")]
    [InlineData("/* oops")]
    public void Unterminated_lexemes_set_error(string sql)
    {
        Assert.IsType<SqlParseException>(Sql.Lex(sql).Error);
    }
}
