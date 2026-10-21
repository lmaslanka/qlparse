-- expect: Batch(TsqlSetOptionStatement,TsqlSetOptionStatement,TsqlDeclareStatement,TsqlTransactionStatement,TsqlTryCatchStatement)
set nocount on;
set xact_abort off;

declare @FromAccount int = 1001,
        @ToAccount   int = 2002,
        @Amount      decimal(19,4) = 250.00,
        @Balance     decimal(19,4);

begin transaction TransferFunds;

begin try
    update dbo.Account with (updlock, rowlock)
       set Balance = Balance - @Amount,
           LastModified = sysutcdatetime()
     where AccountID = @FromAccount;

    select @Balance = Balance from dbo.Account where AccountID = @FromAccount;

    if @Balance < 0
    begin
        raiserror('Insufficient funds on account %d.', 16, 1, @FromAccount);
    end

    save transaction BeforeCredit;

    update dbo.Account
       set Balance = Balance + @Amount,
           LastModified = sysutcdatetime()
     where AccountID = @ToAccount;

    if @@rowcount = 0
    begin
        rollback transaction BeforeCredit;
        print 'Target account not found; credit rolled back to savepoint.';
    end

    insert into dbo.AccountLedger (AccountID, Amount, [type], [timestamp])
    values (@FromAccount, -@Amount, 'D', sysutcdatetime()),
           (@ToAccount, @Amount, 'C', sysutcdatetime());

    commit transaction TransferFunds;
end try
begin catch
    if xact_state() <> 0
        rollback transaction;

    select error_number() as ErrorNumber,
           error_severity() as ErrorSeverity,
           error_state() as ErrorState,
           error_line() as ErrorLine,
           error_message() as ErrorMessage;
end catch
