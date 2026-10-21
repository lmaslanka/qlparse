namespace QlParse;

public partial class SqlVisitor
{
    // DO blocks and PL/pgSQL body nodes; dispatched after every ISO bucket so ISO nodes keep their dispatch cost.
    private bool TryVisitPgRoutineQuery(Query query) => TryVisitPlpgsqlQuery1(query) || TryVisitPlpgsqlQuery2(query);

    private bool TryVisitPlpgsqlQuery1(Query query)
    {
        switch (query)
        {
            case PgDoStatement node:
                VisitPgDoStatement(node);
                return true;
            case PlpgsqlBlock node:
                VisitPlpgsqlBlock(node);
                return true;
            case PlpgsqlVariableDeclaration node:
                VisitPlpgsqlVariableDeclaration(node);
                return true;
            case PlpgsqlAliasDeclaration node:
                VisitPlpgsqlAliasDeclaration(node);
                return true;
            case PlpgsqlCursorDeclaration node:
                VisitPlpgsqlCursorDeclaration(node);
                return true;
            case PlpgsqlAssignment node:
                VisitPlpgsqlAssignment(node);
                return true;
            case PlpgsqlLoopStatement node:
                VisitPlpgsqlLoopStatement(node);
                return true;
            case PlpgsqlWhileStatement node:
                VisitPlpgsqlWhileStatement(node);
                return true;
            case PlpgsqlForRangeStatement node:
                VisitPlpgsqlForRangeStatement(node);
                return true;
            case PlpgsqlForQueryStatement node:
                VisitPlpgsqlForQueryStatement(node);
                return true;
            case PlpgsqlForCursorStatement node:
                VisitPlpgsqlForCursorStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitPlpgsqlQuery2(Query query)
    {
        switch (query)
        {
            case PlpgsqlForeachStatement node:
                VisitPlpgsqlForeachStatement(node);
                return true;
            case PlpgsqlExitStatement node:
                VisitPlpgsqlExitStatement(node);
                return true;
            case PlpgsqlReturnStatement node:
                VisitPlpgsqlReturnStatement(node);
                return true;
            case PlpgsqlNullStatement node:
                VisitPlpgsqlNullStatement(node);
                return true;
            case PlpgsqlRaiseStatement node:
                VisitPlpgsqlRaiseStatement(node);
                return true;
            case PlpgsqlAssertStatement node:
                VisitPlpgsqlAssertStatement(node);
                return true;
            case PlpgsqlPerformStatement node:
                VisitPlpgsqlPerformStatement(node);
                return true;
            case PlpgsqlExecuteStatement node:
                VisitPlpgsqlExecuteStatement(node);
                return true;
            case PlpgsqlOpenStatement node:
                VisitPlpgsqlOpenStatement(node);
                return true;
            case PlpgsqlFetchStatement node:
                VisitPlpgsqlFetchStatement(node);
                return true;
            case PlpgsqlSqlStatement node:
                VisitPlpgsqlSqlStatement(node);
                return true;
            default:
                return false;
        }
    }

    // The Postgres-only parts of CREATE FUNCTION / PROCEDURE: RETURNS TABLE columns and TRANSFORM types.
    public virtual void VisitPgRoutineDefinition(CreateRoutineStatement node)
    {
        if (node.Postgres?.ReturnsTable is { } returnsTable)
        {
            VisitPgReturnsTable(returnsTable);
        }

        foreach (var characteristic in node.Characteristics)
        {
            if (characteristic.Postgres is null)
            {
                continue;
            }

            foreach (var type in characteristic.Postgres.Types)
            {
                VisitDataType(type);
            }
        }
    }

    public virtual void VisitPgRoutineParameterExtras(PgRoutineParameterExtras node)
    {
        if (node.Default is not null)
        {
            Visit(node.Default);
        }
    }

    public virtual void VisitPgReturnsTable(PgReturnsTable node)
    {
        foreach (var column in node.Columns)
        {
            VisitRoutineParameter(column);
        }
    }

    public virtual void VisitPgRoutineAttributesAction(PgRoutineAttributesAction node)
    {
        return;
    }

    public virtual void VisitPgDoStatement(PgDoStatement node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitPlpgsqlBlock(PlpgsqlBlock node)
    {
        VisitAllQueries(node.Declarations);
        VisitAllQueries(node.Statements);
        foreach (var handler in node.Handlers)
        {
            VisitPlpgsqlExceptionHandler(handler);
        }
    }

    public virtual void VisitPlpgsqlExceptionHandler(PlpgsqlExceptionHandler node) => VisitAllQueries(node.Statements);

    public virtual void VisitPlpgsqlVariableDeclaration(PlpgsqlVariableDeclaration node)
    {
        VisitDataType(node.Type);
        if (node.Default is not null)
        {
            Visit(node.Default);
        }
    }

    public virtual void VisitPlpgsqlAliasDeclaration(PlpgsqlAliasDeclaration node)
    {
        return;
    }

    public virtual void VisitPlpgsqlCursorDeclaration(PlpgsqlCursorDeclaration node)
    {
        foreach (var argument in node.Arguments)
        {
            VisitRoutineParameter(argument);
        }

        Visit(node.Query);
    }

    public virtual void VisitPlpgsqlAssignment(PlpgsqlAssignment node)
    {
        Visit(node.Target);
        Visit(node.Value);
    }

    public virtual void VisitPlpgsqlLoopBody(PlpgsqlLoopBody node) => VisitAllQueries(node.Statements);

    public virtual void VisitPlpgsqlLoopStatement(PlpgsqlLoopStatement node) => VisitPlpgsqlLoopBody(node.Body);

    public virtual void VisitPlpgsqlWhileStatement(PlpgsqlWhileStatement node)
    {
        Visit(node.Condition);
        VisitPlpgsqlLoopBody(node.Body);
    }

    public virtual void VisitPlpgsqlForRangeStatement(PlpgsqlForRangeStatement node)
    {
        Visit(node.Lower);
        Visit(node.Upper);
        if (node.Step is not null)
        {
            Visit(node.Step);
        }

        VisitPlpgsqlLoopBody(node.Body);
    }

    public virtual void VisitPlpgsqlForQueryStatement(PlpgsqlForQueryStatement node)
    {
        VisitAll(node.Targets);
        Visit(node.Query);
        VisitPlpgsqlLoopBody(node.Body);
    }

    public virtual void VisitPlpgsqlForCursorStatement(PlpgsqlForCursorStatement node)
    {
        VisitAll(node.Arguments);
        VisitPlpgsqlLoopBody(node.Body);
    }

    public virtual void VisitPlpgsqlForeachStatement(PlpgsqlForeachStatement node)
    {
        VisitAll(node.Targets);
        Visit(node.Array);
        VisitPlpgsqlLoopBody(node.Body);
    }

    public virtual void VisitPlpgsqlExitStatement(PlpgsqlExitStatement node)
    {
        if (node.Condition is not null)
        {
            Visit(node.Condition);
        }
    }

    public virtual void VisitPlpgsqlReturnStatement(PlpgsqlReturnStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }

        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitPlpgsqlNullStatement(PlpgsqlNullStatement node)
    {
        return;
    }

    public virtual void VisitPlpgsqlRaiseStatement(PlpgsqlRaiseStatement node)
    {
        VisitAll(node.Arguments);
        foreach (var option in node.Options)
        {
            Visit(option.Value);
        }
    }

    public virtual void VisitPlpgsqlAssertStatement(PlpgsqlAssertStatement node)
    {
        Visit(node.Condition);
        if (node.Message is not null)
        {
            Visit(node.Message);
        }
    }

    public virtual void VisitPlpgsqlPerformStatement(PlpgsqlPerformStatement node) => Visit(node.Query);

    public virtual void VisitPlpgsqlExecuteStatement(PlpgsqlExecuteStatement node)
    {
        Visit(node.Command);
        VisitAll(node.Targets);
        VisitAll(node.Arguments);
    }

    public virtual void VisitPlpgsqlOpenStatement(PlpgsqlOpenStatement node)
    {
        if (node.Query is not null)
        {
            Visit(node.Query);
        }

        VisitAll(node.Arguments);
    }

    public virtual void VisitPlpgsqlFetchStatement(PlpgsqlFetchStatement node)
    {
        if (node.Count is not null)
        {
            Visit(node.Count);
        }

        VisitAll(node.Targets);
    }

    public virtual void VisitPlpgsqlSqlStatement(PlpgsqlSqlStatement node)
    {
        Visit(node.Statement);
        VisitAll(node.Targets);
    }
}
