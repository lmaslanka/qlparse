-- expect: Batch(TsqlSetTransactionIsolationLevelStatement,TsqlSetOptionStatement,TsqlDeclareStatement,TsqlTransactionStatement,SelectStatement,TsqlUpdateStatement,TsqlTransactionStatement,SelectStatement,TsqlSetTransactionIsolationLevelStatement,TsqlTransactionStatement,SelectStatement,SelectStatement,TsqlTransactionStatement,TsqlSetTransactionIsolationLevelStatement,TsqlDeclareStatement,TsqlUpdateStatement,SelectStatement,TsqlSetOptionStatement)
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
SET LOCK_TIMEOUT 5000;

DECLARE @NextInvoice int, @Prefix char(3) = 'INV';

BEGIN TRANSACTION;

SELECT @NextInvoice = LastValue + 1
FROM dbo.Counter WITH (UPDLOCK, HOLDLOCK)
WHERE CounterName = 'Invoice';

UPDATE dbo.Counter SET LastValue = @NextInvoice WHERE CounterName = 'Invoice';

COMMIT TRANSACTION;

SELECT @Prefix + RIGHT(REPLICATE('0', 8) + CAST(@NextInvoice AS varchar(8)), 8) AS InvoiceNumber;

SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
BEGIN TRAN;
    SELECT SUM(Balance) AS TotalBalance FROM dbo.Account;
    SELECT COUNT(*) AS OpenOrders FROM dbo.Orders WITH (READCOMMITTEDLOCK) WHERE status = 'open';
COMMIT;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- queue pattern: grab the next job without blocking other workers
DECLARE @job TABLE (JobID int, Payload nvarchar(max));

UPDATE TOP (1) q
SET q.status = 'processing', q.StartedAt = SYSUTCDATETIME(), q.Worker = HOST_NAME()
OUTPUT inserted.JobID, inserted.Payload INTO @job
FROM dbo.JobQueue AS q WITH (ROWLOCK, READPAST, UPDLOCK)
WHERE q.status = 'queued';

SELECT JobID, Payload FROM @job;
SET LOCK_TIMEOUT -1;
