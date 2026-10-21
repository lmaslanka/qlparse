-- expect: Batch(TsqlTransactionStatement,TsqlUpdateStatement,TsqlDeleteStatement,TsqlDeleteStatement,TsqlUpdateStatement,TsqlUpdateStatement,TsqlIfStatement)
begin tran

update o
set    o.status = 5,
       o.ShippedDate = isnull(o.ShippedDate, s.ShipDate),
       o.ModifiedDate = getdate()
output deleted.OrderID, deleted.status as OldStatus, inserted.status as NewStatus
from   dbo.Orders o with (rowlock)
inner join dbo.Shipments s on s.OrderID = o.OrderID
where  o.status in (2, 3)
  and  s.DeliveredAt is not null

delete ol
output deleted.*
into dbo.OrderLineArchive
from dbo.OrderLines ol
join dbo.Orders o on o.OrderID = ol.OrderID
where o.status = 9
  and o.OrderDate < dateadd(year, -3, getdate())

delete from dbo.Orders
where status = 9
  and OrderDate < dateadd(year, -3, getdate())
  and not exists (select 1 from dbo.OrderLines ol where ol.OrderID = Orders.OrderID)

update c
set c.LastOrderDate = x.LastOrderDate,
    c.OrderCount = x.OrderCount
from dbo.Customers as c
join (
    select CustomerID, max(OrderDate) as LastOrderDate, count(*) as OrderCount
    from dbo.Orders
    group by CustomerID
) as x on x.CustomerID = c.CustomerID
where c.LastOrderDate is null or c.LastOrderDate < x.LastOrderDate

update top (500) dbo.EmailQueue with (readpast, updlock)
set status = 'sending', LockedAt = sysutcdatetime()
output inserted.*
where status = 'pending'

if @@error <> 0
    rollback tran
else
    commit tran
