-- expect: Script(TsqlUseStatement | TsqlCreateProcedureStatement | TsqlDeclareStatement,TsqlExecuteStatement,SelectStatement)
USE WideWorldImporters;
GO

CREATE OR ALTER PROCEDURE Warehouse.usp_SyncStockHoldings
    @AsOf datetime2(7) = NULL,
    @RowsAffected int = 0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AsOf = COALESCE(@AsOf, SYSUTCDATETIME());

    DECLARE @changes TABLE (
        merge_action nvarchar(10),
        StockItemID int,
        old_qty int,
        new_qty int
    );

    BEGIN TRAN;

    MERGE Warehouse.StockItemHoldings AS t
    USING Warehouse.StockItemHoldings_Staging AS s
       ON t.StockItemID = s.StockItemID
    WHEN MATCHED AND t.QuantityOnHand <> s.QuantityOnHand THEN
        UPDATE SET QuantityOnHand = s.QuantityOnHand,
                   LastEditedWhen = @AsOf
    WHEN NOT MATCHED THEN
        INSERT (StockItemID, QuantityOnHand, BinLocation, LastStocktakeQuantity, LastCostPrice, ReorderLevel, TargetStockLevel, LastEditedBy, LastEditedWhen)
        VALUES (s.StockItemID, s.QuantityOnHand, s.BinLocation, 0, 0, 10, 100, 1, @AsOf)
    OUTPUT $action, inserted.StockItemID, deleted.QuantityOnHand, inserted.QuantityOnHand INTO @changes;

    SET @RowsAffected = @@ROWCOUNT;

    COMMIT;

    SELECT c.*, si.StockItemName
    FROM @changes AS c
    INNER JOIN Warehouse.StockItems AS si ON si.StockItemID = c.StockItemID
    ORDER BY c.merge_action, si.StockItemName;
END;
GO

DECLARE @n int;
EXEC Warehouse.usp_SyncStockHoldings @RowsAffected = @n OUTPUT;
SELECT @n AS RowsMerged;
GO
