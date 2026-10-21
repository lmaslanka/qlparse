-- expect: Script(TsqlIfStatement | TsqlIfStatement | TsqlCreateTriggerStatement | TsqlToggleTriggerStatement | TsqlToggleTriggerStatement)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DdlEventLog]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[DdlEventLog](
	[EventID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[EventTime] [datetime2](3) NOT NULL DEFAULT (sysdatetime()),
	[EventType] [nvarchar](128) NULL,
	[SchemaName] [sysname] NULL,
	[ObjectName] [sysname] NULL,
	[LoginName] [sysname] NULL,
	[Command] [nvarchar](max) NULL,
	[EventXml] [xml] NULL
)
END
GO

IF EXISTS (SELECT * FROM sys.triggers WHERE name = N'trg_LogDdl' AND parent_class = 0)
    DROP TRIGGER [trg_LogDdl] ON DATABASE;
GO

CREATE TRIGGER [trg_LogDdl]
ON DATABASE
FOR CREATE_TABLE, ALTER_TABLE, DROP_TABLE, CREATE_PROCEDURE, ALTER_PROCEDURE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @e xml = EVENTDATA();

    INSERT INTO dbo.DdlEventLog (EventType, SchemaName, ObjectName, LoginName, Command, EventXml)
    VALUES (
        @e.value('(/EVENT_INSTANCE/EventType)[1]', 'nvarchar(128)'),
        @e.value('(/EVENT_INSTANCE/SchemaName)[1]', 'nvarchar(128)'),
        @e.value('(/EVENT_INSTANCE/ObjectName)[1]', 'nvarchar(128)'),
        @e.value('(/EVENT_INSTANCE/LoginName)[1]', 'nvarchar(128)'),
        @e.value('(/EVENT_INSTANCE/TSQLCommand/CommandText)[1]', 'nvarchar(max)'),
        @e
    );

    IF @e.value('(/EVENT_INSTANCE/EventType)[1]', 'nvarchar(128)') = N'DROP_TABLE'
       AND @e.value('(/EVENT_INSTANCE/SchemaName)[1]', 'nvarchar(128)') = N'audit'
    BEGIN
        RAISERROR(N'Tables in the audit schema cannot be dropped.', 16, 1);
        ROLLBACK;
    END
END;
GO

DISABLE TRIGGER [trg_LogDdl] ON DATABASE;
GO
ENABLE TRIGGER [trg_LogDdl] ON DATABASE;
GO
