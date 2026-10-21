-- expect: Script(TsqlSetOptionStatement,TsqlSetOptionStatement | TsqlIfStatement | CreateViewStatement | CreateIndexStatement | CreateIndexStatement | TsqlAlterViewStatement | SelectStatement)
SET NUMERIC_ROUNDABORT OFF;
SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON;
GO

IF OBJECT_ID(N'Sales.vw_ProductSalesSummary', N'V') IS NOT NULL
    DROP VIEW Sales.vw_ProductSalesSummary;
GO

CREATE VIEW Sales.vw_ProductSalesSummary
WITH SCHEMABINDING
AS
SELECT sod.ProductID,
       SUM(sod.OrderQty) AS TotalQty,
       SUM(sod.UnitPrice * sod.OrderQty) AS GrossRevenue,
       COUNT_BIG(*) AS LineCount
FROM Sales.SalesOrderDetail AS sod
GROUP BY sod.ProductID;
GO

CREATE UNIQUE CLUSTERED INDEX IX_vw_ProductSalesSummary
    ON Sales.vw_ProductSalesSummary (ProductID);
GO

CREATE NONCLUSTERED INDEX IX_vw_ProductSalesSummary_Revenue
    ON Sales.vw_ProductSalesSummary (GrossRevenue DESC);
GO

ALTER VIEW dbo.vw_ActiveProducts
AS
SELECT p.ProductID, p.Name, p.ProductNumber, p.ListPrice, s.TotalQty
FROM Production.Product AS p
LEFT JOIN Sales.vw_ProductSalesSummary AS s WITH (NOEXPAND) ON s.ProductID = p.ProductID
WHERE p.SellEndDate IS NULL
  AND p.DiscontinuedDate IS NULL;
GO

SELECT TOP (20) * FROM dbo.vw_ActiveProducts ORDER BY TotalQty DESC;
