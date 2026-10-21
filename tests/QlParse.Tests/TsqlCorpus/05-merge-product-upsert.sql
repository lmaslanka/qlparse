-- expect: Batch(TsqlSetOptionStatement,TsqlDeclareStatement,TsqlMergeStatement,SelectStatement,SelectStatement)
SET NOCOUNT ON;

DECLARE @MergeLog TABLE
(
    [Action]      nvarchar(10) NOT NULL,
    ProductID     int NULL,
    OldListPrice  money NULL,
    NewListPrice  money NULL
);

MERGE INTO dbo.Product WITH (HOLDLOCK) AS tgt
USING
(
    SELECT s.ProductNumber, s.Name, s.ListPrice, s.Color, s.SellStartDate
    FROM staging.ProductFeed AS s
    WHERE s.BatchID = 42
) AS src
    ON tgt.ProductNumber = src.ProductNumber
WHEN MATCHED AND (tgt.ListPrice <> src.ListPrice OR ISNULL(tgt.Color, N'') <> ISNULL(src.Color, N'')) THEN
    UPDATE SET tgt.ListPrice = src.ListPrice,
               tgt.Color = src.Color,
               tgt.ModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductNumber, Name, ListPrice, Color, SellStartDate, ModifiedDate)
    VALUES (src.ProductNumber, src.Name, src.ListPrice, src.Color, src.SellStartDate, GETDATE())
WHEN NOT MATCHED BY SOURCE AND tgt.DiscontinuedDate IS NULL THEN
    DELETE
OUTPUT $action, COALESCE(inserted.ProductID, deleted.ProductID), deleted.ListPrice, inserted.ListPrice
    INTO @MergeLog ([Action], ProductID, OldListPrice, NewListPrice);

SELECT [Action], COUNT(*) AS [RowCount]
FROM @MergeLog
GROUP BY [Action]
ORDER BY [Action];

SELECT * FROM @MergeLog WHERE [Action] = N'UPDATE' AND NewListPrice > OldListPrice * 1.2;
