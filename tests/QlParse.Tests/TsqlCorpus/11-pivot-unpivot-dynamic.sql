-- expect: Batch(SelectStatement,TsqlDeclareStatement,SelectStatement,TsqlSetVariableStatement,TsqlExecuteStatement,SelectStatement,SelectStatement)
-- Yearly sales pivot per salesperson
SELECT SalesPersonID, [2022], [2023], [2024]
FROM
(
    SELECT soh.SalesPersonID, YEAR(soh.OrderDate) AS OrderYear, soh.TotalDue
    FROM Sales.SalesOrderHeader soh
    WHERE soh.SalesPersonID IS NOT NULL
) AS src
PIVOT
(
    SUM(TotalDue) FOR OrderYear IN ([2022], [2023], [2024])
) AS pvt
ORDER BY SalesPersonID;

-- Dynamic pivot over the order statuses that actually exist
DECLARE @cols nvarchar(max), @query nvarchar(max);

SELECT @cols = STRING_AGG(QUOTENAME(status), ',') WITHIN GROUP (ORDER BY status)
FROM (SELECT DISTINCT CAST(status AS varchar(10)) AS status FROM dbo.Orders) AS s;

SET @query = N'SELECT CustomerID, ' + @cols + N'
FROM (SELECT CustomerID, CAST(status AS varchar(10)) AS status, OrderID FROM dbo.Orders) AS s
PIVOT (COUNT(OrderID) FOR status IN (' + @cols + N')) AS p;';

EXEC sp_executesql @query;

-- Unpivot quarterly targets back into rows
SELECT EmployeeID, [Quarter], Target
FROM dbo.SalesTargets
UNPIVOT
(
    Target FOR [Quarter] IN (Q1, Q2, Q3, Q4)
) AS u
WHERE Target > 0;

-- Same with CROSS APPLY VALUES (keeps NULLs)
SELECT t.EmployeeID, v.[Quarter], v.Target
FROM dbo.SalesTargets AS t
CROSS APPLY (VALUES ('Q1', t.Q1), ('Q2', t.Q2), ('Q3', t.Q3), ('Q4', t.Q4)) AS v([Quarter], Target);
