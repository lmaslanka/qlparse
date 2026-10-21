-- expect: Script(TsqlUseStatement | TsqlCreatePartitionFunctionStatement | TsqlCreatePartitionSchemeStatement | CreateTableStatement | CreateTableStatement | TsqlAlterPartitionSchemeStatement,TsqlAlterPartitionFunctionStatement,AlterTableStatement,TsqlAlterPartitionFunctionStatement,TruncateStatement | SelectStatement,TruncateStatement,TsqlAlterIndexStatement)
USE SalesDB
GO
CREATE PARTITION FUNCTION pf_OrderDate (date)
AS RANGE RIGHT FOR VALUES ('2022-01-01', '2023-01-01', '2024-01-01', '2025-01-01')
GO
CREATE PARTITION SCHEME ps_OrderDate
AS PARTITION pf_OrderDate
TO ([FG2021], [FG2022], [FG2023], [FG2024], [PRIMARY])
GO
CREATE TABLE dbo.OrderHistory
(
    OrderID bigint NOT NULL,
    OrderDate date NOT NULL,
    CustomerID int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    CONSTRAINT PK_OrderHistory PRIMARY KEY CLUSTERED (OrderDate, OrderID)
) ON ps_OrderDate (OrderDate)
GO
CREATE TABLE dbo.OrderHistory_Switch
(
    OrderID bigint NOT NULL,
    OrderDate date NOT NULL,
    CustomerID int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    CONSTRAINT PK_OrderHistory_Switch PRIMARY KEY CLUSTERED (OrderDate, OrderID)
) ON [FG2021]
GO
-- roll the window forward
ALTER PARTITION SCHEME ps_OrderDate NEXT USED [PRIMARY]
ALTER PARTITION FUNCTION pf_OrderDate() SPLIT RANGE ('2026-01-01')
ALTER TABLE dbo.OrderHistory SWITCH PARTITION 1 TO dbo.OrderHistory_Switch
ALTER PARTITION FUNCTION pf_OrderDate() MERGE RANGE ('2022-01-01')
TRUNCATE TABLE dbo.OrderHistory_Switch
GO
SELECT $PARTITION.pf_OrderDate(OrderDate) AS PartitionNumber, COUNT(*) AS [Rows], MIN(OrderDate) AS MinDate, MAX(OrderDate) AS MaxDate
FROM dbo.OrderHistory
GROUP BY $PARTITION.pf_OrderDate(OrderDate)
ORDER BY PartitionNumber;

TRUNCATE TABLE dbo.OrderHistory WITH (PARTITIONS (2, 4 TO 5));
ALTER INDEX PK_OrderHistory ON dbo.OrderHistory REBUILD PARTITION = 3 WITH (DATA_COMPRESSION = PAGE);
