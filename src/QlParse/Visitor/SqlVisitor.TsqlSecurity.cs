namespace QlParse;

public partial class SqlVisitor
{
    // Workstream F: SQL Server-only database, object and security statement nodes.
    internal bool TryVisitTsqlSecurityQuery(Query query) => TryVisitTsqlDatabaseQuery(query) || TryVisitTsqlPrincipalQuery(query);

    private bool TryVisitTsqlDatabaseQuery(Query query)
    {
        switch (query)
        {
            case TsqlCreateDatabaseStatement node:
                VisitTsqlCreateDatabaseStatement(node);
                return true;
            case TsqlAlterDatabaseStatement node:
                VisitTsqlAlterDatabaseStatement(node);
                return true;
            case TsqlAlterDatabaseScopedConfigurationStatement node:
                VisitTsqlAlterDatabaseScopedConfigurationStatement(node);
                return true;
            case TsqlCreateSchemaStatement node:
                VisitTsqlCreateSchemaStatement(node);
                return true;
            case TsqlAlterSchemaTransferStatement node:
                VisitTsqlAlterSchemaTransferStatement(node);
                return true;
            case TsqlAlterAuthorizationStatement node:
                VisitTsqlAlterAuthorizationStatement(node);
                return true;
            case TsqlXmlSchemaCollectionStatement node:
                VisitTsqlXmlSchemaCollectionStatement(node);
                return true;
            case TsqlCreateAssemblyStatement node:
                VisitTsqlCreateAssemblyStatement(node);
                return true;
            case TsqlCreateDefaultOrRuleStatement node:
                VisitTsqlCreateDefaultOrRuleStatement(node);
                return true;
            case TsqlCreateExternalObjectStatement node:
                VisitTsqlCreateExternalObjectStatement(node);
                return true;
            case TsqlCreateExternalTableStatement node:
                VisitTsqlCreateExternalTableStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitTsqlPrincipalQuery(Query query)
    {
        switch (query)
        {
            case TsqlCreatePrincipalStatement node:
                VisitTsqlCreatePrincipalStatement(node);
                return true;
            case TsqlAlterPrincipalStatement node:
                VisitTsqlAlterPrincipalStatement(node);
                return true;
            case TsqlDropSecurableStatement node:
                VisitTsqlDropSecurableStatement(node);
                return true;
            case TsqlPermissionStatement node:
                VisitTsqlPermissionStatement(node);
                return true;
            case TsqlExecuteAsStatement node:
                VisitTsqlExecuteAsStatement(node);
                return true;
            case TsqlRevertStatement node:
                VisitTsqlRevertStatement(node);
                return true;
            case TsqlSecurityPolicyStatement node:
                VisitTsqlSecurityPolicyStatement(node);
                return true;
            case TsqlSecurityObjectStatement node:
                VisitTsqlSecurityObjectStatement(node);
                return true;
            case TsqlAuditSpecificationStatement node:
                VisitTsqlAuditSpecificationStatement(node);
                return true;
            case TsqlSignatureStatement node:
                VisitTsqlSignatureStatement(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlObjectOption(TsqlObjectOption node)
    {
        if (node.Options is not null)
        {
            VisitTsqlObjectOptionList(node.Options);
        }
    }

    public virtual void VisitTsqlObjectOptionList(TsqlObjectOptionList node) => VisitTsqlObjectOptions(node.Items);

    private void VisitTsqlObjectOptions(IReadOnlyList<TsqlObjectOption> options)
    {
        foreach (var option in options)
        {
            VisitTsqlObjectOption(option);
        }
    }

    public virtual void VisitTsqlSecurable(TsqlSecurable node)
    {
        return;
    }

    public virtual void VisitTsqlCreateDatabaseStatement(TsqlCreateDatabaseStatement node)
    {
        if (node.Containment is not null)
        {
            VisitTsqlObjectOption(node.Containment);
        }

        foreach (var filegroup in node.Filegroups)
        {
            VisitTsqlDatabaseFilegroup(filegroup);
        }

        foreach (var file in node.LogFiles)
        {
            VisitTsqlObjectOptionList(file);
        }

        VisitTsqlObjectOptions(node.Options);
    }

    public virtual void VisitTsqlDatabaseFilegroup(TsqlDatabaseFilegroup node)
    {
        foreach (var file in node.Files)
        {
            VisitTsqlObjectOptionList(file);
        }
    }

    public virtual void VisitTsqlAlterDatabaseStatement(TsqlAlterDatabaseStatement node)
    {
        foreach (var file in node.Files)
        {
            VisitTsqlObjectOptionList(file);
        }

        VisitTsqlObjectOptions(node.Options);
    }

    public virtual void VisitTsqlAlterDatabaseScopedConfigurationStatement(TsqlAlterDatabaseScopedConfigurationStatement node) =>
        VisitTsqlObjectOption(node.Option);

    public virtual void VisitTsqlCreateSchemaStatement(TsqlCreateSchemaStatement node)
    {
        foreach (var element in node.Elements)
        {
            Visit(element);
        }
    }

    public virtual void VisitTsqlAlterSchemaTransferStatement(TsqlAlterSchemaTransferStatement node) =>
        VisitTsqlSecurable(node.Securable);

    public virtual void VisitTsqlAlterAuthorizationStatement(TsqlAlterAuthorizationStatement node) =>
        VisitTsqlSecurable(node.Securable);

    public virtual void VisitTsqlXmlSchemaCollectionStatement(TsqlXmlSchemaCollectionStatement node) =>
        Visit(node.Content);

    public virtual void VisitTsqlCreateAssemblyStatement(TsqlCreateAssemblyStatement node)
    {
        foreach (var source in node.Sources)
        {
            Visit(source);
        }

        if (node.Option is not null)
        {
            VisitTsqlObjectOption(node.Option);
        }
    }

    public virtual void VisitTsqlCreateDefaultOrRuleStatement(TsqlCreateDefaultOrRuleStatement node) =>
        Visit(node.Expression);

    public virtual void VisitTsqlCreateExternalObjectStatement(TsqlCreateExternalObjectStatement node) =>
        VisitTsqlObjectOptionList(node.Options);

    public virtual void VisitTsqlCreateExternalTableStatement(TsqlCreateExternalTableStatement node)
    {
        foreach (var element in node.Elements)
        {
            Visit(element);
        }

        VisitTsqlObjectOptionList(node.Options);
        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitTsqlCreatePrincipalStatement(TsqlCreatePrincipalStatement node) =>
        VisitTsqlObjectOptions(node.Options);

    public virtual void VisitTsqlAlterPrincipalStatement(TsqlAlterPrincipalStatement node) =>
        VisitTsqlObjectOptions(node.Options);

    public virtual void VisitTsqlDropSecurableStatement(TsqlDropSecurableStatement node)
    {
        return;
    }

    public virtual void VisitTsqlPermissionStatement(TsqlPermissionStatement node)
    {
        foreach (var permission in node.Permissions)
        {
            VisitTsqlPermission(permission);
        }

        if (node.Securable is not null)
        {
            VisitTsqlSecurable(node.Securable);
        }
    }

    public virtual void VisitTsqlPermission(TsqlPermission node)
    {
        return;
    }

    public virtual void VisitTsqlExecuteAsStatement(TsqlExecuteAsStatement node)
    {
        return;
    }

    public virtual void VisitTsqlRevertStatement(TsqlRevertStatement node)
    {
        return;
    }

    public virtual void VisitTsqlSecurityPolicyStatement(TsqlSecurityPolicyStatement node)
    {
        foreach (var predicate in node.Predicates)
        {
            VisitTsqlSecurityPredicate(predicate);
        }

        if (node.Options is not null)
        {
            VisitTsqlObjectOptionList(node.Options);
        }
    }

    public virtual void VisitTsqlSecurityPredicate(TsqlSecurityPredicate node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
    }

    public virtual void VisitTsqlSecurityObjectStatement(TsqlSecurityObjectStatement node)
    {
        VisitTsqlObjectOptions(node.Clauses);
        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitTsqlAuditSpecificationStatement(TsqlAuditSpecificationStatement node)
    {
        foreach (var action in node.Actions)
        {
            VisitTsqlAuditAction(action);
        }

        if (node.Options is not null)
        {
            VisitTsqlObjectOptionList(node.Options);
        }
    }

    public virtual void VisitTsqlAuditAction(TsqlAuditAction node)
    {
        foreach (var action in node.Actions)
        {
            VisitTsqlPermission(action);
        }

        if (node.Securable is not null)
        {
            VisitTsqlSecurable(node.Securable);
        }
    }

    public virtual void VisitTsqlSignatureStatement(TsqlSignatureStatement node)
    {
        VisitTsqlSecurable(node.Securable);
        VisitTsqlObjectOptions(node.Signers);
    }
}
