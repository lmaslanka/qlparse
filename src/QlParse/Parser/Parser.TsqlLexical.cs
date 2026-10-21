namespace QlParse;

// SQL Server lexical expressions (workstream A): money and binary literals, `~`, @@system functions,
// $PARTITION.fn(expr), the reserved words that end an implicit alias, and the legacy `*=` comparison.
internal sealed partial class Parser
{
    private const char AtSign = '@';
    private const int GlobalVariablePrefixLength = 2;

    internal Expression? TryParseTsqlLexicalPrefix() => _current.Kind switch
    {
        SyntaxKind.MoneyLiteral or SyntaxKind.BinaryLiteral => Literal(),
        SyntaxKind.TildeToken => ParseUnary(),
        _ => null,
    };

    // `@name` stays the EmbeddedHost expression SqlOptions.AtParameters gives; `@@name` is a system function.
    internal Expression ParseTsqlVariable()
    {
        var text = _current.TextOf(_source);
        if (text.Length <= GlobalVariablePrefixLength || text[1] != AtSign)
        {
            return EmbeddedHost();
        }

        var name = Advance();
        return new TsqlGlobalVariableExpression { Name = name, Span = name.Span };
    }

    // `target.name(...)`: `[db.]$PARTITION.fn(expr)`, otherwise an ordinary method invocation.
    internal Expression ParseTsqlMemberCall(Expression target, SyntaxToken dot, SyntaxToken name)
    {
        if (target is IdentifierExpression partition && IsPartitionKeyword(partition.Identifier))
        {
            return ParseTsqlPartitionFunctionCall(null, null, partition.Identifier, dot, name, target.Span);
        }

        if (target is MemberAccessExpression { Target: IdentifierExpression database } member && IsPartitionKeyword(member.Member))
        {
            return ParseTsqlPartitionFunctionCall(database.Identifier, member.Dot, member.Member, dot, name, target.Span);
        }

        return ParseMethodInvocation(target, dot, name);
    }

    private bool IsPartitionKeyword(SyntaxToken token) => TokenEquals(token, Keyword.TsqlPartition);

    private TsqlPartitionFunctionCallExpression ParseTsqlPartitionFunctionCall(
        SyntaxToken? database,
        SyntaxToken? databaseDot,
        SyntaxToken partitionKeyword,
        SyntaxToken dot,
        SyntaxToken functionName,
        SourceSpan start)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var argument = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlPartitionFunctionCallExpression
        {
            Span = SourceSpan.From(start, closeParen),
            Database = database,
            DatabaseDot = databaseDot,
            PartitionKeyword = partitionKeyword,
            Dot = dot,
            FunctionName = functionName,
            OpenParen = openParen,
            Argument = argument,
            CloseParen = closeParen,
        };
    }

    // An unquoted T-SQL reserved word never starts an implicit alias, nor does a non-reserved word that opens
    // the clause after a table (MERGE ... USING, WINDOW w AS).
    internal bool IsTsqlReservedAliasWord() =>
        _current.Kind == SyntaxKind.Identifier && (Keyword.IsTsqlReserved(_current.TextOf(_source)) || IsTsqlClauseWordAfterTable());

    // `a *= b` (legacy left outer join); every other compound assignment ends the expression so that
    // `SET @v += 1` and `UPDATE ... SET c -= 1` parsers see their operator.
    internal bool IsLegacyOuterJoinStar() => _source[_current.Position] == '*';
}
