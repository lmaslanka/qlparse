-- expect: Batch(SelectStatement,SelectStatement,SelectStatement,TsqlDeclareStatement,SelectStatement)
SELECT
    COALESCE(st.[Group], 'All regions') AS Region,
    COALESCE(st.Name, 'All territories') AS Territory,
    YEAR(soh.OrderDate) AS [year],
    SUM(soh.TotalDue) AS Revenue,
    GROUPING(st.[Group]) AS g_region,
    GROUPING_ID(st.[Group], st.Name, YEAR(soh.OrderDate)) AS gid
FROM Sales.SalesOrderHeader soh
JOIN Sales.SalesTerritory st ON st.TerritoryID = soh.TerritoryID
GROUP BY GROUPING SETS
(
    (st.[Group], st.Name, YEAR(soh.OrderDate)),
    (st.[Group], YEAR(soh.OrderDate)),
    (YEAR(soh.OrderDate)),
    ()
)
ORDER BY gid, Region, Territory, [year];

SELECT pc.Name AS Category, ps.Name AS Subcategory, SUM(sod.LineTotal) AS Sales
FROM Sales.SalesOrderDetail sod
JOIN Production.Product p ON p.ProductID = sod.ProductID
JOIN Production.ProductSubcategory ps ON ps.ProductSubcategoryID = p.ProductSubcategoryID
JOIN Production.ProductCategory pc ON pc.ProductCategoryID = ps.ProductCategoryID
GROUP BY ROLLUP (pc.Name, ps.Name);

SELECT p.Color, p.Size, COUNT(*) AS Products
FROM Production.Product p
GROUP BY CUBE (p.Color, p.Size)
HAVING COUNT(*) > 1;

DECLARE @PageNumber int = 3, @PageSize int = 25;

SELECT p.ProductID, p.Name, p.ListPrice, p.SellStartDate,
       DATEDIFF(DAY, p.SellStartDate, GETDATE()) AS DaysOnSale,
       TRY_CONVERT(int, p.Size) AS NumericSize,
       COUNT(*) OVER () AS TotalRows
FROM Production.Product p
WHERE p.ListPrice > 0
ORDER BY p.ListPrice DESC, p.ProductID
OFFSET (@PageNumber - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;
