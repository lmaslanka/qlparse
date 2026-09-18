namespace QlParse.Tests;

public sealed class ParseExpressionTests
{
    [Theory]
    [InlineData("1 or 2", SyntaxKind.OrKeyword)]
    [InlineData("1 and 2", SyntaxKind.AndKeyword)]
    [InlineData("1 = 2", SyntaxKind.EqualsToken)]
    [InlineData("1 != 2", SyntaxKind.NotEqualsToken)]
    [InlineData("1 <> 2", SyntaxKind.NotEqualsToken)]
    [InlineData("1 > 2", SyntaxKind.GreaterThan)]
    [InlineData("1 >= 2", SyntaxKind.GreaterOrEqual)]
    [InlineData("1 < 2", SyntaxKind.LessThan)]
    [InlineData("1 <= 2", SyntaxKind.LessOrEqual)]
    [InlineData("1 + 2", SyntaxKind.PlusToken)]
    [InlineData("1 - 2", SyntaxKind.MinusToken)]
    [InlineData("1 || 2", SyntaxKind.ConcatToken)]
    [InlineData("1 * 2", SyntaxKind.Star)]
    [InlineData("1 / 2", SyntaxKind.SlashToken)]
    [InlineData("a -> 'b'", SyntaxKind.JsonArrowToken)]
    [InlineData("a ->> 'b'", SyntaxKind.JsonTextArrowToken)]
    public void Parses_binary_operators(string sql, SyntaxKind kind)
    {
        Assert.Equal(kind, SqlAssert.Expr<BinaryExpression>(sql).OperatorToken.Kind);
    }

    [Fact]
    public void And_binds_tighter_than_or()
    {
        var or = SqlAssert.Expr<BinaryExpression>("1 or 2 and 3");
        Assert.Equal(SyntaxKind.OrKeyword, or.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.AndKeyword, Assert.IsType<BinaryExpression>(or.Right).OperatorToken.Kind);
    }

    [Fact]
    public void Multiply_binds_tighter_than_add()
    {
        var add = SqlAssert.Expr<BinaryExpression>("1 + 2 * 3");
        Assert.Equal(SyntaxKind.PlusToken, add.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.Star, Assert.IsType<BinaryExpression>(add.Right).OperatorToken.Kind);
    }

    [Fact]
    public void Member_access_and_qualified_star()
    {
        var member = SqlAssert.Expr<MemberAccessExpression>("a.b.c");
        Assert.IsType<MemberAccessExpression>(member.Target);
        Assert.IsType<QualifiedStarExpression>(SqlAssert.Select("select t.*").SelectList[0].Expression);
    }

    [Fact]
    public void Json_arrow_applies_to_member_access()
    {
        var arrow = SqlAssert.Expr<BinaryExpression>("a.b -> 'c'");
        Assert.Equal(SyntaxKind.JsonArrowToken, arrow.OperatorToken.Kind);
        Assert.IsType<MemberAccessExpression>(arrow.Left);
    }

    [Fact]
    public void Json_arrows_chain_left()
    {
        var outer = SqlAssert.Expr<BinaryExpression>("a -> 'b' ->> 'c'");
        Assert.Equal(SyntaxKind.JsonTextArrowToken, outer.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.JsonArrowToken, Assert.IsType<BinaryExpression>(outer.Left).OperatorToken.Kind);
    }

    [Fact]
    public void Parses_collate()
    {
        var collate = SqlAssert.Expr<CollateExpression>("a collate en_us");
        Assert.IsType<IdentifierExpression>(collate.Expression);
    }

    [Fact]
    public void Parses_colon_cast()
    {
        var cast = SqlAssert.Expr<ColonCastExpression>("a :: int");
        Assert.IsType<IdentifierExpression>(cast.Expression);
        Assert.Equal(SyntaxKind.Identifier, cast.Type.Name.Kind);
    }

    [Fact]
    public void Parses_between()
    {
        var between = SqlAssert.Expr<BetweenExpression>("x between 1 and 2");
        Assert.Null(between.NotKeyword);
        var notBetween = SqlAssert.Expr<BetweenExpression>("x not between 1 and 2");
        Assert.NotNull(notBetween.NotKeyword);
    }

    [Fact]
    public void Parses_in_list_and_subquery()
    {
        var list = SqlAssert.Expr<InExpression>("x in (1, 2)");
        Assert.Equal(Count.Two, list.Values.Count);
        Assert.Null(list.Query);

        var subquery = SqlAssert.Expr<InExpression>("x in (select 1)");
        Assert.Empty(subquery.Values);
        Assert.NotNull(subquery.Query);

        Assert.NotNull(SqlAssert.Expr<InExpression>("x not in (1)").NotKeyword);
    }

    [Fact]
    public void Parses_like()
    {
        var like = SqlAssert.Expr<LikeExpression>("x like 'a%'");
        Assert.Null(like.NotKeyword);
        Assert.Null(like.Escape);

        var notLike = SqlAssert.Expr<LikeExpression>("x not like 'a%' escape '/'");
        Assert.NotNull(notLike.NotKeyword);
        Assert.NotNull(notLike.EscapeKeyword);
        Assert.NotNull(notLike.Escape);
    }

    [Theory]
    [InlineData("x is null", SyntaxKind.NullKeyword, false)]
    [InlineData("x is not null", SyntaxKind.NullKeyword, true)]
    [InlineData("x is true", SyntaxKind.TrueKeyword, false)]
    [InlineData("x is false", SyntaxKind.FalseKeyword, false)]
    [InlineData("x is unknown", SyntaxKind.UnknownKeyword, false)]
    [InlineData("x is not true", SyntaxKind.TrueKeyword, true)]
    public void Parses_is(string sql, SyntaxKind value, bool not)
    {
        var expression = SqlAssert.Expr<IsExpression>(sql);
        Assert.Equal(value, expression.Value.Kind);
        Assert.Equal(not, expression.NotKeyword is not null);
    }

    [Fact]
    public void Is_rejects_other_values()
    {
        Assert.Throws<SqlParseException>(() => Sql.Parse("select x is 1"));
    }

    [Fact]
    public void Parses_overlaps()
    {
        Assert.IsType<OverlapsExpression>(SqlAssert.Expr("(1, 2) overlaps (3, 4)"));
    }

    [Fact]
    public void Parses_match()
    {
        var match = SqlAssert.Expr<MatchExpression>("x match (select 1)");
        Assert.Null(match.UniqueKeyword);
        Assert.Null(match.MatchType);

        var uniquePartial = SqlAssert.Expr<MatchExpression>("x match unique partial (select 1)");
        Assert.NotNull(uniquePartial.UniqueKeyword);
        Assert.Equal(SyntaxKind.PartialKeyword, uniquePartial.MatchType!.Value.Kind);

        var full = SqlAssert.Expr<MatchExpression>("x match full (select 1)");
        Assert.Equal(SyntaxKind.FullKeyword, full.MatchType!.Value.Kind);
    }

    [Theory]
    [InlineData("a = all (select 1)", SyntaxKind.AllKeyword)]
    [InlineData("a = any (select 1)", SyntaxKind.AnyKeyword)]
    [InlineData("a = some (select 1)", SyntaxKind.SomeKeyword)]
    public void Parses_quantified_subquery(string sql, SyntaxKind quantifier)
    {
        var expression = SqlAssert.Expr<QuantifiedSubqueryExpression>(sql);
        Assert.Equal(SyntaxKind.EqualsToken, expression.OperatorToken.Kind);
        Assert.Equal(quantifier, expression.Quantifier.Kind);
    }
}
