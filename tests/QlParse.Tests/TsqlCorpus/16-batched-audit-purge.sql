-- expect: Batch(TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlDeclareStatement,TsqlWhileStatement,TsqlWhileStatement,TsqlPrintStatement)
SET NOCOUNT ON;
SET DEADLOCK_PRIORITY LOW;

DECLARE @BatchSize int = 5000,
        @Cutoff datetime2(0) = DATEADD(MONTH, -18, SYSUTCDATETIME()),
        @Deleted int = 1,
        @Total bigint = 0,
        @Iterations int = 0,
        @MaxIterations int = 2000;

WHILE 1 = 1
BEGIN
    SET @Iterations += 1;

    IF @Iterations > @MaxIterations
    BEGIN
        RAISERROR('Iteration limit reached, stopping. Deleted so far: %I64d', 10, 1, @Total) WITH NOWAIT;
        BREAK;
    END

    DELETE TOP (@BatchSize) FROM dbo.AuditLog
    WHERE date < @Cutoff;

    SET @Deleted = @@ROWCOUNT;
    SET @Total += @Deleted;

    IF @Deleted = 0
        BREAK;

    IF @Iterations % 10 <> 0
    BEGIN
        WAITFOR DELAY '00:00:00.200';
        CONTINUE;
    END

    RAISERROR('Deleted %I64d rows so far...', 0, 1, @Total) WITH NOWAIT;
    CHECKPOINT;
    WAITFOR DELAY '00:00:02';
END

-- clean up orphaned detail rows in batches using a join
WHILE EXISTS (SELECT 1 FROM dbo.AuditLogDetail d WHERE NOT EXISTS (SELECT 1 FROM dbo.AuditLog a WHERE a.AuditLogID = d.AuditLogID))
BEGIN
    DELETE TOP (1000) d
    FROM dbo.AuditLogDetail AS d
    LEFT JOIN dbo.AuditLog AS a ON a.AuditLogID = d.AuditLogID
    WHERE a.AuditLogID IS NULL;

    IF @@ROWCOUNT < 1000 BREAK;
END

PRINT CONCAT('Purge complete: ', @Total, ' rows in ', @Iterations, ' iterations.');
