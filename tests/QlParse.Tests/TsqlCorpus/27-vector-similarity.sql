-- expect: Batch(TsqlDropObjectsStatement,CreateTableStatement,TsqlInsertStatement,TsqlDeclareStatement,TsqlDeclareStatement,WithQuery,SelectStatement)
-- Requires SQL Server 2025 or Azure SQL Database (vector data type)
DROP TABLE IF EXISTS dbo.ProductEmbedding;

CREATE TABLE dbo.ProductEmbedding
(
    ProductID   int NOT NULL CONSTRAINT PK_ProductEmbedding PRIMARY KEY,
    Description nvarchar(400) NOT NULL,
    Embedding   vector(3) NOT NULL
);

INSERT INTO dbo.ProductEmbedding (ProductID, Description, Embedding)
VALUES (1, N'Road bike, carbon frame', '[0.12, 0.85, 0.33]'),
       (2, N'Mountain bike, full suspension', '[0.10, 0.80, 0.51]'),
       (3, N'Helmet, MIPS', '[0.91, 0.05, 0.12]');

DECLARE @q vector(3) = CAST('[0.11, 0.82, 0.40]' AS vector(3));
DECLARE @k int = 2;

WITH scored AS
(
    SELECT pe.ProductID, pe.Description,
           VECTOR_DISTANCE('cosine', @q, pe.Embedding) AS CosineDistance,
           VECTOR_DISTANCE('euclidean', @q, pe.Embedding) AS EuclideanDistance
    FROM dbo.ProductEmbedding AS pe
)
SELECT TOP (@k) s.ProductID, s.Description,
       CAST(s.CosineDistance AS decimal(9,6)) AS CosineDistance,
       CAST(s.EuclideanDistance AS decimal(9,6)) AS EuclideanDistance
FROM scored AS s
ORDER BY s.CosineDistance;

SELECT ProductID, CAST(Embedding AS nvarchar(max)) AS EmbeddingJson
FROM dbo.ProductEmbedding
WHERE VECTOR_DISTANCE('dot', @q, Embedding) < 0
FOR JSON PATH;
