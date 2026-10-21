-- expect: Script(TsqlIfStatement | CreateTableStatement | TsqlCreateFulltextIndexStatement | SelectStatement,SelectStatement,SelectStatement,SelectStatement | TsqlAlterFulltextIndexStatement)
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftDocuments')
    CREATE FULLTEXT CATALOG ftDocuments AS DEFAULT;
GO
CREATE TABLE dbo.KnowledgeArticle
(
    ArticleID int IDENTITY(1,1) NOT NULL,
    Title nvarchar(200) NOT NULL,
    Body nvarchar(max) NOT NULL,
    FileExtension varchar(8) NULL,
    Attachment varbinary(max) NULL,
    CONSTRAINT PK_KnowledgeArticle PRIMARY KEY CLUSTERED (ArticleID)
);
GO
CREATE FULLTEXT INDEX ON dbo.KnowledgeArticle
(
    Title LANGUAGE 1033,
    Body LANGUAGE 1033,
    Attachment TYPE COLUMN FileExtension LANGUAGE 1033
)
KEY INDEX PK_KnowledgeArticle
ON ftDocuments
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM);
GO
SELECT ArticleID, Title
FROM dbo.KnowledgeArticle
WHERE CONTAINS((Title, Body), N'"deadlock*" AND NOT "trace flag"');

SELECT ka.ArticleID, ka.Title, ft.[RANK]
FROM FREETEXTTABLE(dbo.KnowledgeArticle, Body, N'how do I reduce blocking', 10) AS ft
INNER JOIN dbo.KnowledgeArticle AS ka ON ka.ArticleID = ft.[KEY]
ORDER BY ft.[RANK] DESC;

SELECT ArticleID FROM dbo.KnowledgeArticle WHERE FREETEXT(Body, N'index fragmentation');

SELECT ArticleID, Title
FROM dbo.KnowledgeArticle
WHERE CONTAINS(Body, N'NEAR((query, plan), 5)')
   OR CONTAINS(Title, N'FORMSOF(INFLECTIONAL, rebuild)');
GO
ALTER FULLTEXT INDEX ON dbo.KnowledgeArticle START FULL POPULATION;
GO
