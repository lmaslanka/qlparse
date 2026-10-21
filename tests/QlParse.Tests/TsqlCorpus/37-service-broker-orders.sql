-- expect: Script(TsqlAlterDatabaseStatement | TsqlMessageTypeStatement,TsqlMessageTypeStatement,TsqlCreateContractStatement,TsqlCreateQueueStatement,TsqlCreateQueueStatement,TsqlServiceStatement,TsqlServiceStatement | TsqlDeclareStatement,TsqlDeclareStatement,TsqlTransactionStatement,TsqlBeginDialogStatement,TsqlSendStatement,TsqlTransactionStatement | TsqlDeclareStatement,TsqlWaitforStatement,TsqlIfStatement)
ALTER DATABASE CURRENT SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
GO
CREATE MESSAGE TYPE [//Contoso/Orders/Request] VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE [//Contoso/Orders/Reply] VALIDATION = WELL_FORMED_XML;
CREATE CONTRACT [//Contoso/Orders/Contract]
(
    [//Contoso/Orders/Request] SENT BY INITIATOR,
    [//Contoso/Orders/Reply] SENT BY TARGET
);
CREATE QUEUE dbo.OrderRequestQueue WITH STATUS = ON, RETENTION = OFF;
CREATE QUEUE dbo.OrderReplyQueue;
CREATE SERVICE [//Contoso/Orders/TargetService] ON QUEUE dbo.OrderRequestQueue ([//Contoso/Orders/Contract]);
CREATE SERVICE [//Contoso/Orders/InitiatorService] ON QUEUE dbo.OrderReplyQueue;
GO
DECLARE @handle uniqueidentifier;
DECLARE @body xml = N'<Order><Id>5001</Id><Customer>42</Customer></Order>';

BEGIN TRANSACTION;

BEGIN DIALOG CONVERSATION @handle
    FROM SERVICE [//Contoso/Orders/InitiatorService]
    TO SERVICE N'//Contoso/Orders/TargetService'
    ON CONTRACT [//Contoso/Orders/Contract]
    WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @handle MESSAGE TYPE [//Contoso/Orders/Request] (@body);

COMMIT TRANSACTION;
GO
DECLARE @h uniqueidentifier, @msg xml, @mt sysname, @reply xml = N'<Ack/>';

WAITFOR (
    RECEIVE TOP (1)
        @h = conversation_handle,
        @mt = message_type_name,
        @msg = CAST(message_body AS xml)
    FROM dbo.OrderRequestQueue
), TIMEOUT 5000;

IF @mt = N'//Contoso/Orders/Request'
BEGIN
    SEND ON CONVERSATION @h MESSAGE TYPE [//Contoso/Orders/Reply] (@reply);
    END CONVERSATION @h;
END
ELSE IF @mt = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'
    END CONVERSATION @h;
GO
