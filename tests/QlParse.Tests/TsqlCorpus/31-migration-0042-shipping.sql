-- expect: Script(TsqlSetOptionStatement | TsqlIfStatement | TsqlIfStatement | TsqlTransactionStatement,TsqlIfStatement,TsqlIfStatement,TsqlIfStatement,TsqlIfStatement,TsqlIfStatement,TsqlInsertStatement,TsqlTransactionStatement | TsqlSetOptionStatement)
/* Migration 0042: add shipping columns and ShipMethod lookup table */
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SchemaVersion]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SchemaVersion](
        [Version] int NOT NULL CONSTRAINT [PK_SchemaVersion] PRIMARY KEY,
        [Description] nvarchar(200) NOT NULL,
        [AppliedAt] datetime2(0) NOT NULL CONSTRAINT [DF_SchemaVersion_AppliedAt] DEFAULT (sysutcdatetime()),
        [AppliedBy] sysname NOT NULL CONSTRAINT [DF_SchemaVersion_AppliedBy] DEFAULT (suser_sname())
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE [Version] = 42)
BEGIN
    PRINT 'Migration 42 already applied, skipping.';
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Orders', 'ShipMethodID') IS NULL
    ALTER TABLE dbo.Orders ADD ShipMethodID int NULL;

IF COL_LENGTH(N'dbo.Orders', N'TrackingNumber') IS NULL
    ALTER TABLE dbo.Orders ADD TrackingNumber varchar(40) NULL, ShippedAt datetime2(0) NULL;

IF OBJECT_ID(N'dbo.ShipMethod', N'U') IS NULL
    CREATE TABLE dbo.ShipMethod (ShipMethodID int NOT NULL PRIMARY KEY, name nvarchar(50) NOT NULL, BaseRate money NOT NULL DEFAULT 0);

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'ShipVia')
    EXEC sp_rename N'dbo.Orders.ShipVia', N'ShipViaLegacy', N'COLUMN';

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_ShipMethod')
    ALTER TABLE dbo.Orders WITH CHECK ADD CONSTRAINT FK_Orders_ShipMethod FOREIGN KEY (ShipMethodID) REFERENCES dbo.ShipMethod (ShipMethodID);

INSERT INTO dbo.SchemaVersion ([Version], [Description]) VALUES (42, N'Shipping columns and ShipMethod lookup');

COMMIT TRANSACTION;
GO
SET NOEXEC OFF;
GO
