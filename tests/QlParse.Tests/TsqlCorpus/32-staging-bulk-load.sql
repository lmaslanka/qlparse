-- expect: Batch(TsqlDropObjectsStatement,TsqlDropObjectsStatement,TsqlDropObjectsStatement,TsqlDropObjectsStatement,TsqlDropIndexStatement,CreateTableStatement,TsqlBulkInsertStatement,SelectStatement,TsqlDeleteStatement,CreateIndexStatement,SelectStatement,TruncateStatement)
-- Rebuild staging area for the nightly customer load
DROP TABLE IF EXISTS staging.CustomerLoad;
DROP TABLE IF EXISTS #Rejected;
DROP VIEW IF EXISTS staging.vw_LoadSummary;
DROP PROCEDURE IF EXISTS staging.usp_ValidateLoad;
DROP INDEX IF EXISTS IX_Customer_Email ON dbo.Customer;

CREATE TABLE staging.CustomerLoad
(
    RowID int IDENTITY(1,1) PRIMARY KEY,
    SourceSystem varchar(20) NOT NULL,
    ExternalID varchar(50) NOT NULL,
    Email nvarchar(256) NULL,
    BirthDate varchar(30) NULL,
    LoadedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

BULK INSERT staging.CustomerLoad
FROM 'D:\Feeds\customers_20240314.csv'
WITH (
    FORMAT = 'CSV',
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '0x0a',
    CODEPAGE = '65001',
    TABLOCK,
    MAXERRORS = 10,
    ERRORFILE = 'D:\Feeds\errors\customers_20240314.err'
);

SELECT RowID, ExternalID, Email, BirthDate
INTO #Rejected
FROM staging.CustomerLoad
WHERE TRY_CONVERT(date, BirthDate, 23) IS NULL
   OR Email NOT LIKE '%_@_%._%';

DELETE cl
FROM staging.CustomerLoad AS cl
WHERE EXISTS (SELECT 1 FROM #Rejected AS r WHERE r.RowID = cl.RowID);

CREATE UNIQUE NONCLUSTERED INDEX IX_CustomerLoad_External ON staging.CustomerLoad (SourceSystem, ExternalID) WITH (IGNORE_DUP_KEY = ON);

SELECT COUNT(*) AS Rejected FROM #Rejected;
TRUNCATE TABLE dbo.CustomerLoadErrors;
