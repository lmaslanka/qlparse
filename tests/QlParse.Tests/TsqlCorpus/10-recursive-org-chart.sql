-- expect: Batch(TsqlDeclareStatement,WithQuery,SelectStatement,SelectStatement)
DECLARE @RootEmployeeID int = 1

;WITH OrgChart (EmployeeID, ManagerID, FullName, JobTitle, [level], SortPath) AS
(
    SELECT e.EmployeeID, e.ManagerID, CAST(e.FirstName + N' ' + e.LastName AS nvarchar(200)), e.JobTitle, 0,
           CAST(RIGHT('00000' + CAST(e.EmployeeID AS varchar(10)), 5) AS varchar(900))
    FROM dbo.Employee AS e
    WHERE e.EmployeeID = @RootEmployeeID
    UNION ALL
    SELECT e.EmployeeID, e.ManagerID, CAST(e.FirstName + N' ' + e.LastName AS nvarchar(200)), e.JobTitle, oc.level + 1,
           CAST(oc.SortPath + '/' + RIGHT('00000' + CAST(e.EmployeeID AS varchar(10)), 5) AS varchar(900))
    FROM dbo.Employee AS e
    INNER JOIN OrgChart AS oc ON e.ManagerID = oc.EmployeeID
)
SELECT REPLICATE(N'    ', oc.level) + oc.FullName AS OrgTree,
       oc.JobTitle,
       oc.[level],
       DirectReports = (SELECT COUNT(*) FROM dbo.Employee AS d WHERE d.ManagerID = oc.EmployeeID)
FROM OrgChart AS oc
ORDER BY oc.SortPath
OPTION (MAXRECURSION 100)

-- Comma-separated direct reports per manager (pre-2017 STUFF / FOR XML PATH trick)
SELECT m.EmployeeID,
       m.FirstName + ' ' + m.LastName AS Manager,
       STUFF((SELECT ', ' + r.FirstName + ' ' + r.LastName
              FROM dbo.Employee AS r
              WHERE r.ManagerID = m.EmployeeID
              ORDER BY r.LastName
              FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS Reports
FROM dbo.Employee AS m
WHERE EXISTS (SELECT 1 FROM dbo.Employee AS x WHERE x.ManagerID = m.EmployeeID)

-- Same thing with STRING_AGG (SQL Server 2017+)
SELECT m.EmployeeID,
       STRING_AGG(CONVERT(nvarchar(max), r.FirstName + N' ' + r.LastName), N', ') WITHIN GROUP (ORDER BY r.LastName) AS Reports
FROM dbo.Employee AS m
JOIN dbo.Employee AS r ON r.ManagerID = m.EmployeeID
GROUP BY m.EmployeeID
