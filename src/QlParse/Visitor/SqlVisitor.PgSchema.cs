namespace QlParse;

public partial class SqlVisitor
{
    // Postgres schema-object statements; dispatched after every ISO bucket so ISO nodes keep their dispatch cost.
    private bool TryVisitPgSchemaQuery(Query query) => TryVisitPgSchemaCreate(query) || TryVisitPgSchemaOther(query);

    private bool TryVisitPgSchemaCreate(Query query)
    {
        switch (query)
        {
            case PgCreateTypeStatement node:
                VisitPgCreateTypeStatement(node);
                return true;
            case PgCreateExtensionStatement node:
                VisitPgCreateExtensionStatement(node);
                return true;
            case PgCreateLanguageStatement node:
                VisitPgCreateLanguageStatement(node);
                return true;
            case PgCreateForeignDataWrapperStatement node:
                VisitPgCreateForeignDataWrapperStatement(node);
                return true;
            case PgCreateServerStatement node:
                VisitPgCreateServerStatement(node);
                return true;
            case PgCreateForeignTableStatement node:
                VisitPgCreateForeignTableStatement(node);
                return true;
            case PgCreateOperatorStatement node:
                VisitPgCreateOperatorStatement(node);
                return true;
            case PgCreateOperatorClassStatement node:
                VisitPgCreateOperatorClassStatement(node);
                return true;
            case PgCreateAggregateStatement node:
                VisitPgCreateAggregateStatement(node);
                return true;
            case PgCreateCollationStatement node:
                VisitPgCreateCollationStatement(node);
                return true;
            case PgCreateConversionStatement node:
                VisitPgCreateConversionStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitPgSchemaOther(Query query)
    {
        switch (query)
        {
            case PgCreateStatisticsStatement node:
                VisitPgCreateStatisticsStatement(node);
                return true;
            case PgCreateAccessMethodStatement node:
                VisitPgCreateAccessMethodStatement(node);
                return true;
            case PgCreateTextSearchStatement node:
                VisitPgCreateTextSearchStatement(node);
                return true;
            case PgCreatePublicationStatement node:
                VisitPgCreatePublicationStatement(node);
                return true;
            case PgCreateSubscriptionStatement node:
                VisitPgCreateSubscriptionStatement(node);
                return true;
            case PgCreateEventTriggerStatement node:
                VisitPgCreateEventTriggerStatement(node);
                return true;
            case PgExecuteFunctionClause node:
                VisitPgExecuteFunctionClause(node);
                return true;
            case PgUserMappingStatement node:
                VisitPgUserMappingStatement(node);
                return true;
            case PgImportForeignSchemaStatement node:
                VisitPgImportForeignSchemaStatement(node);
                return true;
            case PgDropStatement node:
                VisitPgDropStatement(node);
                return true;
            case PgAlterStatement node:
                VisitPgAlterStatement(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitPgDefinitionList(PgDefinitionList node)
    {
        foreach (var element in node.Elements)
        {
            VisitPgDefinitionElement(element);
        }
    }

    public virtual void VisitPgDefinitionElement(PgDefinitionElement node)
    {
        if (node.Type is not null)
        {
            VisitDataType(node.Type);
        }
    }

    public virtual void VisitPgObjectReference(PgObjectReference node)
    {
        foreach (var argument in node.Arguments)
        {
            VisitDataType(argument.Type);
        }

        foreach (var argument in node.OrderedArguments)
        {
            VisitDataType(argument.Type);
        }
    }

    public virtual void VisitPgCreateTypeStatement(PgCreateTypeStatement node)
    {
        if (node.Definition is not null)
        {
            VisitPgDefinitionList(node.Definition);
        }
    }

    public virtual void VisitPgCreateExtensionStatement(PgCreateExtensionStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateLanguageStatement(PgCreateLanguageStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateForeignDataWrapperStatement(PgCreateForeignDataWrapperStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateServerStatement(PgCreateServerStatement node)
    {
        return;
    }

    public virtual void VisitPgUserMappingStatement(PgUserMappingStatement node)
    {
        return;
    }

    public virtual void VisitPgImportForeignSchemaStatement(PgImportForeignSchemaStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateForeignTableStatement(PgCreateForeignTableStatement node)
    {
        foreach (var column in node.Columns)
        {
            VisitPgForeignColumn(column);
        }

        VisitTableConstraints(node.Constraints);
        if (node.Partition is not null)
        {
            VisitAll(node.Partition.Values);
            VisitAll(node.Partition.ToValues);
        }
    }

    public virtual void VisitPgForeignColumn(PgForeignColumn node)
    {
        VisitDataType(node.Type);
        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }

        VisitTableConstraints(node.Checks);
    }

    public virtual void VisitPgCreateOperatorStatement(PgCreateOperatorStatement node) => VisitPgDefinitionList(node.Definition);

    public virtual void VisitPgCreateOperatorClassStatement(PgCreateOperatorClassStatement node)
    {
        if (node.Type is not null)
        {
            VisitDataType(node.Type);
        }

        foreach (var item in node.Items)
        {
            VisitPgOperatorClassItem(item);
        }
    }

    public virtual void VisitPgOperatorClassItem(PgOperatorClassItem node)
    {
        foreach (var type in node.Types)
        {
            VisitDataType(type);
        }

        if (node.Function is not null)
        {
            VisitPgObjectReference(node.Function);
        }

        if (node.StorageType is not null)
        {
            VisitDataType(node.StorageType);
        }
    }

    public virtual void VisitPgCreateAggregateStatement(PgCreateAggregateStatement node)
    {
        VisitPgObjectReference(node.Signature);
        VisitPgDefinitionList(node.Definition);
    }

    public virtual void VisitPgCreateCollationStatement(PgCreateCollationStatement node)
    {
        if (node.Definition is not null)
        {
            VisitPgDefinitionList(node.Definition);
        }
    }

    public virtual void VisitPgCreateConversionStatement(PgCreateConversionStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateStatisticsStatement(PgCreateStatisticsStatement node) => VisitAll(node.Expressions);

    public virtual void VisitPgCreateAccessMethodStatement(PgCreateAccessMethodStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateTextSearchStatement(PgCreateTextSearchStatement node) => VisitPgDefinitionList(node.Definition);

    public virtual void VisitPgCreatePublicationStatement(PgCreatePublicationStatement node)
    {
        foreach (var publicationObject in node.Objects)
        {
            VisitPgPublicationObject(publicationObject);
        }
    }

    public virtual void VisitPgPublicationObject(PgPublicationObject node)
    {
        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitPgCreateSubscriptionStatement(PgCreateSubscriptionStatement node)
    {
        return;
    }

    public virtual void VisitPgCreateEventTriggerStatement(PgCreateEventTriggerStatement node) => VisitPgExecuteFunctionClause(node.Execute);

    public virtual void VisitPgExecuteFunctionClause(PgExecuteFunctionClause node) => VisitAll(node.Arguments);

    public virtual void VisitPgDropStatement(PgDropStatement node)
    {
        foreach (var dropped in node.Objects)
        {
            VisitPgObjectReference(dropped);
        }
    }

    public virtual void VisitPgAlterStatement(PgAlterStatement node)
    {
        VisitPgObjectReference(node.Target);
        foreach (var action in node.Actions)
        {
            Visit(action);
        }
    }

    public virtual void Visit(PgAlterAction action)
    {
        switch (action)
        {
            case PgAttributeAction node when node.Type is not null:
                VisitDataType(node.Type);
                break;
            case PgDefinitionAction node:
                VisitPgDefinitionList(node.Definition);
                break;
            case PgMemberAction node:
                VisitPgObjectReference(node.Member);
                break;
            case PgOperatorFamilyAction node:
                VisitPgOperatorFamilyAction(node);
                break;
            case PgPublicationObjectsAction node:
                VisitPgPublicationObjectsAction(node);
                break;
            case PgRoutineAttributesAction node:
                VisitPgRoutineAttributesAction(node);
                break;
            case PgForeignTableAction node:
                VisitPgForeignTableAction(node);
                break;
            default:
                return;
        }
    }

    public virtual void VisitPgOperatorFamilyAction(PgOperatorFamilyAction node)
    {
        foreach (var item in node.Items)
        {
            VisitPgOperatorClassItem(item);
        }
    }

    public virtual void VisitPgPublicationObjectsAction(PgPublicationObjectsAction node)
    {
        foreach (var publicationObject in node.Objects)
        {
            VisitPgPublicationObject(publicationObject);
        }
    }
}
