-- expect: Script(TsqlUseStatement | TsqlSetOptionStatement,TsqlSetOptionStatement | TsqlCreateProcedureStatement)
USE WideWorldImporters;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [Sales].[usp_ApplyCustomerCredit]
@CustomerID int,
@CreditAmount decimal(18,2),
@Reason nvarchar(200) = N'Goodwill credit',
@TransactionID int = NULL OUTPUT
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @ReturnCode int = 0,
            @ErrMsg nvarchar(2048),
            @ErrSeverity int,
            @ErrState int;

    IF @CreditAmount <= 0
    BEGIN
        RAISERROR(N'Credit amount must be positive (%s).', 16, 1, @Reason);
        RETURN 1;
    END;

    BEGIN TRY
        BEGIN TRAN;

        INSERT Sales.CustomerTransactions
            (CustomerID, TransactionTypeID, TransactionDate, AmountExcludingTax, TaxAmount, TransactionAmount, OutstandingBalance, IsFinalized, LastEditedBy)
        VALUES
            (@CustomerID, 3, CAST(SYSDATETIME() AS date), -@CreditAmount, 0, -@CreditAmount, -@CreditAmount, 0, 1);

        SET @TransactionID = CAST(SCOPE_IDENTITY() AS int);

        UPDATE Sales.Customers
            SET CreditLimit = ISNULL(CreditLimit, 0) + @CreditAmount
        WHERE CustomerID = @CustomerID;

        IF @@ROWCOUNT = 0
            RAISERROR(N'Customer %d does not exist.', 16, 1, @CustomerID);

        COMMIT;
    END TRY
    BEGIN CATCH
        SELECT @ErrMsg = ERROR_MESSAGE(), @ErrSeverity = ERROR_SEVERITY(), @ErrState = ERROR_STATE();

        IF XACT_STATE() <> 0 ROLLBACK TRAN;

        INSERT dbo.ErrorLog (ErrorTime, UserName, ErrorNumber, ErrorSeverity, ErrorState, ErrorProcedure, ErrorLine, ErrorMessage)
        VALUES (SYSDATETIME(), SUSER_SNAME(), ERROR_NUMBER(), @ErrSeverity, @ErrState, ERROR_PROCEDURE(), ERROR_LINE(), @ErrMsg);

        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState) WITH LOG;
        SET @ReturnCode = -1;
    END CATCH;

    RETURN @ReturnCode;
END;
GO
