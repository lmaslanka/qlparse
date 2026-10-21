-- expect: Script(TsqlCreateFunctionStatement | TsqlCreateFunctionStatement | SelectStatement)
CREATE FUNCTION [dbo].[ufn_GetFiscalYear] (@date date)
RETURNS smallint
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @fy smallint;
    SET @fy = CASE WHEN MONTH(@date) >= 7 THEN YEAR(@date) + 1 ELSE YEAR(@date) END;
    RETURN @fy;
END
GO

CREATE OR ALTER FUNCTION dbo.ufn_FormatPhone (@phone varchar(20))
RETURNS varchar(20)
WITH SCHEMABINDING, RETURNS NULL ON NULL INPUT
AS
BEGIN
    DECLARE @digits varchar(20) = '';
    DECLARE @i int = 1;

    WHILE @i <= LEN(@phone)
    BEGIN
        IF SUBSTRING(@phone, @i, 1) LIKE '[0-9]'
            SET @digits = @digits + SUBSTRING(@phone, @i, 1);
        SET @i += 1;
    END

    RETURN CASE LEN(@digits)
               WHEN 10 THEN '(' + LEFT(@digits, 3) + ') ' + SUBSTRING(@digits, 4, 3) + '-' + RIGHT(@digits, 4)
               WHEN 7 THEN LEFT(@digits, 3) + '-' + RIGHT(@digits, 4)
               ELSE @phone
           END;
END
GO

SELECT dbo.ufn_GetFiscalYear('2024-08-15') AS FY,
       dbo.ufn_FormatPhone('555.867.5309') AS Phone7,
       dbo.ufn_FormatPhone('(206) 555-0100') AS Phone10;
GO
