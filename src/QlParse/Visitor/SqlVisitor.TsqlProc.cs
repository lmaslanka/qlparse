namespace QlParse;

public partial class SqlVisitor
{
    // Workstream E: SQL Server-only procedural, trigger, transaction and session statement nodes.
    internal bool TryVisitTsqlProcQuery(Query query) =>
        TryVisitTsqlProcControl(query) || TryVisitTsqlProcStatement(query) || TryVisitTsqlProcModule(query);

    private bool TryVisitTsqlProcControl(Query query)
    {
        switch (query)
        {
            case TsqlBlockStatement node:
                VisitTsqlBlockStatement(node);
                return true;
            case TsqlAtomicBlockStatement node:
                VisitTsqlAtomicBlockStatement(node);
                return true;
            case TsqlTryCatchStatement node:
                VisitTsqlTryCatchStatement(node);
                return true;
            case TsqlIfStatement node:
                VisitTsqlIfStatement(node);
                return true;
            case TsqlWhileStatement node:
                VisitTsqlWhileStatement(node);
                return true;
            case TsqlBreakStatement node:
                VisitTsqlBreakStatement(node);
                return true;
            case TsqlContinueStatement node:
                VisitTsqlContinueStatement(node);
                return true;
            case TsqlGotoStatement node:
                VisitTsqlGotoStatement(node);
                return true;
            case TsqlLabelStatement node:
                VisitTsqlLabelStatement(node);
                return true;
            case TsqlReturnStatement node:
                VisitTsqlReturnStatement(node);
                return true;
            case TsqlWaitforStatement node:
                VisitTsqlWaitforStatement(node);
                return true;
            case TsqlThrowStatement node:
                VisitTsqlThrowStatement(node);
                return true;
            case TsqlRaiserrorStatement node:
                VisitTsqlRaiserrorStatement(node);
                return true;
            case TsqlPrintStatement node:
                VisitTsqlPrintStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitTsqlProcStatement(Query query)
    {
        switch (query)
        {
            case TsqlDeclareStatement node:
                VisitTsqlDeclareStatement(node);
                return true;
            case TsqlDeclareCursorStatement node:
                VisitTsqlDeclareCursorStatement(node);
                return true;
            case TsqlSetVariableStatement node:
                VisitTsqlSetVariableStatement(node);
                return true;
            case TsqlSetOptionStatement node:
                VisitTsqlSetOptionStatement(node);
                return true;
            case TsqlSetTransactionIsolationLevelStatement node:
                VisitTsqlSetTransactionIsolationLevelStatement(node);
                return true;
            case TsqlSetIdentityInsertStatement node:
                VisitTsqlSetIdentityInsertStatement(node);
                return true;
            case TsqlCursorStatement node:
                VisitTsqlCursorStatement(node);
                return true;
            case TsqlFetchStatement node:
                VisitTsqlFetchStatement(node);
                return true;
            case TsqlTransactionStatement node:
                VisitTsqlTransactionStatement(node);
                return true;
            case TsqlUseStatement node:
                VisitTsqlUseStatement(node);
                return true;
            case TsqlExecuteStatement node:
                VisitTsqlExecuteStatement(node);
                return true;
            case TsqlExecuteStringStatement node:
                VisitTsqlExecuteStringStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitTsqlProcModule(Query query)
    {
        switch (query)
        {
            case TsqlCreateProcedureStatement node:
                VisitTsqlCreateProcedureStatement(node);
                return true;
            case TsqlCreateFunctionStatement node:
                VisitTsqlCreateFunctionStatement(node);
                return true;
            case TsqlCreateTriggerStatement node:
                VisitTsqlCreateTriggerStatement(node);
                return true;
            case TsqlCreateOrAlterViewStatement node:
                VisitTsqlCreateOrAlterViewStatement(node);
                return true;
            case TsqlCreateAggregateStatement node:
                VisitTsqlCreateAggregateStatement(node);
                return true;
            case TsqlToggleTriggerStatement node:
                VisitTsqlToggleTriggerStatement(node);
                return true;
            case TsqlCreateEventNotificationStatement node:
                VisitTsqlCreateEventNotificationStatement(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlStatements(IReadOnlyList<Query> statements)
    {
        foreach (var statement in statements)
        {
            Visit(statement);
        }
    }

    public virtual void VisitTsqlBlockStatement(TsqlBlockStatement node) => VisitTsqlStatements(node.Statements);

    public virtual void VisitTsqlAtomicBlockStatement(TsqlAtomicBlockStatement node)
    {
        foreach (var option in node.Options)
        {
            VisitTsqlOption(option);
        }

        VisitTsqlStatements(node.Statements);
    }

    public virtual void VisitTsqlTryCatchStatement(TsqlTryCatchStatement node)
    {
        VisitTsqlStatements(node.TryStatements);
        VisitTsqlStatements(node.CatchStatements);
    }

    public virtual void VisitTsqlIfStatement(TsqlIfStatement node)
    {
        Visit(node.Condition);
        Visit(node.Then);
        if (node.Else is not null)
        {
            Visit(node.Else);
        }
    }

    public virtual void VisitTsqlWhileStatement(TsqlWhileStatement node)
    {
        Visit(node.Condition);
        Visit(node.Body);
    }

    public virtual void VisitTsqlBreakStatement(TsqlBreakStatement node)
    {
        return;
    }

    public virtual void VisitTsqlContinueStatement(TsqlContinueStatement node)
    {
        return;
    }

    public virtual void VisitTsqlGotoStatement(TsqlGotoStatement node)
    {
        return;
    }

    public virtual void VisitTsqlLabelStatement(TsqlLabelStatement node)
    {
        return;
    }

    public virtual void VisitTsqlReturnStatement(TsqlReturnStatement node)
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

    public virtual void VisitTsqlWaitforStatement(TsqlWaitforStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }

        if (node.Statement is not null)
        {
            Visit(node.Statement);
        }

        if (node.Timeout is not null)
        {
            Visit(node.Timeout);
        }
    }

    public virtual void VisitTsqlThrowStatement(TsqlThrowStatement node) => VisitTsqlExpressions(node.Arguments);

    public virtual void VisitTsqlRaiserrorStatement(TsqlRaiserrorStatement node) => VisitTsqlExpressions(node.Arguments);

    public virtual void VisitTsqlPrintStatement(TsqlPrintStatement node) => Visit(node.Value);

    public virtual void VisitTsqlExpressions(IReadOnlyList<Expression> expressions)
    {
        foreach (var expression in expressions)
        {
            Visit(expression);
        }
    }

    public virtual void VisitTsqlDeclareStatement(TsqlDeclareStatement node)
    {
        foreach (var variable in node.Variables)
        {
            VisitTsqlVariableDeclaration(variable);
        }
    }

    public virtual void VisitTsqlVariableDeclaration(TsqlVariableDeclaration node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }

        if (node.Table is not null)
        {
            VisitTsqlTableDefinition(node.Table);
        }

        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitTsqlTableDefinition(TsqlTableDefinition node)
    {
        foreach (var element in node.Elements)
        {
            Visit(element);
        }
    }

    public virtual void VisitTsqlDeclareCursorStatement(TsqlDeclareCursorStatement node) => VisitTsqlCursorDefinition(node.Cursor);

    public virtual void VisitTsqlCursorDefinition(TsqlCursorDefinition node) => Visit(node.Query);

    public virtual void VisitTsqlSetVariableStatement(TsqlSetVariableStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }

        if (node.Cursor is not null)
        {
            VisitTsqlCursorDefinition(node.Cursor);
        }
    }

    public virtual void VisitTsqlSetOptionStatement(TsqlSetOptionStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitTsqlSetTransactionIsolationLevelStatement(TsqlSetTransactionIsolationLevelStatement node)
    {
        return;
    }

    public virtual void VisitTsqlSetIdentityInsertStatement(TsqlSetIdentityInsertStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCursorStatement(TsqlCursorStatement node)
    {
        return;
    }

    public virtual void VisitTsqlFetchStatement(TsqlFetchStatement node)
    {
        if (node.Offset is not null)
        {
            Visit(node.Offset);
        }
    }

    public virtual void VisitTsqlTransactionStatement(TsqlTransactionStatement node)
    {
        if (node.Durability is not null)
        {
            VisitTsqlOption(node.Durability);
        }
    }

    public virtual void VisitTsqlUseStatement(TsqlUseStatement node)
    {
        return;
    }

    public virtual void VisitTsqlOption(TsqlOption node)
    {
        return;
    }
}
