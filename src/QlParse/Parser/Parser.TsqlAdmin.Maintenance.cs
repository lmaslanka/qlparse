namespace QlParse;

// Workstream G: DBCC, BACKUP / RESTORE, CHECKPOINT, KILL, RECONFIGURE, SHUTDOWN, SETUSER and the legacy
// READTEXT / WRITETEXT / UPDATETEXT statements.
internal sealed partial class Parser
{
    // DBCC command [(argument, ...)] [WITH option, ...]
    private TsqlDbccStatement ParseTsqlDbcc()
    {
        var dbccKeyword = Advance();
        var command = Expect(SyntaxKind.Identifier);
        SyntaxToken? openParen = null;
        SyntaxToken? closeParen = null;
        IReadOnlyList<Expression> arguments = [];
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var options = ParseTsqlOptionalWith();
        return new TsqlDbccStatement
        {
            Span = SourceSpan.From(dbccKeyword, options.Options?.Span ?? closeParen?.Span ?? command.Span),
            DbccKeyword = dbccKeyword,
            Command = command,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
        };
    }

    internal readonly record struct TsqlWithClause(SyntaxToken? WithKeyword, TsqlOptionList? Options);

    // [WITH option, ...] at the end of a statement.
    internal TsqlWithClause ParseTsqlOptionalWith()
    {
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return default;
        }

        var withKeyword = Advance();
        return new TsqlWithClause(withKeyword, ParseTsqlWithOptions());
    }

    // BACKUP CERTIFICATE / MASTER KEY / ... belong to the security statements.
    private bool IsTsqlBackupTarget() => NextEquals(Keyword.AdminDatabase) || NextEquals(Keyword.AdminLog);

    private bool IsTsqlRestoreTarget()
    {
        if (_index >= _tokens.Count || _tokens[_index].Kind != SyntaxKind.Identifier)
        {
            return false;
        }

        var text = _tokens[_index].TextOf(_source);
        return TextEquals(text, Keyword.AdminDatabase) || TextEquals(text, Keyword.AdminLog)
            || TextEquals(text, Keyword.AdminHeaderOnly) || TextEquals(text, Keyword.AdminFileListOnly)
            || TextEquals(text, Keyword.AdminVerifyOnly) || TextEquals(text, Keyword.AdminLabelOnly)
            || TextEquals(text, Keyword.AdminRewindOnly);
    }

    // BACKUP {DATABASE | LOG} name [file_or_filegroup, ...] TO device, ... [MIRROR TO device, ...] [WITH ...]
    private TsqlBackupStatement ParseTsqlBackup()
    {
        var backupKeyword = Advance();
        var targetKind = Advance();
        var database = ParseTsqlNameOrVariable();
        var files = IdentifierEquals(Keyword.To) ? null : ParseTsqlBareOptionList(allowConnector: false);
        var toKeyword = ExpectIdent(Keyword.To);
        var devices = ParseTsqlBackupDevices();
        var mirrors = new List<TsqlBackupMirror>();
        while (IdentifierEquals(Keyword.AdminMirror) && NextEquals(Keyword.To))
        {
            var mirrorKeyword = Advance();
            var mirrorTo = Advance();
            var mirrorDevices = ParseTsqlBackupDevices();
            mirrors.Add(new TsqlBackupMirror
            {
                Span = SourceSpan.From(mirrorKeyword, mirrorDevices[^1].Span),
                MirrorKeyword = mirrorKeyword,
                ToKeyword = mirrorTo,
                Devices = mirrorDevices,
            });
        }

        var options = ParseTsqlOptionalWith();
        var end = options.Options?.Span ?? (mirrors.Count > 0 ? mirrors[^1].Span : devices[^1].Span);
        return new TsqlBackupStatement
        {
            Span = SourceSpan.From(backupKeyword, end),
            BackupKeyword = backupKeyword,
            TargetKind = targetKind,
            Database = database,
            Files = files,
            ToKeyword = toKeyword,
            Devices = devices,
            Mirrors = mirrors,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
        };
    }

    // RESTORE {DATABASE | LOG} name [files] [FROM device, ...] [WITH ...]
    // RESTORE {HEADERONLY | FILELISTONLY | VERIFYONLY | LABELONLY | REWINDONLY} FROM device, ... [WITH ...]
    private TsqlRestoreStatement ParseTsqlRestore()
    {
        var restoreKeyword = Advance();
        var targetKind = Advance();
        SyntaxToken? database = null;
        TsqlOptionList? files = null;
        if (TokenEquals(targetKind, Keyword.AdminDatabase) || TokenEquals(targetKind, Keyword.AdminLog))
        {
            database = ParseTsqlNameOrVariable();
            files = _current.Kind == SyntaxKind.Identifier ? ParseTsqlBareOptionList(allowConnector: false) : null;
        }

        SyntaxToken? fromKeyword = null;
        IReadOnlyList<TsqlBackupDevice> devices = [];
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            devices = ParseTsqlBackupDevices();
        }

        var options = ParseTsqlOptionalWith();
        var end = options.Options?.Span
            ?? (devices.Count > 0 ? devices[^1].Span : files?.Span ?? database?.Span ?? targetKind.Span);
        return new TsqlRestoreStatement
        {
            Span = SourceSpan.From(restoreKeyword, end),
            RestoreKeyword = restoreKeyword,
            TargetKind = targetKind,
            Database = database,
            Files = files,
            FromKeyword = fromKeyword,
            Devices = devices,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
        };
    }

    private List<TsqlBackupDevice> ParseTsqlBackupDevices()
    {
        var devices = new List<TsqlBackupDevice> { ParseTsqlBackupDevice() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            devices.Add(ParseTsqlBackupDevice());
        }

        return devices;
    }

    // {DISK | TAPE | URL | DATABASE_SNAPSHOT} = {'path' | @variable} | logical_device | @variable
    private TsqlBackupDevice ParseTsqlBackupDevice()
    {
        if (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.EqualsToken)
        {
            var deviceType = Advance();
            var equalsToken = Advance();
            var path = _current.Kind is SyntaxKind.String or SyntaxKind.EmbeddedHost
                ? Advance()
                : throw new SqlParseException($"Expected device path, found {_current.Kind}", _current.Position);
            return new TsqlBackupDevice
            {
                Span = SourceSpan.From(deviceType, path),
                DeviceType = deviceType,
                EqualsToken = equalsToken,
                Name = path,
            };
        }

        var name = ParseTsqlNameOrVariable();
        return new TsqlBackupDevice { Span = name.Span, Name = name };
    }

    // CHECKPOINT [checkpoint_duration]
    private TsqlCheckpointStatement ParseTsqlCheckpoint()
    {
        var checkpointKeyword = Advance();
        var duration = _current.Kind == SyntaxKind.Number ? Advance() : (SyntaxToken?)null;
        return new TsqlCheckpointStatement
        {
            Span = duration is SyntaxToken value ? SourceSpan.From(checkpointKeyword, value) : checkpointKeyword.Span,
            CheckpointKeyword = checkpointKeyword,
            Duration = duration,
        };
    }

    // KILL {session_id | 'uow' | @variable} [WITH STATUSONLY]
    // KILL QUERY NOTIFICATION SUBSCRIPTION {ALL | id} | KILL STATS JOB id
    private TsqlKillStatement ParseTsqlKill()
    {
        var killKeyword = Advance();
        var qualifier = new List<SyntaxToken>();
        if (IdentifierEquals(Keyword.AdminQuery) && NextEquals(Keyword.AdminNotification))
        {
            qualifier.Add(Advance());
            qualifier.Add(Advance());
            qualifier.Add(Expect(SyntaxKind.Identifier));
        }
        else
        {
            if (IdentifierEquals(Keyword.AdminStats) && NextEquals(Keyword.AdminJob))
            {
                qualifier.Add(Advance());
                qualifier.Add(Advance());
            }
        }

        var target = _current.Kind is SyntaxKind.Number or SyntaxKind.String or SyntaxKind.EmbeddedHost or SyntaxKind.AllKeyword
            ? Advance()
            : throw new SqlParseException($"Expected session id, found {_current.Kind}", _current.Position);
        SyntaxToken? withKeyword = null;
        SyntaxToken? statusOnly = null;
        if (_current.Kind == SyntaxKind.WithKeyword && NextEquals(Keyword.AdminStatusOnly))
        {
            withKeyword = Advance();
            statusOnly = Advance();
        }

        return new TsqlKillStatement
        {
            Span = SourceSpan.From(killKeyword, statusOnly ?? target),
            KillKeyword = killKeyword,
            Qualifier = qualifier,
            Target = target,
            WithKeyword = withKeyword,
            StatusOnlyKeyword = statusOnly,
        };
    }

    // RECONFIGURE [WITH OVERRIDE]
    private TsqlReconfigureStatement ParseTsqlReconfigure()
    {
        var reconfigureKeyword = Advance();
        var (withKeyword, overrideKeyword) = ParseTsqlWithWord(Keyword.AdminOverride);
        return new TsqlReconfigureStatement
        {
            Span = SourceSpan.From(reconfigureKeyword, overrideKeyword ?? reconfigureKeyword),
            ReconfigureKeyword = reconfigureKeyword,
            WithKeyword = withKeyword,
            OverrideKeyword = overrideKeyword,
        };
    }

    // SHUTDOWN [WITH NOWAIT]
    private TsqlShutdownStatement ParseTsqlShutdown()
    {
        var shutdownKeyword = Advance();
        var (withKeyword, nowaitKeyword) = ParseTsqlWithWord(Keyword.AdminNowait);
        return new TsqlShutdownStatement
        {
            Span = SourceSpan.From(shutdownKeyword, nowaitKeyword ?? shutdownKeyword),
            ShutdownKeyword = shutdownKeyword,
            WithKeyword = withKeyword,
            NowaitKeyword = nowaitKeyword,
        };
    }

    // SETUSER ['user' [WITH NORESET]]
    private TsqlSetUserStatement ParseTsqlSetUser()
    {
        var setUserKeyword = Advance();
        if (_current.Kind != SyntaxKind.String)
        {
            return new TsqlSetUserStatement { Span = setUserKeyword.Span, SetUserKeyword = setUserKeyword };
        }

        var user = Advance();
        var (withKeyword, noResetKeyword) = ParseTsqlWithWord(Keyword.AdminNoReset);
        return new TsqlSetUserStatement
        {
            Span = SourceSpan.From(setUserKeyword, noResetKeyword ?? user),
            SetUserKeyword = setUserKeyword,
            User = user,
            WithKeyword = withKeyword,
            NoResetKeyword = noResetKeyword,
        };
    }

    // `WITH word` for one fixed word, or nothing.
    private (SyntaxToken? WithKeyword, SyntaxToken? Word) ParseTsqlWithWord(string word)
    {
        if (_current.Kind != SyntaxKind.WithKeyword || !NextEquals(word))
        {
            return (null, null);
        }

        var withKeyword = Advance();
        return (withKeyword, Advance());
    }

    // READTEXT table.column text_pointer offset size [HOLDLOCK]
    private TsqlReadTextStatement ParseTsqlReadText()
    {
        var readTextKeyword = Advance();
        var column = ParseQualifiedName();
        var textPointer = ParseExpression();
        var offset = ParseExpression();
        var size = ParseExpression();
        var holdLock = IdentifierEquals(Keyword.AdminHoldLock) ? Advance() : (SyntaxToken?)null;
        return new TsqlReadTextStatement
        {
            Span = SourceSpan.From(readTextKeyword, holdLock?.Span ?? size.Span),
            ReadTextKeyword = readTextKeyword,
            Column = column,
            TextPointer = textPointer,
            Offset = offset,
            Size = size,
            HoldLockKeyword = holdLock,
        };
    }

    // WRITETEXT [BULK] table.column text_pointer [WITH LOG] data
    private TsqlWriteTextStatement ParseTsqlWriteText()
    {
        var writeTextKeyword = Advance();
        var bulkKeyword = IdentifierEquals(Keyword.AdminBulk) ? Advance() : (SyntaxToken?)null;
        var column = ParseQualifiedName();
        var textPointer = ParseExpression();
        var (withKeyword, logKeyword) = ParseTsqlWithWord(Keyword.AdminLog);
        var data = ParseExpression();
        return new TsqlWriteTextStatement
        {
            Span = SourceSpan.From(writeTextKeyword, data.Span),
            WriteTextKeyword = writeTextKeyword,
            BulkKeyword = bulkKeyword,
            Column = column,
            TextPointer = textPointer,
            WithKeyword = withKeyword,
            LogKeyword = logKeyword,
            Data = data,
        };
    }

    // UPDATETEXT [BULK] table.column text_pointer {NULL | offset} {NULL | delete_length} [WITH LOG]
    //     [data | table.source_column source_text_pointer]
    private TsqlUpdateTextStatement ParseTsqlUpdateText()
    {
        var updateTextKeyword = Advance();
        var bulkKeyword = IdentifierEquals(Keyword.AdminBulk) ? Advance() : (SyntaxToken?)null;
        var column = ParseQualifiedName();
        var textPointer = ParseExpression();
        var insertOffset = ParseExpression();
        var deleteLength = ParseExpression();
        var (withKeyword, logKeyword) = ParseTsqlWithWord(Keyword.AdminLog);
        Expression? data = null;
        Expression? sourceTextPointer = null;
        if (_current.Kind is SyntaxKind.String or SyntaxKind.EmbeddedHost or SyntaxKind.BinaryLiteral)
        {
            data = ParseExpression();
        }
        else
        {
            if (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.Dot)
            {
                data = ParseExpression();
                sourceTextPointer = ParseExpression();
            }
        }

        var end = sourceTextPointer?.Span ?? data?.Span ?? logKeyword?.Span ?? deleteLength.Span;
        return new TsqlUpdateTextStatement
        {
            Span = SourceSpan.From(updateTextKeyword, end),
            UpdateTextKeyword = updateTextKeyword,
            BulkKeyword = bulkKeyword,
            Column = column,
            TextPointer = textPointer,
            InsertOffset = insertOffset,
            DeleteLength = deleteLength,
            WithKeyword = withKeyword,
            LogKeyword = logKeyword,
            Data = data,
            SourceTextPointer = sourceTextPointer,
        };
    }
}
