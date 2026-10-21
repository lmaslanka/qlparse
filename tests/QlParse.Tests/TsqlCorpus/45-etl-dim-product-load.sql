-- expect: Script(TsqlCreateProcedureStatement | TsqlDeclareStatement,TsqlExecuteStatement,SelectStatement)
CREATE OR ALTER PROCEDURE etl.usp_LoadDimProduct
    @BatchID int,
    @RowsInserted int = NULL OUTPUT,
    @RowsUpdated int = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT, XACT_ABORT ON;

    DECLARE @trancount int = @@TRANCOUNT, @sql nvarchar(max), @cnt int;
    DECLARE @actions TABLE (act nvarchar(10));

    BEGIN TRY
        IF @trancount = 0
            BEGIN TRANSACTION;
        ELSE
            SAVE TRANSACTION usp_LoadDimProduct;

        SET @sql = N'SELECT @cnt = COUNT(*) FROM staging.' + QUOTENAME(CONCAT(N'Product_', @BatchID)) + N';';
        EXEC sys.sp_executesql @sql, N'@cnt int OUTPUT', @cnt = @cnt OUTPUT;
        RAISERROR(N'Batch %d: %d staged rows', 0, 1, @BatchID, @cnt) WITH NOWAIT;

        MERGE dw.DimProduct WITH (HOLDLOCK) AS d
        USING (SELECT ProductCode, Name, Category, ListPrice FROM staging.ProductCurrent WHERE BatchID = @BatchID) AS s
            ON d.ProductCode = s.ProductCode AND d.IsCurrent = 1
        WHEN MATCHED AND EXISTS (SELECT s.Name, s.Category, s.ListPrice EXCEPT SELECT d.Name, d.Category, d.ListPrice) THEN
            UPDATE SET d.Name = s.Name, d.Category = s.Category, d.ListPrice = s.ListPrice, d.UpdatedBatchID = @BatchID
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (ProductCode, Name, Category, ListPrice, IsCurrent, InsertedBatchID)
            VALUES (s.ProductCode, s.Name, s.Category, s.ListPrice, 1, @BatchID)
        OUTPUT $action INTO @actions (act);

        SELECT @RowsInserted = COUNT(CASE WHEN act = N'INSERT' THEN 1 END),
               @RowsUpdated  = COUNT(CASE WHEN act = N'UPDATE' THEN 1 END)
        FROM @actions;

        INSERT INTO etl.LoadLog (BatchID, TableName, RowsInserted, RowsUpdated, LoadedAt)
        VALUES (@BatchID, N'dw.DimProduct', @RowsInserted, @RowsUpdated, SYSUTCDATETIME());

        IF @trancount = 0
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1 OR (XACT_STATE() = 1 AND @trancount = 0)
            ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE() = 1 AND @trancount > 0
            ROLLBACK TRANSACTION usp_LoadDimProduct;

        DECLARE @msg nvarchar(4000) = CONCAT(N'etl.usp_LoadDimProduct failed for batch ', @BatchID, N': ', ERROR_MESSAGE());
        RAISERROR(@msg, 16, 1);
        RETURN 1;
    END CATCH

    RETURN 0;
END
GO
DECLARE @ins int, @upd int, @rc int;
EXEC @rc = etl.usp_LoadDimProduct @BatchID = 20240314, @RowsInserted = @ins OUTPUT, @RowsUpdated = @upd OUTPUT;
SELECT @rc AS ReturnCode, @ins AS Inserted, @upd AS Updated;
GO
