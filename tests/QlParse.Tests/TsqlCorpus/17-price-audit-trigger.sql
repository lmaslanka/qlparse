-- expect: Script(TsqlSetOptionStatement | TsqlSetOptionStatement | TsqlIfStatement | TsqlCreateTriggerStatement | AlterTableStatement)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID(N'dbo.ProductPriceAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductPriceAudit
    (
        AuditID      bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductPriceAudit PRIMARY KEY,
        ProductID    int NOT NULL,
        OldPrice     money NULL,
        NewPrice     money NULL,
        ChangedBy    sysname NOT NULL CONSTRAINT DF_ProductPriceAudit_ChangedBy DEFAULT (SUSER_SNAME()),
        ChangedAt    datetime2(3) NOT NULL CONSTRAINT DF_ProductPriceAudit_ChangedAt DEFAULT (SYSDATETIME()),
        [action]     char(1) NOT NULL
    );
END
GO
CREATE TRIGGER [dbo].[trg_Product_PriceAudit]
ON [dbo].[Product]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    IF UPDATE(ListPrice) OR UPDATE(StandardCost)
    BEGIN
        INSERT INTO dbo.ProductPriceAudit (ProductID, OldPrice, NewPrice, [action])
        SELECT i.ProductID, d.ListPrice, i.ListPrice,
               CASE WHEN d.ProductID IS NULL THEN 'I' ELSE 'U' END
        FROM inserted AS i
        LEFT JOIN deleted AS d ON d.ProductID = i.ProductID
        WHERE d.ProductID IS NULL
           OR ISNULL(d.ListPrice, -1) <> ISNULL(i.ListPrice, -1);
    END

    IF EXISTS (SELECT 1 FROM inserted WHERE ListPrice < StandardCost)
    BEGIN
        RAISERROR('List price cannot be lower than standard cost.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO
ALTER TABLE [dbo].[Product] ENABLE TRIGGER [trg_Product_PriceAudit]
GO
