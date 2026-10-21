-- expect: Script(TsqlIfStatement | CreateTableStatement | TsqlSetIdentityInsertStatement,TsqlInsertStatement,TsqlSetIdentityInsertStatement,TsqlDbccStatement,TsqlInsertStatement,TsqlInsertStatement,SelectStatement,TsqlDeclareStatement,TsqlExecuteStatement,SelectStatement,AlterSequenceStatement)
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'InvoiceNumber' AND SCHEMA_NAME(schema_id) = N'Billing')
    CREATE SEQUENCE Billing.InvoiceNumber
        AS bigint
        START WITH 100000
        INCREMENT BY 1
        MINVALUE 100000
        NO MAXVALUE
        NO CYCLE
        CACHE 50;
GO

CREATE TABLE Billing.Invoice
(
    InvoiceID     int IDENTITY(1,1) NOT NULL,
    InvoiceNumber bigint NOT NULL CONSTRAINT DF_Invoice_Number DEFAULT (NEXT VALUE FOR Billing.InvoiceNumber),
    CustomerID    int NOT NULL,
    [date]        date NOT NULL,
    Amount        decimal(18,2) NOT NULL,
    CONSTRAINT PK_Invoice PRIMARY KEY CLUSTERED (InvoiceID),
    CONSTRAINT UQ_Invoice_Number UNIQUE (InvoiceNumber)
);
GO

SET IDENTITY_INSERT Billing.Invoice ON;

INSERT INTO Billing.Invoice (InvoiceID, InvoiceNumber, CustomerID, [date], Amount)
VALUES (1, 99999, 42, '2023-12-31', 150.00);

SET IDENTITY_INSERT Billing.Invoice OFF;

DBCC CHECKIDENT ('Billing.Invoice', RESEED, 1);

INSERT INTO Billing.Invoice (CustomerID, date, Amount) VALUES (42, GETDATE(), 99.95);
INSERT INTO Billing.Invoice (InvoiceNumber, CustomerID, date, Amount)
VALUES (NEXT VALUE FOR Billing.InvoiceNumber, 43, GETDATE(), 10.00);

SELECT SCOPE_IDENTITY() AS LastIdentity, IDENT_CURRENT('Billing.Invoice') AS CurrentIdentity;

DECLARE @first sql_variant, @last sql_variant;
EXEC sys.sp_sequence_get_range
     @sequence_name = N'Billing.InvoiceNumber',
     @range_size = 10,
     @range_first_value = @first OUTPUT,
     @range_last_value = @last OUTPUT;
SELECT @first AS RangeFirst, @last AS RangeLast;

ALTER SEQUENCE Billing.InvoiceNumber RESTART WITH 200000 INCREMENT BY 1;
GO
