-- expect: Script(TsqlSetOptionStatement | TsqlSetOptionStatement | TsqlCreateProcedureStatement)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.usp_SearchCustomers
    @LastName     nvarchar(50)  = NULL,
    @City         nvarchar(30)  = NULL,
    @SortColumn   sysname       = N'LastName',
    @SortDesc     bit           = 0,
    @PageNumber   int           = 1,
    @PageSize     int           = 50,
    @TotalRows    int           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @sql nvarchar(max) = N'',
            @where nvarchar(max) = N' WHERE 1 = 1',
            @params nvarchar(max) = N'@LastName nvarchar(50), @City nvarchar(30), @Skip int, @Take int, @Total int OUTPUT',
            @Skip int = (@PageNumber - 1) * @PageSize;

    IF @SortColumn NOT IN (N'LastName', N'FirstName', N'City', N'ModifiedDate')
        SET @SortColumn = N'LastName';

    IF @LastName IS NOT NULL SET @where += N' AND c.LastName LIKE @LastName + N''%''';
    IF @City IS NOT NULL SET @where += N' AND a.City = @City';

    SET @sql = N'SELECT @Total = COUNT(*) FROM dbo.Customer AS c '
             + N'LEFT JOIN dbo.Address AS a ON a.CustomerID = c.CustomerID' + @where + N';' + NCHAR(13) + NCHAR(10)
             + N'SELECT c.CustomerID, c.FirstName, c.LastName, a.City, c.ModifiedDate '
             + N'FROM dbo.Customer AS c LEFT JOIN dbo.Address AS a ON a.CustomerID = c.CustomerID'
             + @where
             + N' ORDER BY ' + QUOTENAME(@SortColumn) + CASE WHEN @SortDesc = 1 THEN N' DESC' ELSE N' ASC' END
             + N' OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;';

    PRINT @sql;

    EXEC sys.sp_executesql @sql, @params,
         @LastName = @LastName,
         @City = @City,
         @Skip = @Skip,
         @Take = @PageSize,
         @Total = @TotalRows OUTPUT;
END
GO
