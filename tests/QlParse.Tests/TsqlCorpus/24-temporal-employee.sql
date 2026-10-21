-- expect: Batch(CreateTableStatement,TsqlInsertStatement,TsqlUpdateStatement,TsqlDeclareStatement,SelectStatement,SelectStatement,SelectStatement,AlterTableStatement,AlterTableStatement,TsqlDeleteStatement,AlterTableStatement)
CREATE TABLE dbo.Employee
(
    EmployeeID   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED,
    FullName     nvarchar(100) NOT NULL,
    Department   nvarchar(50) NOT NULL,
    Salary       decimal(12,2) NOT NULL,
    [level]      tinyint NOT NULL CONSTRAINT DF_Employee_Level DEFAULT (1),
    ValidFrom    datetime2(7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    ValidTo      datetime2(7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.EmployeeHistory, DATA_CONSISTENCY_CHECK = ON));

INSERT INTO dbo.Employee (FullName, Department, Salary, level)
VALUES (N'Ana Trujillo', N'Finance', 72000, 2),
       (N'Thomas Hardy', N'Sales', 65000, 1);

UPDATE dbo.Employee SET Salary = Salary * 1.05 WHERE Department = N'Finance';

DECLARE @asOf datetime2(7) = DATEADD(MINUTE, -5, SYSUTCDATETIME());

SELECT EmployeeID, FullName, Salary, ValidFrom, ValidTo
FROM dbo.Employee FOR SYSTEM_TIME AS OF @asOf;

SELECT EmployeeID, FullName, Salary, ValidFrom, ValidTo
FROM dbo.Employee FOR SYSTEM_TIME BETWEEN '2024-01-01' AND '9999-12-31 23:59:59.9999999'
WHERE EmployeeID = 1
ORDER BY ValidFrom;

SELECT e.FullName, h.Salary AS PreviousSalary, e.Salary AS CurrentSalary
FROM dbo.Employee AS e
JOIN dbo.Employee FOR SYSTEM_TIME ALL AS h ON h.EmployeeID = e.EmployeeID AND h.ValidTo <= SYSUTCDATETIME()
WHERE e.Salary <> h.Salary;

ALTER TABLE dbo.Employee ADD Email nvarchar(256) NULL;

-- trim history older than seven years
ALTER TABLE dbo.Employee SET (SYSTEM_VERSIONING = OFF);
DELETE FROM dbo.EmployeeHistory WHERE ValidTo < DATEADD(YEAR, -7, SYSUTCDATETIME());
ALTER TABLE dbo.Employee SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.EmployeeHistory, DATA_CONSISTENCY_CHECK = ON));
