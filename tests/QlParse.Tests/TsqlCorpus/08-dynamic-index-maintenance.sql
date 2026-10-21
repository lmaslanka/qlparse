-- expect: Batch(TsqlSetOptionStatement,TsqlDeclareStatement,TsqlIfStatement,SelectStatement,TsqlDeclareCursorStatement,TsqlCursorStatement,TsqlFetchStatement,TsqlWhileStatement,TsqlCursorStatement,TsqlCursorStatement,TsqlDropObjectsStatement)
SET NOCOUNT ON;

DECLARE @SchemaName sysname,
        @TableName sysname,
        @IndexName sysname,
        @Frag float,
        @sql nvarchar(max);

IF OBJECT_ID('tempdb..#frag') IS NOT NULL
    DROP TABLE #frag;

SELECT s.name AS SchemaName,
       o.name AS TableName,
       i.name AS IndexName,
       ps.avg_fragmentation_in_percent AS Frag
INTO #frag
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS ps
JOIN sys.indexes AS i ON i.object_id = ps.object_id AND i.index_id = ps.index_id
JOIN sys.objects AS o ON o.object_id = i.object_id
JOIN sys.schemas AS s ON s.schema_id = o.schema_id
WHERE ps.avg_fragmentation_in_percent > 10
  AND ps.page_count > 1000
  AND i.name IS NOT NULL
  AND o.type = 'U';

DECLARE frag_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT SchemaName, TableName, IndexName, Frag FROM #frag ORDER BY Frag DESC;

OPEN frag_cursor;
FETCH NEXT FROM frag_cursor INTO @SchemaName, @TableName, @IndexName, @Frag;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = CONCAT(N'ALTER INDEX ', QUOTENAME(@IndexName), N' ON ', QUOTENAME(@SchemaName), N'.', QUOTENAME(@TableName),
                      CASE WHEN @Frag >= 30 THEN N' REBUILD WITH (ONLINE = ON, SORT_IN_TEMPDB = ON)' ELSE N' REORGANIZE' END, N';');

    RAISERROR(N'%s', 0, 1, @sql) WITH NOWAIT;

    BEGIN TRY
        EXEC (@sql);
    END TRY
    BEGIN CATCH
        PRINT CONCAT(N'Failed: ', @sql, N' -> ', ERROR_MESSAGE());
    END CATCH

    FETCH NEXT FROM frag_cursor INTO @SchemaName, @TableName, @IndexName, @Frag;
END

CLOSE frag_cursor;
DEALLOCATE frag_cursor;

DROP TABLE #frag;
