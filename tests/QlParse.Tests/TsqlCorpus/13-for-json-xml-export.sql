-- expect: Script(TsqlUseStatement | SelectStatement,SelectStatement,SelectStatement,TsqlDeclareStatement,SelectStatement)
USE AdventureWorks2022;
GO

-- Nested JSON document for the API export
SELECT TOP (10)
       soh.SalesOrderID AS [order.id],
       soh.OrderDate AS [order.date],
       soh.Status AS [order.status],
       c.AccountNumber AS [customer.account],
       (
           SELECT sod.ProductID AS productId,
                  sod.OrderQty AS qty,
                  sod.UnitPrice AS unitPrice,
                  sod.SpecialOfferID AS offer
           FROM Sales.SalesOrderDetail AS sod
           WHERE sod.SalesOrderID = soh.SalesOrderID
           FOR JSON PATH
       ) AS [order.lines]
FROM Sales.SalesOrderHeader AS soh
JOIN Sales.Customer AS c ON c.CustomerID = soh.CustomerID
ORDER BY soh.OrderDate DESC
FOR JSON PATH, ROOT('orders'), INCLUDE_NULL_VALUES;

SELECT TOP (1) p.ProductID, p.Name, p.Color, p.Size
FROM Production.Product AS p
WHERE p.ProductSubcategoryID = 1
FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER;

SELECT pc.Name AS [@category],
       (SELECT ps.Name AS [@name], ps.ProductSubcategoryID AS [@id]
        FROM Production.ProductSubcategory AS ps
        WHERE ps.ProductCategoryID = pc.ProductCategoryID
        FOR XML PATH('Subcategory'), TYPE)
FROM Production.ProductCategory AS pc
FOR XML PATH('Category'), ROOT('Catalog');

DECLARE @x xml = (SELECT TOP (5) BusinessEntityID, FirstName, LastName FROM Person.Person FOR XML RAW('person'), ELEMENTS, ROOT('people'));

SELECT n.value('(BusinessEntityID)[1]', 'int') AS Id,
       n.value('(LastName/text())[1]', 'nvarchar(50)') AS LastName
FROM @x.nodes('/people/person') AS t(n);
GO
