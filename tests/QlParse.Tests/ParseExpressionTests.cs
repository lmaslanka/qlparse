namespace QlParse.Tests;

public sealed class ParseExpressionTests
{
    private sealed class EmptyVisitor : SqlVisitor;

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
    public void Parses_multiset_ops()
    {
        var all = SqlAssert.Expr<MultisetOperationExpression>("a multiset union all b");
        Assert.Equal(SyntaxKind.UnionKeyword, all.Operator.Kind);
        Assert.Equal(SyntaxKind.AllKeyword, all.Quantifier!.Value.Kind);

        var distinct = SqlAssert.Expr<MultisetOperationExpression>("a multiset except distinct b");
        Assert.Equal(SyntaxKind.ExceptKeyword, distinct.Operator.Kind);
        Assert.Equal(SyntaxKind.DistinctKeyword, distinct.Quantifier!.Value.Kind);

        Assert.Null(SqlAssert.Expr<MultisetOperationExpression>("a multiset intersect b").Quantifier);
    }

    [Fact]
    public void Multiset_intersect_binds_tighter_than_union()
    {
        var union = SqlAssert.Expr<MultisetOperationExpression>("multiset[1] multiset union multiset[2] multiset intersect multiset[3]");
        Assert.Equal(SyntaxKind.UnionKeyword, union.Operator.Kind);
        Assert.Equal(SyntaxKind.IntersectKeyword, Assert.IsType<MultisetOperationExpression>(union.Right).Operator.Kind);
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
        Assert.NotNull(SqlAssert.Expr<ColonCastExpression>("a::numeric(10, 2)").Type.Precision);
    }

    [Fact]
    public void Parses_dereference_and_methods()
    {
        var arrow = SqlAssert.Expr<DereferenceExpression>("p.manager -> name");
        Assert.IsType<MemberAccessExpression>(arrow.Reference);
        Assert.IsType<DereferenceExpression>(SqlAssert.Expr("deref( p ) -> name"));

        var method = SqlAssert.Expr<MethodInvocationExpression>("p.label( )");
        Assert.Empty(method.Arguments);
        Assert.Null(method.AsKeyword);
        var chained = SqlAssert.Expr<MethodInvocationExpression>("p.label( ).score( 1 )");
        Assert.IsType<MethodInvocationExpression>(chained.Target);

        var generalized = SqlAssert.Expr<MethodInvocationExpression>("(p as sch.person).label(p.first_name)");
        Assert.NotNull(generalized.AsKeyword);
        Assert.Single(generalized.Type!.NameTail);
        Assert.Single(generalized.Arguments);

        var staticMethod = SqlAssert.Expr<StaticMethodInvocationExpression>("sch.person :: make( p.first_name )");
        Assert.IsType<MemberAccessExpression>(staticMethod.Type);
        Assert.Empty(SqlAssert.Expr<StaticMethodInvocationExpression>("person :: make( )").Arguments);
        new EmptyVisitor().Visit(generalized);
    }

    [Fact]
    public void Parses_period_predicates()
    {
        var contains = SqlAssert.Expr<PeriodPredicateExpression>("period ( a , b ) contains period ( c , d )");
        Assert.IsType<PeriodExpression>(contains.Left);
        Assert.IsType<PeriodExpression>(contains.Right);
        Assert.NotNull(SqlAssert.Expr<PeriodPredicateExpression>("period ( a , b ) immediately precedes period ( c , d )").ImmediatelyKeyword);
        Assert.IsType<OverlapsExpression>(SqlAssert.Expr("period ( a , b ) overlaps period ( c , d )"));
        Assert.IsType<PeriodPredicateExpression>(SqlAssert.Expr("dept_period equals period ( a , b )"));
        Assert.IsType<PeriodPredicateExpression>(SqlAssert.Expr("period ( a , b ) succeeds period ( c , d )"));

        var alias = SqlAssert.Select("select a contains from t").SelectList[0];
        Assert.IsType<IdentifierExpression>(alias.Expression);
        Assert.NotNull(alias.Alias);
        new EmptyVisitor().Visit(contains);
    }

    [Fact]
    public void Parses_xml_and_json()
    {
        var parsed = SqlAssert.Expr<MarkupCallExpression>("xmlparse ( document '<a/>' preserve whitespace )");
        Assert.NotNull(parsed.Clauses);
        var serialized = SqlAssert.Expr<MarkupCallExpression>("xmlserialize ( content x as varchar ( 20 ) encoding utf8 )");
        Assert.NotNull(serialized.Returning);
        var element = SqlAssert.Expr<MarkupCallExpression>("xmlelement ( name emp , xmlnamespaces ( default 'urn:e' ) , xmlattributes ( id as \"id\" ) , name )");
        Assert.Equal(Count.Three, element.Arguments.Count);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlforest ( name as emp_name , dept )"));
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlconcat ( a , b )"));
        Assert.NotNull(SqlAssert.Expr<MarkupCallExpression>("xmlagg ( x order by x )").OrderBy);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlcomment ( 'note' )"));
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlpi ( name php , 'echo' )"));
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmldocument ( x )"));
        var query = SqlAssert.Expr<MarkupCallExpression>("xmlquery ( '/a' passing x as emp returning content )");
        Assert.NotNull(query.Returning);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlexists ( '/a' passing x )"));
        Assert.NotNull(SqlAssert.Expr<MarkupCallExpression>("xmlcast ( x as integer )").Returning);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("xmlvalidate ( x according to xmlschema 'urn:s' )"));
        Assert.Equal(SyntaxKind.Identifier, SqlAssert.Expr<IsExpression>("x is document").Value.Kind);
        Assert.NotNull(SqlAssert.Expr<IsExpression>("x is not content").NotKeyword);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast ( x as xml ( document ) )").Type.Modifier);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast ( x as json )").Type);

        var obj = SqlAssert.Expr<MarkupCallExpression>("json_object ( key 'a' value 1 , 'b' : 2 absent on null with unique keys returning varchar ( 20 ) )");
        Assert.NotNull(obj.Returning);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("json_array ( 1 , 2 null on null )"));
        Assert.NotNull(SqlAssert.Expr<MarkupCallExpression>("json_array ( select a from t )").Query);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("json_objectagg ( key k value v )"));
        Assert.NotNull(SqlAssert.Expr<MarkupCallExpression>("json_arrayagg ( v order by v )").OrderBy);
        var value = SqlAssert.Expr<MarkupCallExpression>("json_value ( j , '$.a' returning integer default 0 on empty error on error )");
        Assert.NotNull(value.Returning);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("json_query ( j , '$.a' with conditional array wrapper )"));
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("json_exists ( j , '$.a' true on error )"));
        Assert.NotNull(SqlAssert.Expr<MarkupCallExpression>("json_serialize ( j returning varchar ( 100 ) )").Returning);
        Assert.IsType<MarkupCallExpression>(SqlAssert.Expr("json_scalar ( 1 )"));
        var predicate = SqlAssert.Expr<IsExpression>("j is not json object with unique keys");
        Assert.NotNull(predicate.NotKeyword);
        Assert.NotNull(predicate.KindKeyword);
        Assert.NotNull(predicate.KeysKeyword);
        Assert.IsType<JsonAccessorExpression>(Assert.IsType<MemberAccessExpression>(SqlAssert.Expr("j [ 0 ].name")).Target);
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr("xmlparse "));
        new EmptyVisitor().Visit(element);
        new EmptyVisitor().Visit(value);
    }

    [Fact]
    public void Parses_mdarray_ops()
    {
        var constructed = SqlAssert.Expr<MdarrayConstructorExpression>("mdarray [ 0 : 1 , 0 : 2 ] ( 1 , 2 , 3 )");
        Assert.Equal(Count.Two, constructed.Dimensions.Count);
        Assert.NotNull(SqlAssert.Expr<MdarrayConstructorExpression>("mdarray ( select a from t )").Query);
        var slice = SqlAssert.Expr<MdarraySliceExpression>("a [ 1 : 2 , * ]");
        Assert.Equal(Count.Two, slice.Axes.Count);
        Assert.NotNull(slice.Axes[1].Star);
        Assert.IsType<MdarrayAggregateExpression>(SqlAssert.Expr("mdarray_sum ( a )"));
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast ( x as integer mdarray [ 0 : 4 ] )").Type.Collections);
        new EmptyVisitor().Visit(slice);
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

    [Fact]
    public void Parses_similar()
    {
        var similar = SqlAssert.Expr<SimilarExpression>("x similar to 'a+'");
        Assert.Null(similar.NotKeyword);
        Assert.Null(similar.Escape);

        var notSimilar = SqlAssert.Expr<SimilarExpression>("x not similar to 'a+' escape '/'");
        Assert.NotNull(notSimilar.NotKeyword);
        Assert.NotNull(notSimilar.EscapeKeyword);
        Assert.NotNull(notSimilar.Escape);
    }

    [Fact]
    public void Parses_distinct_from()
    {
        var distinct = SqlAssert.Expr<DistinctFromExpression>("x is distinct from y");
        Assert.Null(distinct.NotKeyword);
        Assert.Equal(SyntaxKind.FromKeyword, distinct.FromKeyword.Kind);

        Assert.NotNull(SqlAssert.Expr<DistinctFromExpression>("x is not distinct from y").NotKeyword);
    }

    [Fact]
    public void Parses_normalized()
    {
        var plain = SqlAssert.Expr<NormalizedPredicateExpression>("x is normalized");
        Assert.Null(plain.NotKeyword);
        Assert.Null(plain.Form);

        var formed = SqlAssert.Expr<NormalizedPredicateExpression>("x is not nfc normalized");
        Assert.NotNull(formed.NotKeyword);
        Assert.NotNull(formed.Form);

        Assert.NotNull(SqlAssert.Expr<NormalizedPredicateExpression>("x is nfkd normalized").Form);
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
        Assert.NotNull(Sql.Parse("select x is 1").Error);
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
    [InlineData("a = all (select 1)", SyntaxKind.EqualsToken, SyntaxKind.AllKeyword)]
    [InlineData("a <> any (select 1)", SyntaxKind.NotEqualsToken, SyntaxKind.AnyKeyword)]
    [InlineData("a < some (select 1)", SyntaxKind.LessThan, SyntaxKind.SomeKeyword)]
    [InlineData("a > all (select 1)", SyntaxKind.GreaterThan, SyntaxKind.AllKeyword)]
    [InlineData("a <= any (select 1)", SyntaxKind.LessOrEqual, SyntaxKind.AnyKeyword)]
    [InlineData("a >= some (select 1)", SyntaxKind.GreaterOrEqual, SyntaxKind.SomeKeyword)]
    [InlineData("(a, b) = all (select 1, 2)", SyntaxKind.EqualsToken, SyntaxKind.AllKeyword)]
    public void Parses_quantified_subquery(string sql, SyntaxKind op, SyntaxKind quantifier)
    {
        var expression = SqlAssert.Expr<QuantifiedSubqueryExpression>(sql);
        Assert.Equal(op, expression.OperatorToken.Kind);
        Assert.Equal(quantifier, expression.Quantifier.Kind);
    }
}
