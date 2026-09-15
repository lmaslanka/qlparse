using System.Text;

namespace SqlParser;

internal static class Formatter
{
    public static string Format(SelectStatement statement)
    {
        const string indent = "    ";
        var sql = new StringBuilder();
        sql.Append(Keyword.Select.ToUpperInvariant());
        for (var i = 0; i < statement.SelectList.Count; i++)
        {
            sql.AppendLine();
            sql.Append(indent);
            AppendExpression(sql, statement.SelectList[i]);
            if (i < statement.SelectList.Count - 1)
            {
                sql.Append(',');
            }
        }

        sql.AppendLine();
        sql.Append(Keyword.From.ToUpperInvariant());
        sql.Append(' ');
        AppendIdentifier(sql, statement.TableName);

        if (statement.Where is not null)
        {
            sql.AppendLine();
            sql.Append(Keyword.Where.ToUpperInvariant());
            sql.Append(' ');
            AppendExpression(sql, statement.Where.Expression);
        }

        return sql.ToString();
    }

    private static void AppendExpression(StringBuilder sql, Expression expression)
    {
        switch (expression)
        {
            case IdentifierExpression identifier:
                AppendIdentifier(sql, identifier.Identifier);
                break;
            case LiteralExpression literal:
                sql.Append(literal.Literal.Text);
                break;
            case BinaryExpression binary:
                AppendExpression(sql, binary.Left);
                sql.Append(' ');
                sql.Append(FormatOperator(binary.OperatorToken));
                sql.Append(' ');
                AppendExpression(sql, binary.Right);
                break;
            default:
                throw new InvalidOperationException($"Unknown expression {expression.GetType().Name}");
        }
    }

    private static void AppendIdentifier(StringBuilder sql, SyntaxToken token) =>
        sql.Append(token.Text.ToLowerInvariant());

    private const string EqualsOperator = "=";

    private static string FormatOperator(SyntaxToken token) => token.Kind switch
    {
        SyntaxKind.EqualsToken => EqualsOperator,
        SyntaxKind.AndKeyword => Keyword.And.ToUpperInvariant(),
        SyntaxKind.OrKeyword => Keyword.Or.ToUpperInvariant(),
        _ => token.Text,
    };
}
