namespace QlParse;

// Workstream E: EXEC and module (procedure, function, trigger, aggregate, view) nodes.
public partial class SqlVisitor
{
    public virtual void VisitTsqlExecuteStatement(TsqlExecuteStatement node)
    {
        foreach (var argument in node.Arguments)
        {
            VisitTsqlExecuteArgument(argument);
        }

        foreach (var option in node.Options)
        {
            VisitTsqlExecuteOption(option);
        }
    }

    public virtual void VisitTsqlExecuteArgument(TsqlExecuteArgument node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitTsqlExecuteOption(TsqlExecuteOption node)
    {
        foreach (var resultSet in node.ResultSets)
        {
            VisitTsqlResultSetDefinition(resultSet);
        }
    }

    public virtual void VisitTsqlResultSetDefinition(TsqlResultSetDefinition node)
    {
        foreach (var column in node.Columns)
        {
            VisitTsqlResultSetColumn(column);
        }
    }

    public virtual void VisitTsqlResultSetColumn(TsqlResultSetColumn node) => VisitDataType(node.DataType);

    public virtual void VisitTsqlExecuteStringStatement(TsqlExecuteStringStatement node)
    {
        Visit(node.Command);
        VisitTsqlExpressions(node.Arguments);
    }

    public virtual void VisitTsqlParameter(TsqlParameter node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }

        if (node.Default is not null)
        {
            Visit(node.Default);
        }
    }

    public virtual void VisitTsqlParameters(IReadOnlyList<TsqlParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            VisitTsqlParameter(parameter);
        }
    }

    public virtual void VisitTsqlOptions(IReadOnlyList<TsqlOption> options)
    {
        foreach (var option in options)
        {
            VisitTsqlOption(option);
        }
    }

    public virtual void VisitTsqlExternalName(TsqlExternalName node)
    {
        return;
    }

    public virtual void VisitTsqlCreateProcedureStatement(TsqlCreateProcedureStatement node)
    {
        VisitTsqlParameters(node.Parameters);
        VisitTsqlOptions(node.Options);
        VisitTsqlStatements(node.Statements);
        if (node.ExternalName is not null)
        {
            VisitTsqlExternalName(node.ExternalName);
        }
    }

    public virtual void VisitTsqlCreateFunctionStatement(TsqlCreateFunctionStatement node)
    {
        VisitTsqlParameters(node.Parameters);
        if (node.ReturnType is not null)
        {
            VisitDataType(node.ReturnType);
        }

        if (node.ReturnTable is not null)
        {
            VisitTsqlTableDefinition(node.ReturnTable);
        }

        VisitTsqlOptions(node.Options);
        if (node.Body is not null)
        {
            Visit(node.Body);
        }

        if (node.ExternalName is not null)
        {
            VisitTsqlExternalName(node.ExternalName);
        }
    }

    public virtual void VisitTsqlCreateTriggerStatement(TsqlCreateTriggerStatement node)
    {
        VisitTsqlOptions(node.Options);
        VisitTsqlStatements(node.Statements);
        if (node.ExternalName is not null)
        {
            VisitTsqlExternalName(node.ExternalName);
        }
    }

    public virtual void VisitTsqlCreateOrAlterViewStatement(TsqlCreateOrAlterViewStatement node) =>
        VisitCreateViewStatement(node.View);

    public virtual void VisitTsqlCreateAggregateStatement(TsqlCreateAggregateStatement node)
    {
        VisitTsqlParameters(node.Parameters);
        VisitDataType(node.ReturnType);
        VisitTsqlExternalName(node.ExternalName);
    }

    public virtual void VisitTsqlToggleTriggerStatement(TsqlToggleTriggerStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateEventNotificationStatement(TsqlCreateEventNotificationStatement node)
    {
        return;
    }
}
