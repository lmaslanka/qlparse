namespace QlParse;

// Workstream F: SQL Server databases, schemas, other objects and security statements. Dispatches on the first word's
// length and text, then on the object-kind words after it, and returns null for every statement it does not own so
// the remaining T-SQL hooks and the ISO dispatch run unchanged.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlSecurityStatement() =>
        _current.Kind == SyntaxKind.Identifier ? TryParseTsqlSecurityWord(_current.TextOf(_source)) : null;

    private Query? TryParseTsqlSecurityWord(ReadOnlySpan<char> text) => text.Length switch
    {
        Len3 => TextEquals(text, Keyword.Add) && IsTsqlSignatureNext() ? ParseTsqlSignature() : null,
        Len4 => TryParseTsqlSecurityLen4(text),
        Len5 => TryParseTsqlSecurityLen5(text),
        Len6 => TryParseTsqlSecurityLen6(text),
        Len7 => TextEquals(text, Keyword.Execute) ? TryParseTsqlExecuteAs() : null,
        _ => null,
    };

    private Query? TryParseTsqlSecurityLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.SecDeny))
        {
            return ParseTsqlPermissionStatement();
        }

        if (TextEquals(text, Keyword.Drop))
        {
            return TryParseTsqlSecurityDrop();
        }

        if (TextEquals(text, Keyword.Open))
        {
            return TryParseTsqlOpenOrCloseKey();
        }

        return TextEquals(text, Keyword.Exec) ? TryParseTsqlExecuteAs() : null;
    }

    private Query? TryParseTsqlSecurityLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Grant))
        {
            return ParseTsqlPermissionStatement();
        }

        if (TextEquals(text, Keyword.Alter))
        {
            return TryParseTsqlSecurityAlter();
        }

        return TextEquals(text, Keyword.Close) ? TryParseTsqlOpenOrCloseKey() : null;
    }

    private Query? TryParseTsqlSecurityLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Create))
        {
            return TryParseTsqlSecurityCreate();
        }

        if (TextEquals(text, Keyword.Revoke))
        {
            return ParseTsqlPermissionStatement();
        }

        return TextEquals(text, Keyword.SecRevert) ? ParseTsqlRevert() : null;
    }

    private Query? TryParseTsqlSecurityCreate()
    {
        var length = TsqlSecurableKindLength(_index);
        if (length == 0)
        {
            return null;
        }

        var kind = _tokens[_index];
        if (IsTsqlPrincipalKind(kind))
        {
            return ParseTsqlCreatePrincipal(length);
        }

        return TryParseTsqlCreateDatabaseObject(kind, length) ?? TryParseTsqlCreateOtherObject(kind, length) ?? TryParseTsqlKeyObject(kind, length);
    }

    // DATABASE, DATABASE SCOPED CREDENTIAL, {DATABASE | SERVER} AUDIT SPECIFICATION, DATABASE ENCRYPTION KEY,
    // SERVER AUDIT, SCHEMA.
    private Query? TryParseTsqlCreateDatabaseObject(SyntaxToken kind, int length)
    {
        if (TokenEquals(kind, Keyword.UtilDatabase) && length == 1)
        {
            return ParseTsqlCreateDatabase();
        }

        return TokenEquals(kind, Keyword.Schema) ? ParseTsqlCreateSchema() : TryParseTsqlAuditOrDatabaseKind(kind, length);
    }

    // The shared CREATE / ALTER forms of DATABASE SCOPED CREDENTIAL, DATABASE ENCRYPTION KEY, SERVER AUDIT and the
    // audit specifications.
    private Query? TryParseTsqlAuditOrDatabaseKind(SyntaxToken kind, int length)
    {
        if (IsTsqlKindAt(_index + 1, Keyword.SecAudit) && length == Len3)
        {
            return ParseTsqlAuditSpecification();
        }

        if (TokenEquals(kind, Keyword.SecServer))
        {
            return length == Len2 && IsTsqlKindAt(_index + 1, Keyword.SecAudit) ? ParseTsqlSecurityObject(Len2, named: true) : null;
        }

        if (TokenEquals(kind, Keyword.UtilDatabase) && length == Len3)
        {
            return ParseTsqlSecurityObject(Len3, named: !IsTsqlKindAt(_index + 1, Keyword.SecEncryption));
        }

        return null;
    }

    // XML SCHEMA COLLECTION, ASSEMBLY, DEFAULT, RULE, EXTERNAL {DATA SOURCE | FILE FORMAT | MODEL | TABLE}, SECURITY POLICY.
    private Query? TryParseTsqlCreateOtherObject(SyntaxToken kind, int length)
    {
        if (TokenEquals(kind, Keyword.Xml))
        {
            return ParseTsqlXmlSchemaCollection();
        }

        if (TokenEquals(kind, Keyword.SecAssembly))
        {
            return ParseTsqlCreateAssembly();
        }

        if (TokenEquals(kind, Keyword.Default) || TokenEquals(kind, Keyword.DdlRule))
        {
            return ParseTsqlCreateDefaultOrRule();
        }

        if (TokenEquals(kind, Keyword.External))
        {
            return IsTsqlKindAt(_index + 1, Keyword.Table) ? ParseTsqlCreateExternalTable() : ParseTsqlCreateExternalObject(length - 1);
        }

        return TokenEquals(kind, Keyword.UtilSecurity) ? ParseTsqlSecurityPolicy() : null;
    }

    // MASTER KEY, CERTIFICATE, CREDENTIAL, {SYMMETRIC | ASYMMETRIC} KEY, COLUMN {MASTER | ENCRYPTION} KEY.
    private Query? TryParseTsqlKeyObject(SyntaxToken kind, int length)
    {
        if (TokenEquals(kind, Keyword.SecMaster))
        {
            return ParseTsqlSecurityObject(Len2, named: false);
        }

        return IsTsqlNamedKeyKind(kind) || TokenEquals(kind, Keyword.Column) ? ParseTsqlSecurityObject(length, named: true) : null;
    }

    private bool IsTsqlNamedKeyKind(SyntaxToken kind) =>
        TokenEquals(kind, Keyword.SecCertificate) || TokenEquals(kind, Keyword.SecCredential) || IsTsqlKeyPairKind(kind);

    private bool IsTsqlKeyPairKind(SyntaxToken kind) => TokenEquals(kind, Keyword.Symmetric) || TokenEquals(kind, Keyword.Asymmetric);

    private bool IsTsqlPrincipalKind(SyntaxToken kind) =>
        kind.Kind == SyntaxKind.UserKeyword || IsTsqlRoleKind(kind) || TokenEquals(kind, Keyword.SecLogin);

    private bool IsTsqlRoleKind(SyntaxToken kind) =>
        TokenEquals(kind, Keyword.Role) || TokenEquals(kind, Keyword.SecApplication)
            || (TokenEquals(kind, Keyword.SecServer) && IsTsqlKindAt(_index + 1, Keyword.Role));

    private Query? TryParseTsqlSecurityAlter()
    {
        if (NextEquals(Keyword.Authorization))
        {
            return ParseTsqlAlterAuthorization();
        }

        if (NextEquals(Keyword.UtilDatabase) && IsTsqlKindAt(_index + 1, Keyword.SecScoped) && IsTsqlKindAt(_index + Len2, Keyword.SecConfiguration))
        {
            return ParseTsqlAlterScopedConfiguration();
        }

        var length = TsqlSecurableKindLength(_index);
        if (length == 0)
        {
            return null;
        }

        var kind = _tokens[_index];
        if (IsTsqlPrincipalKind(kind))
        {
            return ParseTsqlAlterPrincipal(length);
        }

        return TryParseTsqlAlterObject(kind, length) ?? TryParseTsqlAuditOrDatabaseKind(kind, length) ?? TryParseTsqlKeyObject(kind, length);
    }

    // ALTER DATABASE name ..., ALTER SCHEMA s TRANSFER ..., ALTER XML SCHEMA COLLECTION, ALTER SECURITY POLICY.
    private Query? TryParseTsqlAlterObject(SyntaxToken kind, int length)
    {
        if (TokenEquals(kind, Keyword.UtilDatabase) && length == 1)
        {
            return ParseTsqlAlterDatabase();
        }

        if (TokenEquals(kind, Keyword.Schema))
        {
            return IsTsqlKindAt(_index + Len2, Keyword.SecTransfer) ? ParseTsqlAlterSchemaTransfer() : null;
        }

        if (TokenEquals(kind, Keyword.Xml))
        {
            return ParseTsqlXmlSchemaCollection();
        }

        return TokenEquals(kind, Keyword.UtilSecurity) ? ParseTsqlSecurityPolicy() : null;
    }

    private Query? TryParseTsqlSecurityDrop()
    {
        if (IsTsqlSignatureNext())
        {
            return ParseTsqlSignature();
        }

        var length = TsqlSecurableKindLength(_index);
        return length == 0 ? null : ParseTsqlDropSecurable(length);
    }

    // {OPEN | CLOSE} MASTER KEY ..., {OPEN | CLOSE} SYMMETRIC KEY k ..., CLOSE ALL SYMMETRIC KEYS
    private Query? TryParseTsqlOpenOrCloseKey()
    {
        if (NextEquals(Keyword.SecMaster) && IsTsqlKindAt(_index + 1, Keyword.Key))
        {
            return ParseTsqlSecurityObject(Len2, named: false);
        }

        if (NextEquals(Keyword.Symmetric) && IsTsqlKindAt(_index + 1, Keyword.Key))
        {
            return ParseTsqlSecurityObject(Len2, named: true);
        }

        if (NextKind == SyntaxKind.AllKeyword && IsTsqlKindAt(_index + 1, Keyword.Symmetric) && IsTsqlKindAt(_index + Len2, Keyword.Keys))
        {
            return ParseTsqlSecurityObject(Len3, named: false);
        }

        return null;
    }
}
