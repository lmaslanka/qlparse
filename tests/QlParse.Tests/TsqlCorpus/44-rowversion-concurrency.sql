-- expect: Script(TsqlIfStatement | CreateTableStatement | TsqlCreateProcedureStatement | TsqlDeclareStatement,TsqlExecuteStatement,SelectStatement)
IF NOT EXISTS (SELECT 1 FROM sys.messages WHERE message_id = 50101 AND language_id = 1033)
    EXEC sp_addmessage @msgnum = 50101, @severity = 16, @msgtext = N'Document %s was modified by another user.', @lang = 'us_english';
GO
CREATE TABLE [dbo].[Document]
(
    [DocumentID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Document] PRIMARY KEY,
    [Title] nvarchar(200) NOT NULL,
    [Status] varchar(20) NOT NULL CONSTRAINT [DF_Document_Status] DEFAULT ('draft'),
    [Body] nvarchar(max) NULL,
    [RowVer] rowversion NOT NULL,
    [UpdatedAt] datetime2(3) NOT NULL CONSTRAINT [DF_Document_UpdatedAt] DEFAULT (sysutcdatetime())
)
GO
create or alter procedure dbo.usp_UpdateDocument
    @DocumentID int,
    @Title nvarchar(200),
    @Body nvarchar(max),
    @RowVer binary(8),
    @NewRowVer binary(8) = null output
as
begin
    set nocount on;
    declare @msg nvarchar(2048);

    update dbo.Document
       set Title = @Title,
           Body = @Body,
           UpdatedAt = sysutcdatetime()
     where DocumentID = @DocumentID
       and RowVer = @RowVer;

    if @@rowcount = 0
    begin
        if not exists (select 1 from dbo.Document where DocumentID = @DocumentID)
        begin
            set @msg = N'Document not found.';
            throw 50404, @msg, 1;
        end

        set @msg = formatmessage(50101, cast(@DocumentID as varchar(12)));
        throw 50101, @msg, 1;
    end

    select @NewRowVer = RowVer from dbo.Document where DocumentID = @DocumentID;
end
go
declare @rv binary(8) = (select RowVer from dbo.Document where DocumentID = 1),
        @newrv binary(8);
exec dbo.usp_UpdateDocument @DocumentID = 1, @Title = N'Q3 plan', @Body = N'...', @RowVer = @rv, @NewRowVer = @newrv output;
select @rv as OldVersion, @newrv as NewVersion, min_active_rowversion() as MinActive;
go
