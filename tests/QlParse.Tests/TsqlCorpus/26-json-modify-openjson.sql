-- expect: Batch(TsqlDeclareStatement,TsqlIfStatement,TsqlSetVariableStatement,TsqlSetVariableStatement,TsqlSetVariableStatement,TsqlSetVariableStatement,TsqlSetVariableStatement,TsqlSetVariableStatement,SelectStatement,SelectStatement,TsqlUpdateStatement,TsqlInsertStatement,SelectStatement)
DECLARE @json nvarchar(max) = N'{
  "id": 17,
  "name": "Mountain Bike",
  "tags": ["bike", "outdoor"],
  "price": { "list": 1299.00, "currency": "USD" },
  "active": true
}';

IF ISJSON(@json) = 0
BEGIN
    RAISERROR('Invalid JSON document.', 16, 1);
    RETURN;
END

SET @json = JSON_MODIFY(@json, '$.price.list', 1199.00);
SET @json = JSON_MODIFY(@json, 'append $.tags', 'sale');
SET @json = JSON_MODIFY(@json, '$.active', CAST(0 AS bit));
SET @json = JSON_MODIFY(@json, '$.discontinued', NULL);
SET @json = JSON_MODIFY(@json, 'strict $.name', N'Mountain Bike 2025');
SET @json = JSON_MODIFY(@json, '$.dimensions', JSON_QUERY(N'{"w":60,"h":110}'));

SELECT JSON_VALUE(@json, '$.name') AS name,
       CAST(JSON_VALUE(@json, '$.price.list') AS money) AS list_price,
       JSON_QUERY(@json, '$.tags') AS tags,
       JSON_VALUE(@json, 'lax $.missing') AS missing_lax;

SELECT o.[key], o.value, o.type
FROM OPENJSON(@json) AS o;

UPDATE dbo.ProductDocument
SET Doc = JSON_MODIFY(Doc, '$.price.currency', 'EUR'),
    ModifiedAt = SYSUTCDATETIME()
WHERE ISJSON(Doc) = 1
  AND JSON_VALUE(Doc, '$.price.currency') = 'USD'
  AND JSON_VALUE(Doc, '$.region') = 'EU';

INSERT INTO dbo.ProductDocument (ProductID, Doc)
SELECT j.id, j.doc
FROM OPENJSON(N'[{"id":1,"doc":{"name":"A"}},{"id":2,"doc":{"name":"B"}}]')
     WITH (id int '$.id', doc nvarchar(max) '$.doc' AS JSON) AS j;

SELECT ProductID, Doc
FROM dbo.ProductDocument
WHERE JSON_PATH_EXISTS(Doc, '$.dimensions') = 1
FOR JSON PATH, ROOT('products');
