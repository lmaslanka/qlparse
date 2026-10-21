-- expect: Batch(TsqlSetOptionStatement,WithQuery,TsqlPrintStatement,TsqlDeclareStatement,TsqlWhileStatement,WithQuery,WithQuery)
SET NOCOUNT ON

-- remove duplicate contacts, keeping the most recently modified row
;WITH dupes AS
(
    SELECT ContactID,
           ROW_NUMBER() OVER (PARTITION BY LOWER(LTRIM(RTRIM(Email))) ORDER BY ModifiedDate DESC, ContactID DESC) AS rn
    FROM dbo.Contact
    WHERE Email IS NOT NULL
)
DELETE FROM dupes
WHERE rn > 1

PRINT CONCAT(@@ROWCOUNT, ' duplicate contacts removed')

DECLARE @batch int = 0

WHILE 1 = 1
BEGIN
    SET @batch = @batch + 1;

    WITH stale AS
    (
        SELECT TOP (2000) s.SessionID
        FROM dbo.UserSession s
        WHERE s.LastSeen < DATEADD(DAY, -30, GETUTCDATE())
        ORDER BY s.LastSeen
    )
    DELETE FROM stale;

    IF @@ROWCOUNT = 0 BREAK
END

;WITH latest AS (
    SELECT CustomerID, OrderID, OrderDate, TotalDue,
           ROW_NUMBER() OVER (PARTITION BY CustomerID ORDER BY OrderDate DESC) AS rn
    FROM dbo.Orders
)
UPDATE c
SET c.LastOrderID = l.OrderID,
    c.LastOrderTotal = l.TotalDue
FROM dbo.Customers c
JOIN latest l ON l.CustomerID = c.CustomerID AND l.rn = 1
WHERE c.LastOrderID IS NULL OR c.LastOrderID <> l.OrderID

;WITH ins AS (SELECT name, value FROM (VALUES ('retention_days', '30'), ('batch_size', '2000')) AS v(name, value))
INSERT INTO dbo.AppSetting (name, value)
SELECT i.name, i.value
FROM ins i
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppSetting s WHERE s.name = i.name)
