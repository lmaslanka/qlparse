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
            SqlAssert.Kinds($" {'\t'}{'\r'}{'\n'} select{'\t'}a "));
    }

    public static readonly TheoryData<string, SyntaxKind> KeywordData = new()
    {
        { "select", SyntaxKind.SelectKeyword },
        { "from", SyntaxKind.FromKeyword },
        { "where", SyntaxKind.WhereKeyword },
        { "and", SyntaxKind.AndKeyword },
        { "or", SyntaxKind.OrKeyword },
        { "as", SyntaxKind.AsKeyword },
        { "inner", SyntaxKind.InnerKeyword },
        { "join", SyntaxKind.JoinKeyword },
        { "left", SyntaxKind.LeftKeyword },
        { "right", SyntaxKind.RightKeyword },
        { "full", SyntaxKind.FullKeyword },
        { "cross", SyntaxKind.CrossKeyword },
        { "outer", SyntaxKind.OuterKeyword },
        { "natural", SyntaxKind.NaturalKeyword },
        { "on", SyntaxKind.OnKeyword },
        { "using", SyntaxKind.UsingKeyword },
        { "between", SyntaxKind.BetweenKeyword },
        { "in", SyntaxKind.InKeyword },
        { "is", SyntaxKind.IsKeyword },
        { "null", SyntaxKind.NullKeyword },
        { "group", SyntaxKind.GroupKeyword },
        { "grouping", SyntaxKind.GroupingKeyword },
        { "by", SyntaxKind.ByKeyword },
        { "order", SyntaxKind.OrderKeyword },
        { "limit", SyntaxKind.LimitKeyword },
        { "offset", SyntaxKind.OffsetKeyword },
        { "fetch", SyntaxKind.FetchKeyword },
        { "having", SyntaxKind.HavingKeyword },
        { "not", SyntaxKind.NotKeyword },
        { "asc", SyntaxKind.AscKeyword },
        { "desc", SyntaxKind.DescKeyword },
        { "case", SyntaxKind.CaseKeyword },
        { "when", SyntaxKind.WhenKeyword },
        { "then", SyntaxKind.ThenKeyword },
        { "else", SyntaxKind.ElseKeyword },
        { "end", SyntaxKind.EndKeyword },
        { "exists", SyntaxKind.ExistsKeyword },
        { "all", SyntaxKind.AllKeyword },
        { "any", SyntaxKind.AnyKeyword },
        { "some", SyntaxKind.SomeKeyword },
        { "union", SyntaxKind.UnionKeyword },
        { "except", SyntaxKind.ExceptKeyword },
        { "intersect", SyntaxKind.IntersectKeyword },
        { "with", SyntaxKind.WithKeyword },
        { "over", SyntaxKind.OverKeyword },
        { "window", SyntaxKind.WindowKeyword },
        { "lateral", SyntaxKind.LateralKeyword },
        { "tablesample", SyntaxKind.TablesampleKeyword },
        { "recursive", SyntaxKind.RecursiveKeyword },
        { "search", SyntaxKind.SearchKeyword },
        { "cycle", SyntaxKind.CycleKeyword },
        { "distinct", SyntaxKind.DistinctKeyword },
        { "like", SyntaxKind.LikeKeyword },
        { "similar", SyntaxKind.SimilarKeyword },
        { "cast", SyntaxKind.CastKeyword },
        { "treat", SyntaxKind.TreatKeyword },
        { "deref", SyntaxKind.DerefKeyword },
        { "specifictype", SyntaxKind.SpecifictypeKeyword },
        { "nullif", SyntaxKind.NullIfKeyword },
        { "coalesce", SyntaxKind.CoalesceKeyword },
        { "array", SyntaxKind.ArrayKeyword },
        { "multiset", SyntaxKind.MultisetKeyword },
        { "unnest", SyntaxKind.UnnestKeyword },
        { "set", SyntaxKind.SetKeyword },
        { "cardinality", SyntaxKind.CardinalityKeyword },
        { "element", SyntaxKind.ElementKeyword },
        { "absent", SyntaxKind.AbsentKeyword },
        { "true", SyntaxKind.TrueKeyword },
        { "false", SyntaxKind.FalseKeyword },
        { "filter", SyntaxKind.FilterKeyword },
        { "escape", SyntaxKind.EscapeKeyword },
        { "values", SyntaxKind.ValuesKeyword },
        { "unknown", SyntaxKind.UnknownKeyword },
        { "collate", SyntaxKind.CollateKeyword },
        { "overlaps", SyntaxKind.OverlapsKeyword },
        { "unique", SyntaxKind.UniqueKeyword },
        { "match", SyntaxKind.MatchKeyword },
        { "partial", SyntaxKind.PartialKeyword },
        { "corresponding", SyntaxKind.CorrespondingKeyword },
        { "date", SyntaxKind.DateKeyword },
        { "time", SyntaxKind.TimeKeyword },
        { "timestamp", SyntaxKind.TimestampKeyword },
        { "interval", SyntaxKind.IntervalKeyword },
        { "trim", SyntaxKind.TrimKeyword },
        { "extract", SyntaxKind.ExtractKeyword },
        { "substring", SyntaxKind.SubstringKeyword },
        { "position", SyntaxKind.PositionKeyword },
        { "for", SyntaxKind.ForKeyword },
        { "user", SyntaxKind.UserKeyword },
        { "current_date", SyntaxKind.CurrentDateKeyword },
        { "current_time", SyntaxKind.CurrentTimeKeyword },
        { "current_timestamp", SyntaxKind.CurrentTimestampKeyword },
        { "current_user", SyntaxKind.CurrentUserKeyword },
        { "session_user", SyntaxKind.SessionUserKeyword },
        { "system_user", SyntaxKind.SystemUserKeyword },
        { "localtime", SyntaxKind.LocalTimeKeyword },
        { "localtimestamp", SyntaxKind.LocalTimestampKeyword },
        { "current_role", SyntaxKind.CurrentRoleKeyword },
        { "current_catalog", SyntaxKind.CurrentCatalogKeyword },
        { "current_schema", SyntaxKind.CurrentSchemaKeyword },
        { "current_path", SyntaxKind.CurrentPathKeyword },
        { "convert", SyntaxKind.ConvertKeyword },
        { "translate", SyntaxKind.TranslateKeyword },
        { "read", SyntaxKind.ReadKeyword },
        { "only", SyntaxKind.OnlyKeyword },
        { "update", SyntaxKind.UpdateKeyword },
        { "of", SyntaxKind.OfKeyword },
        { "upper", SyntaxKind.UpperKeyword },
        { "lower", SyntaxKind.LowerKeyword },
        { "overlay", SyntaxKind.OverlayKeyword },
        { "char_length", SyntaxKind.CharLengthKeyword },
        { "octet_length", SyntaxKind.OctetLengthKeyword },
        { "bit_length", SyntaxKind.BitLengthKeyword },
        { "normalize", SyntaxKind.NormalizeKeyword },
        { "floor", SyntaxKind.FloorKeyword },
        { "ceil", SyntaxKind.CeilKeyword },
        { "power", SyntaxKind.PowerKeyword },
        { "sqrt", SyntaxKind.SqrtKeyword },
        { "ln", SyntaxKind.LnKeyword },
        { "exp", SyntaxKind.ExpKeyword },
        { "mod", SyntaxKind.ModKeyword },
        { "abs", SyntaxKind.AbsKeyword },
        { "width_bucket", SyntaxKind.WidthBucketKeyword },
    };

    [Theory]
    [MemberData("KeywordData")]
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
        var result = SqlAssert.Lex(@"""from""");
        Assert.Equal(SyntaxKind.Identifier, result.Tokens[0].Kind);
        Assert.Equal(@"""from""", result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData(@"""a""""b""")]
    public void Quoted_identifier_escapes_quotes(string sql)
    {
        var result = SqlAssert.Lex(sql);
        Assert.Equal(sql, result.Tokens[0].TextOf(result.Source));
    }

    [Theory]
    [InlineData(@"_latin1""foo""")]
    [InlineData(@"_UTF8""a""""b""")]
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
    [InlineData(@"_latin1 ""foo""")]
    public void Character_set_introducer_requires_no_space(string sql)
    {
        Assert.Equal(
            [SyntaxKind.Identifier, SyntaxKind.Identifier, SyntaxKind.EndOfFile],
            SqlAssert.Kinds(sql));
    }

    [Theory]
    [InlineData(@"U&""foo""")]
    [InlineData(@"u&""foo""")]
    [InlineData(@"U&""a""""b""")]
    public void Unicode_delimited_identifier(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.Identifier, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Fact]
    public void Unicode_delimited_identifier_with_escape()
    {
        var text = $"U&{'"'}{'\\'}0441{'"'}";
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
    [InlineData("U&''")]
    public void Strings(string text)
    {
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.String, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Fact]
    public void String_with_unicode_escape()
    {
        var text = $"U&'{'\\'}0441'";
        var result = SqlAssert.Lex(text);
        Assert.Equal(SyntaxKind.String, result.Tokens[0].Kind);
        Assert.Equal(text, result.Tokens[0].TextOf(result.Source));
    }

    [Fact]
    public void Line_comment_is_leading_trivia()
    {
        var result = SqlAssert.Lex($"-- c{'\n'}select");
        var token = result.Tokens[0];
        Assert.Equal(SyntaxKind.SelectKeyword, token.Kind);
        Assert.Equal(1, token.LeadingTriviaCount);
        Assert.Equal(SyntaxKind.LineCommentTrivia, result.Trivia[token.LeadingTriviaStart].Kind);
    }

    [Fact]
    public void Extra_minuses_are_still_a_line_comment()
    {
        var result = SqlAssert.Lex($"--- c{'\n'}select");
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
    public void Unexpected_character_sets_error(string sql)
    {
        var result = Sql.Lex(sql);
        Assert.IsType<SqlParseException>(result.Error);
    }

    [Fact]
    public void Unexpected_character_before_line_comment_sets_error()
    {
        var result = Sql.Lex($"# c{'\n'}select");
        Assert.IsType<SqlParseException>(result.Error);
    }

    [Theory]
    [InlineData("@foo")]
    public void At_parameter_lexes_with_flag(string sql)
    {
        var result = Sql.Lex(sql, SqlOptions.AtParameters);
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
    [InlineData(@"""oops")]
    [InlineData(@"""""")]
    [InlineData(@"U&""oops")]
    [InlineData(@"U&""""")]
    [InlineData("U&'oops")]
    [InlineData(@"_cs""oops")]
    [InlineData(@"_cs""""")]
    [InlineData("/* oops")]
    public void Unterminated_lexemes_set_error(string sql)
    {
        Assert.IsType<SqlParseException>(Sql.Lex(sql).Error);
    }
}
