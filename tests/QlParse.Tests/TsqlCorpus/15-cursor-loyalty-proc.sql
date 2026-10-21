-- expect: Script(TsqlCreateProcedureStatement | TsqlExecuteStatement)
CREATE PROCEDURE [dbo].[usp_RecalculateLoyaltyTiers]
    @Year smallint = NULL
AS
SET NOCOUNT ON

DECLARE @CustomerID int
DECLARE @Total money
DECLARE @Tier varchar(10)
DECLARE @Updated int = 0

IF @Year IS NULL SET @Year = YEAR(GETDATE()) - 1

DECLARE cust CURSOR LOCAL FAST_FORWARD FOR
    SELECT o.CustomerID, SUM(o.TotalDue)
    FROM dbo.Orders o
    WHERE o.OrderDate >= DATEFROMPARTS(@Year, 1, 1)
      AND o.OrderDate <  DATEFROMPARTS(@Year + 1, 1, 1)
    GROUP BY o.CustomerID

OPEN cust
FETCH NEXT FROM cust INTO @CustomerID, @Total

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Tier = CASE
                    WHEN @Total >= 10000 THEN 'Platinum'
                    WHEN @Total >= 5000  THEN 'Gold'
                    WHEN @Total >= 1000  THEN 'Silver'
                    ELSE 'Bronze'
                END

    UPDATE dbo.Customer
    SET LoyaltyTier = @Tier, ModifiedDate = GETDATE()
    WHERE CustomerID = @CustomerID
      AND ISNULL(LoyaltyTier, '') <> @Tier

    SET @Updated = @Updated + @@ROWCOUNT

    FETCH NEXT FROM cust INTO @CustomerID, @Total
END

CLOSE cust
DEALLOCATE cust

PRINT 'Customers updated: ' + CAST(@Updated AS varchar(12))
RETURN @Updated
GO
EXEC dbo.usp_RecalculateLoyaltyTiers @Year = 2024
GO
