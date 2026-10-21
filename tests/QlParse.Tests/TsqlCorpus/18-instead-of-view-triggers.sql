-- expect: Script(CreateViewStatement | TsqlCreateTriggerStatement | TsqlCreateTriggerStatement | TsqlInsertStatement)
CREATE VIEW dbo.vw_CustomerContact
AS
SELECT c.CustomerID, c.FirstName, c.LastName, e.EmailAddress, ph.PhoneNumber
FROM dbo.Customer AS c
LEFT JOIN dbo.CustomerEmail AS e ON e.CustomerID = c.CustomerID AND e.IsPrimary = 1
LEFT JOIN dbo.CustomerPhone AS ph ON ph.CustomerID = c.CustomerID AND ph.IsPrimary = 1;
GO

CREATE TRIGGER dbo.trg_vw_CustomerContact_Insert
ON dbo.vw_CustomerContact
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @map TABLE (CustomerID int NOT NULL, FirstName nvarchar(50), LastName nvarchar(50));

    MERGE dbo.Customer AS c
    USING inserted AS i ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (FirstName, LastName, ModifiedDate)
        VALUES (i.FirstName, i.LastName, GETDATE())
    OUTPUT inserted.CustomerID, i.FirstName, i.LastName INTO @map (CustomerID, FirstName, LastName);

    INSERT INTO dbo.CustomerEmail (CustomerID, EmailAddress, IsPrimary)
    SELECT m.CustomerID, i.EmailAddress, 1
    FROM @map AS m
    JOIN inserted AS i ON i.FirstName = m.FirstName AND i.LastName = m.LastName
    WHERE i.EmailAddress IS NOT NULL;

    INSERT INTO dbo.CustomerPhone (CustomerID, PhoneNumber, IsPrimary)
    SELECT m.CustomerID, i.PhoneNumber, 1
    FROM @map AS m
    JOIN inserted AS i ON i.FirstName = m.FirstName AND i.LastName = m.LastName
    WHERE i.PhoneNumber IS NOT NULL;
END
GO

CREATE TRIGGER dbo.trg_vw_CustomerContact_Delete ON dbo.vw_CustomerContact
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE c SET c.IsDeleted = 1, c.ModifiedDate = GETDATE()
    FROM dbo.Customer AS c
    INNER JOIN deleted AS d ON d.CustomerID = c.CustomerID;
END
GO

INSERT INTO dbo.vw_CustomerContact (FirstName, LastName, EmailAddress, PhoneNumber)
VALUES (N'Jane', N'Doe', N'jane.doe@example.com', N'555-0100');
GO
