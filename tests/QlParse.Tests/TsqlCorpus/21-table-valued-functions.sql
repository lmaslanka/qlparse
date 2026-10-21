-- expect: Script(TsqlCreateFunctionStatement | TsqlCreateFunctionStatement | SelectStatement)
CREATE OR ALTER FUNCTION Sales.ufn_CustomerOrders
(
    @CustomerID int,
    @FromDate   date = '19000101'
)
RETURNS TABLE
AS
RETURN
(
    SELECT soh.SalesOrderID, soh.OrderDate, soh.TotalDue,
           ROW_NUMBER() OVER (ORDER BY soh.OrderDate DESC) AS RecencyRank
    FROM Sales.SalesOrderHeader AS soh
    WHERE soh.CustomerID = @CustomerID
      AND soh.OrderDate >= @FromDate
);
GO

CREATE OR ALTER FUNCTION dbo.ufn_SplitDateRange (@Start date, @End date, @Bucket varchar(5))
RETURNS @ranges TABLE
(
    RangeStart date NOT NULL PRIMARY KEY,
    RangeEnd   date NOT NULL,
    Label      varchar(20) NULL
)
AS
BEGIN
    DECLARE @cur date = @Start;

    WHILE @cur <= @End
    BEGIN
        INSERT INTO @ranges (RangeStart, RangeEnd, Label)
        VALUES (
            @cur,
            CASE @Bucket WHEN 'month' THEN EOMONTH(@cur)
                         WHEN 'week' THEN DATEADD(DAY, 6, @cur)
                         ELSE @cur END,
            CASE @Bucket WHEN 'month' THEN FORMAT(@cur, 'MMM yyyy')
                         WHEN 'week' THEN CONCAT('Week ', DATEPART(ISO_WEEK, @cur))
                         ELSE CONVERT(varchar(10), @cur, 120) END
        );

        SET @cur = CASE @Bucket WHEN 'month' THEN DATEADD(MONTH, 1, @cur)
                                WHEN 'week' THEN DATEADD(WEEK, 1, @cur)
                                ELSE DATEADD(DAY, 1, @cur) END;
    END

    RETURN;
END
GO

SELECT r.Label, o.SalesOrderID, o.TotalDue
FROM dbo.ufn_SplitDateRange('2024-01-01', '2024-06-30', 'month') AS r
OUTER APPLY Sales.ufn_CustomerOrders(29825, r.RangeStart) AS o
WHERE o.RecencyRank = 1 OR o.RecencyRank IS NULL;
