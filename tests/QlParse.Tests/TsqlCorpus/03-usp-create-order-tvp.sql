-- expect: Script(TsqlUseStatement | TsqlIfStatement | TsqlCreateProcedureStatement | TsqlPermissionStatement)
USE [SalesDB]
GO
IF TYPE_ID(N'Sales.OrderLineType') IS NULL
    CREATE TYPE [Sales].[OrderLineType] AS TABLE
    (
        [ProductID] int NOT NULL,
        [Quantity] smallint NOT NULL,
        [UnitPrice] money NOT NULL,
        PRIMARY KEY ([ProductID])
    );
GO
CREATE OR ALTER PROCEDURE [Sales].[usp_CreateOrder]
    @CustomerID   int,
    @Lines        [Sales].[OrderLineType] READONLY,
    @Comment      nvarchar(400) = NULL,
    @OrderID      int = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM @Lines)
    BEGIN
        RAISERROR(N'An order must contain at least one line.', 16, 1);
        RETURN -1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [Sales].[OrderHeader] ([CustomerID], [OrderDate], [Status], [TotalDue], [Comment])
        SELECT @CustomerID, SYSDATETIME(), 1, SUM(l.Quantity * l.UnitPrice), @Comment
        FROM @Lines AS l;

        SET @OrderID = SCOPE_IDENTITY();

        INSERT INTO [Sales].[OrderLine] ([OrderID], [ProductID], [Quantity], [UnitPrice])
        SELECT @OrderID, l.ProductID, l.Quantity, l.UnitPrice
        FROM @Lines AS l;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    RETURN 0;
END
GO
GRANT EXECUTE ON [Sales].[usp_CreateOrder] TO [SalesApp]
GO
