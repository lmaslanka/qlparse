-- expect: Batch(TsqlSetTransactionIsolationLevelStatement,TsqlDeclareStatement,WithQuery)
-- Monthly sales by territory with running totals and rankings
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @StartDate date = DATEFROMPARTS(YEAR(GETDATE()) - 1, 1, 1),
        @EndDate   date = EOMONTH(GETDATE(), -1);

WITH MonthlySales AS
(
    SELECT  st.Name AS Territory,
            DATEFROMPARTS(YEAR(soh.OrderDate), MONTH(soh.OrderDate), 1) AS month,
            SUM(soh.SubTotal) AS Sales,
            COUNT(DISTINCT soh.CustomerID) AS Customers
    FROM    Sales.SalesOrderHeader AS soh WITH (NOLOCK)
    JOIN    Sales.SalesTerritory AS st ON st.TerritoryID = soh.TerritoryID
    WHERE   soh.OrderDate >= @StartDate
      AND   soh.OrderDate < DATEADD(DAY, 1, @EndDate)
    GROUP BY st.Name, DATEFROMPARTS(YEAR(soh.OrderDate), MONTH(soh.OrderDate), 1)
),
Ranked AS
(
    SELECT  ms.*,
            SUM(ms.Sales) OVER (PARTITION BY ms.Territory ORDER BY ms.[month]
                                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningTotal,
            LAG(ms.Sales, 1, 0) OVER (PARTITION BY ms.Territory ORDER BY ms.[month]) AS PrevMonthSales,
            LEAD(ms.Sales) OVER (PARTITION BY ms.Territory ORDER BY ms.[month]) AS NextMonthSales,
            RANK() OVER (PARTITION BY ms.[month] ORDER BY ms.Sales DESC) AS TerritoryRank,
            DENSE_RANK() OVER (ORDER BY ms.Sales DESC) AS OverallDenseRank,
            NTILE(4) OVER (PARTITION BY ms.[month] ORDER BY ms.Sales) AS Quartile,
            ROW_NUMBER() OVER (PARTITION BY ms.Territory ORDER BY ms.Sales DESC) AS rn
    FROM    MonthlySales AS ms
)
SELECT  r.Territory,
        FORMAT(r.[month], 'yyyy-MM') AS [Period],
        r.Sales,
        r.RunningTotal,
        CAST(IIF(r.PrevMonthSales = 0, NULL, (r.Sales - r.PrevMonthSales) / r.PrevMonthSales * 100) AS decimal(9,2)) AS MoMPct,
        r.TerritoryRank,
        CHOOSE(r.Quartile, 'Bottom', 'Lower-mid', 'Upper-mid', 'Top') AS QuartileLabel,
        IIF(r.rn = 1, 'Best month', NULL) AS Flag
FROM    Ranked AS r
ORDER BY r.Territory, r.month
OPTION (RECOMPILE);
